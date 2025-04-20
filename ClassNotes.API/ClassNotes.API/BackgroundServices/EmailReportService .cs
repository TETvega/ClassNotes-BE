using ClassNotes.API.Constants;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Database;
using ClassNotes.API.Hubs;
using ClassNotes.API.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Channels;
using ClassNotes.Models;
using MailKit.Security;
using MimeKit;
using ClassNotes.API.Services.Emails;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Emails;
using MimeKit.Text;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;

namespace ClassNotes.API.BackgroundServices
{
    public class LoggingBackgroundService : BackgroundService
    {
        //_messageQueue es el canal donde se guarda la cola de espera de peticiones, cada que se llama el endpoint correctamente, se añade una entrada a este canal
        //la cual estara compuesta por un EmailStudentListRequest y se ejecutara una vez para esa llamada...
        private readonly Channel<EmailStudentListRequest> _messageQueue;
        private readonly ILogger<EmailsService> _logger;
        private readonly List<SmtpAccountWrapper> _smtpAccounts;


        private class SmtpAccountWrapper
        {
            //Variables necesarias para el servicio de emails, tomadas de emailService...
            public SMTPAcountDto Account { get; }

            public SemaphoreSlim Semaphore { get; }


            public SmtpAccountWrapper(SMTPAcountDto account, int maxConcurrency)
            {
                Account = account;
                Semaphore = new SemaphoreSlim(maxConcurrency);
            }
        }

        public LoggingBackgroundService(Channel<EmailStudentListRequest> messageQueue, ILogger<EmailsService> logger, IConfiguration configuration)
        {
            //Variables necesarias para el servicio de emails...
            _messageQueue = messageQueue;
            _logger = logger;
            var smtpAccounts = configuration.GetSection("SmtpAccounts").Get<List<SMTPAcountDto>>()
                ?? new List<SMTPAcountDto>();

            if (smtpAccounts.Count == 0)
                throw new InvalidOperationException("No hay cuentas SMTP configuradas.");

            // 7 hilos máximos por cuenta SMTP
                _smtpAccounts = smtpAccounts
                .Select(account => new SmtpAccountWrapper(account, 7))
                .ToList();
        }

        //Dentro de este metodo se llevara el proceso en segundo plano...
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //Solo se ejecutara 1 vez por cada entrada en el canal, obteniendo la informacion de la misma, si no hay nada el servicio correra de fondo
           //pero solo estara esperando que si haya algo en el canal...
            await foreach (var message in _messageQueue.Reader.ReadAllAsync(stoppingToken))
            {
                SmtpAccountWrapper acquiredWrapper = null;

                //Variables sacadas del modelo que se envio a travez del canal...
                var centerEntity = message.centerEntity;
                var teacherEntity = message.teacherEntity;
                var courseEntity = message.courseEntity;
                var courseSettingEntity = message.courseSettingEntity;
                var students = message.students;
                var content = message.Content;

                //Si hay un error, que al menos lo ingrese el ilogger y haga print de el mismo...
                try
                {//Por cada entrada dentro de la lista estudiante dentro del modelo enviado...
                    foreach (var student in students)
                    {
                        // Generar PDF
                        var pdfBytes = await EmailsService.GenerateGradeReport(centerEntity, teacherEntity, courseEntity,
                            student.studentEntity, student.studentCourseEntity, courseSettingEntity, student.unitEntities);

                        // Construir el correo
                        var email = new MimeMessage();
                        email.To.Add(MailboxAddress.Parse(student.studentEntity.Email));
                        email.Subject = $"Tus calificaciones de {courseEntity.Name} {courseEntity.Section}";

                        // Adjuntar PDF
                        var body = new TextPart("plain") { Text = content };
                        var attachment = new MimePart("application", "pdf")
                        {
                            Content = new MimeContent(new MemoryStream(pdfBytes)),
                            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                            ContentTransferEncoding = ContentEncoding.Base64,
                            FileName = $"Calificaciones_{courseEntity.Name}_{courseEntity.Section}_{student.studentEntity.FirstName}{student.studentEntity.LastName}.pdf"
                        };

                        var multipart = new Multipart("mixed");
                        multipart.Add(body);
                        multipart.Add(attachment);
                        email.Body = multipart;

                        // Adquirir cuenta SMTP con balanceo de carga
                        acquiredWrapper = await AcquireAccountAsync();
                        email.From.Add(MailboxAddress.Parse(acquiredWrapper.Account.Username));

                        // Enviar el correo
                        using var smtp = new SmtpClient();
                        await smtp.ConnectAsync(
                            acquiredWrapper.Account.Host,
                            acquiredWrapper.Account.Port,
                            SecureSocketOptions.StartTls
                        );

                        await smtp.AuthenticateAsync(
                            acquiredWrapper.Account.Username,
                            acquiredWrapper.Account.Password
                        );

                        await smtp.SendAsync(email);
                        await smtp.DisconnectAsync(true);

                        Console.WriteLine("Email enviado");
                    }

                }
                catch (Exception ex)
                {

                    _logger.LogError($"Error enviando email con PDF: {ex.Message}");
                    Console.WriteLine(ex.ToString());
                }

            }
        }
        //Estos metodos fueron tomados de emailService, necesarios para hacer funcionar el servicio de enviar emails...
        private async Task<SmtpAccountWrapper> AcquireAccountAsync()
        {
            // Primero intentar adquirir inmediatamente
            foreach (var wrapper in _smtpAccounts)
            {
                if (await wrapper.Semaphore.WaitAsync(TimeSpan.Zero))
                {
                    return wrapper;
                }
            }

            // Si todas están ocupadas, esperar a cualquiera
            var waitTasks = _smtpAccounts.Select(w => w.Semaphore.WaitAsync()).ToArray();
            var completedTask = await Task.WhenAny(waitTasks);
            return _smtpAccounts[Array.IndexOf(waitTasks, completedTask)];
        }

        private async Task<ResponseDto<EmailDto>> SendWithAccount(SmtpAccountWrapper wrapper, EmailDto dto)
        {
            try
            {
                var email = new MimeMessage
                {
                    Subject = dto.Subject,
                    Body = new TextPart(TextFormat.Html) { Text = dto.Content }
                };
                email.From.Add(MailboxAddress.Parse(wrapper.Account.Username));
                email.To.Add(MailboxAddress.Parse(dto.To));

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(wrapper.Account.Host, wrapper.Account.Port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(wrapper.Account.Username, wrapper.Account.Password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                return new ResponseDto<EmailDto>
                {
                    StatusCode = 201,
                    Status = true,
                    Message = "Correo enviado exitosamente",
                    Data = dto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error enviando email con cuenta {wrapper.Account.Username}: {ex.Message}");
                return new ResponseDto<EmailDto>
                {
                    StatusCode = 500,
                    Status = false,
                    Message = $"Error temporal con la cuenta {wrapper.Account.Username}",
                    Data = dto
                };
            }
        }
    }
}
