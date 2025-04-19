
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
    public class EmailFeedBackService : BackgroundService
    {
        //_messageQueue es el canal donde se guarda la cola de espera de peticiones, cada que se llama el endpoint correctamente, se añade una entrada a este canal
        //la cual estara compuesta por un EmailStudentListRequest y se ejecutara una vez para esa llamada...
        private readonly Channel<EmailFeedBackRequest> _emailQueue;
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

        public EmailFeedBackService(Channel<EmailFeedBackRequest> emailQueue, IConfiguration configuration)
        {
            //Variables necesarias para el servicio de emails...
            _emailQueue = emailQueue;
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
            await foreach (var message in _emailQueue.Reader.ReadAllAsync(stoppingToken))
            {
                SmtpAccountWrapper acquiredWrapper = null;
                //Variables sacadas del modelo que se envio a travez del canal...
                var teacherEntity = message.TeacherEntity;
                var students = message.Students;
                var activity = message.ActivityEntity;

                ////Si hay un error, que al menos lo ingrese el ilogger y haga print de el mismo...
                try
                {//Por cada entrada dentro de la lista estudiante dentro del modelo enviado...
                    foreach (var student in students)
                    {

                        Console.WriteLine("hi");

                        var email = new MimeMessage
                        {
                            Subject = $"📝 {student.Name}, ya tienes comentarios sobre tu Actividad: {activity.Name}",
                            Body = new TextPart(TextFormat.Html) { Text = @"
                                        <div style='font-family: Arial, sans-serif; text-align: center;'>
                                        <h2 style='color: #4A90E2;'>📘 Hola " + student.Name + @",</h2>
                                        <p style='font-size: 16px; color: #333;'>
                                        El docente <strong>" + teacherEntity.FirstName + @"</strong> ha dejado comentarios y calificación 
                                        para tu tarea <strong>" + activity.Name + @"</strong>.
                                        </p>
                                        <div style='display: inline-block; background: #EAF3FF; padding: 15px; border-radius: 8px; 
                                        font-size: 16px; font-weight: normal; text-align: left; max-width: 500px; margin: 0 auto;'>
                                        <strong>📝 Comentario:</strong><br>
                                        " + student.FeedBack + @"
                                        </div>
                                        <div style='display: inline-block; background: #D4EDDA; color: #155724; padding: 15px; border-radius: 8px; 
                                            font-size: 18px; font-weight: bold; margin-top: 20px;'>
                                             🎯 Calificación obtenida: " + student.Score + @" / " + activity.MaxScore + @"
                                        </div>
                                         </div>" }
                        };
                        acquiredWrapper = await AcquireAccountAsync();
                        Console.WriteLine(acquiredWrapper.Account.Username);
                        email.From.Add(MailboxAddress.Parse(acquiredWrapper.Account.Username));
                        email.To.Add(MailboxAddress.Parse(student.Email));

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

                        await Task.Delay(15000);
                        Console.WriteLine("Email enviado");

                    }

                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.ToString());
                }

            }
        }


        public async Task<ResponseDto<EmailDto>> SendEmailAsync(EmailDto dto)
        {                Console.WriteLine("testing");
            var acquiredWrapper = await AcquireAccountAsync();

            try
            {

                return await SendWithAccount(acquiredWrapper, dto);
            }
            finally
            {
                acquiredWrapper.Semaphore.Release();
            }
        }

        private async Task<SmtpAccountWrapper> AcquireAccountAsync()
        {
            // Primero intentar adquirir inmediatamente
            foreach (var wrapper in _smtpAccounts)
            {;
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


                Console.WriteLine("Email enviado");
                return null;
            }

                
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());
                return null;
            }
}

    }
}


