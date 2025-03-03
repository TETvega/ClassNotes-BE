using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Emails;
using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Layout.Properties;
using ClassNotes.API.Database;
using Microsoft.EntityFrameworkCore;
using ClassNotes.API.Dtos.Otp;
using ClassNotes.API.Database.Entities;
using System.Threading.Tasks;
using iText.Kernel.Pdf.Canvas.Draw;

namespace ClassNotes.API.Services.Emails
{
	public class EmailsService : IEmailsService
	{
		private readonly ClassNotesContext _context;
		private readonly IConfiguration _configuration;

		public EmailsService(ClassNotesContext context, IConfiguration configuration)
        {
			this._context = context;
			this._configuration = configuration;
		}

		// AM: Función para enviar un correo redactado (Receptor, asunto y contenido)
		public async Task<ResponseDto<EmailDto>> SendEmailAsync(EmailDto dto)
		{
			var email = new MimeMessage();

			// AM: Obtenemos la dirección del correo emisor desde el appsettings
			email.From.Add(MailboxAddress.Parse(_configuration.GetSection("Smtp:Username").Value));

			// AM: Dirección del receptor
			email.To.Add(MailboxAddress.Parse(dto.To));

			// AM: Asunto del correo
			email.Subject = dto.Subject;

			// AM: Contenido del correo
			email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
			{
				Text = dto.Content,
			};

			// AM: Configuración del servidor Smtp para enviar el correo
			using var smtp = new SmtpClient();
			smtp.Connect(
				_configuration.GetSection("Smtp:Host").Value,
				Convert.ToInt32(_configuration.GetSection("Smtp:Port").Value),
				SecureSocketOptions.StartTls
				);
			smtp.Authenticate(
				_configuration.GetSection("Smtp:Username").Value,
				_configuration.GetSection("Smtp:Password").Value
				);

			smtp.Send(email);
			smtp.Disconnect(true);

			return new ResponseDto<EmailDto>
			{
				StatusCode = 201,
				Status = true,
				Message = "El correo fue enviado correctamente",
				Data = dto
			};
		}

		// AM: Función para enviar un correo con un PDF adjunto con iText7
		public async Task<ResponseDto<EmailDto>> SendEmailWithPdfAsync(EmailGradeDto dto)
		{
			// AM: Obtener y validar existencia de la clase
			var courseEntity = await _context.Courses.FirstOrDefaultAsync(c => c.Id == dto.CourseId);
			if (courseEntity is null)
			{
				return new ResponseDto<EmailDto>
				{
					StatusCode = 404,
					Status = false,
					Message = "El curso ingresado no está registrado.",
				};
			}

			// AM: Obtener y validar existencia del estudiante
			var studentEntity = await _context.Students.FirstOrDefaultAsync(s => s.Id == dto.StudentId);
			if (studentEntity is null)
			{
				return new ResponseDto<EmailDto>
				{
					StatusCode = 404,
					Status = false,
					Message = "El estudiante ingresado no está registrado.",
				};
			}

			// AM: Validar el registro del estudiante en la clase
			var studentCourseEntity = await _context.StudentsCourses.FirstOrDefaultAsync(sc => sc.CourseId == dto.CourseId && sc.StudentId == dto.StudentId);
			if (studentCourseEntity is null)
			{
				return new ResponseDto<EmailDto>
				{
					StatusCode = 404,
					Status = false,
					Message = "El estudiante ingresado no está registrado en la clase ingresada.",
				};
			}

			// AM: Obtener el centro
			var centerEntity = await _context.Centers.FirstOrDefaultAsync(c => c.Id == courseEntity.CenterId);
			// AM: Obtener el profesor
			var teacherEntity = await _context.Users.FirstOrDefaultAsync(t => t.Id == centerEntity.TeacherId);
			// AM: Obtener el CourseSetting del curso (para saber cual es el minimo para reprobar o aprobar en la clase)
			var courseSettingEntity = await _context.CoursesSettings.FirstOrDefaultAsync(cs => cs.Id == courseEntity.SettingId);



			// AM: Obtener las calificaciones por unidad del estudiante (TEMPORAL PARA PRUEBAS MIENTRAS KEN LO IMPLEMENTA)
			var unitsNotes = await _context.Units // Cambiar por StudentsUnits
				.Where(u => u.CourseId == courseEntity.Id) // Modificar para que compare con StudentCourseId
				.OrderBy(u => u.UnitNumber)
				.ToListAsync();



			// AM: Creamos el correo que se va a enviar
			var email = new MimeMessage();
			email.From.Add(MailboxAddress.Parse(_configuration["Smtp:Username"]));
			// AM: Aquí asignamos la dirección de email del estudiante
			email.To.Add(MailboxAddress.Parse(studentEntity.Email));
			// AM: Titulo del correo
			email.Subject = $"Tus calificaciones de {courseEntity.Name} {courseEntity.Section}";

			// AM: Creamos el PDF de calificaciones con los parametros necesarios
			var pdfBytes = await GenerateGradeReport(centerEntity, teacherEntity, courseEntity, studentEntity, studentCourseEntity, courseSettingEntity, unitsNotes);

			// AM: Crear el cuerpo del mensaje con el PDF adjunto
			var body = new TextPart("plain")
			{
				Text = dto.Content
			};

			// AM: Crear el adjunto en memoria
			var attachment = new MimePart("application", "pdf")
			{
				Content = new MimeContent(new MemoryStream(pdfBytes)),
				ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
				ContentTransferEncoding = ContentEncoding.Base64,
				FileName = $"Calificaciones_{courseEntity.Name}_{courseEntity.Section}_{studentEntity.FirstName}{studentEntity.LastName}.pdf"
			};

			var multipart = new Multipart("mixed");
			multipart.Add(body);
			multipart.Add(attachment);
			email.Body = multipart;

			// AM: Configurar el servidor SMTP y enviar el correo
			using var smtp = new SmtpClient();
			smtp.Connect(
				_configuration["Smtp:Host"],
				int.Parse(_configuration["Smtp:Port"]),
				SecureSocketOptions.StartTls
			);
			smtp.Authenticate(
				_configuration["Smtp:Username"],
				_configuration["Smtp:Password"]
			);

			smtp.Send(email);
			smtp.Disconnect(true);

			return new ResponseDto<EmailDto>
			{
				StatusCode = 201,
				Status = true,
				Message = $"Las calificaciones del estudiante {studentEntity.FirstName} {studentEntity.LastName} en la clase de {courseEntity.Name} fueron enviadas correctamente."
			};
		}

		// AM: Generar el pdf con las calificaciones del estudiante
		public static async Task<byte[]> GenerateGradeReport(CenterEntity center, UserEntity teacher, CourseEntity course, StudentEntity student, StudentCourseEntity studentCourse, CourseSettingEntity courseSetting, List<UnitEntity> unitsNotes)
		{
			// AM: Propiedades para redactar el documento PDF
			using var stream = new MemoryStream();
			using var writer = new PdfWriter(stream);
			using var pdf = new PdfDocument(writer);
			var document = new Document(pdf);
			var date = DateTime.Now;

			// AM: Linea horizontal
			document.Add(new LineSeparator(new SolidLine(2f))
				.SetWidth(UnitValue.CreatePercentValue(100))
				.SetMarginTop(5)
				.SetMarginBottom(5));

			// AM: Titulo
			var title = new Paragraph("Boletín de Calificaciones")
				.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
				.SetFontSize(16)
				.SetUnderline()
				.SetTextAlignment(TextAlignment.CENTER);
			document.Add(title);

			// AM: Subtitulo
			var subtitle = new Paragraph($"{center.Name}\n{center.Abbreviation}")
				.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
				.SetFontSize(14)
				.SetTextAlignment(TextAlignment.CENTER);
			document.Add(subtitle);

			// AM: Linea horizontal
			document.Add(new LineSeparator(new SolidLine(2f))
				.SetWidth(UnitValue.CreatePercentValue(100))
				.SetMarginTop(5)
				.SetMarginBottom(5));

			/****** AM: Datos generales ******/
			// AM: Nombre de la clase
			document.Add(new Paragraph()
				.Add(new Text("Clase: ")
				.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)))
				.Add(new Text(course.Name))
				.SetFontSize(12));
			// AM: Sección de la clase
			document.Add(new Paragraph()
				.Add(new Text("Sección: ")
				.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)))
				.Add(new Text(course.Section))
				.SetFontSize(12));
			// AM: Docente
			document.Add(new Paragraph()
				.Add(new Text("Docente: ")
				.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)))
				.Add(new Text($"{teacher.FirstName} {teacher.LastName}"))
				.SetFontSize(12));
			// AM: Estudiante
			document.Add(new Paragraph()
				.Add(new Text("Estudiante: ")
				.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)))
				.Add(new Text($"{student.FirstName} {student.LastName}"))
				.SetFontSize(12));
			// AM: Fecha actual
			document.Add(new Paragraph()
				.Add(new Text("Fecha: ")
				.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)))
				.Add(new Text($"{date:dd 'de' MMMM 'de' yyyy}"))
				.SetFontSize(12));

			// AM: Tabla de calificaciones
			Table table = new Table(new float[] { 1, 1, 1 });
			table.SetWidth(UnitValue.CreatePercentValue(100)).SetMarginTop(5);

			// ****** TODO: Solo falta hacer dinamica esta parte para mostrar las calificaciones por unidad del estudiante en la clase ******

			string[] headers = { "Parcial", "Nota", "Observación" };
			foreach (var header in headers)
			{
				table.AddHeaderCell(new Cell().Add(new Paragraph(header)
					.SetTextAlignment(TextAlignment.CENTER)
					.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))));
			}

			// Datos dinámicos
			foreach (var unit in unitsNotes)
			{
				// AM: Agregar el número del parcial
				table.AddCell(new Cell().Add(new Paragraph(unit.UnitNumber.ToString())
					.SetTextAlignment(TextAlignment.CENTER)));

				// AM: Agregar la nota del parcial
				table.AddCell(new Cell().Add(new Paragraph(unit.MaxScore.ToString())
					.SetTextAlignment(TextAlignment.CENTER)));

				// AM: Calcular si aprobó o reprobó
				string observation = unit.MaxScore >= courseSetting.MinimumGrade ? "APR" : "REP";
				var observationParagraph = new Paragraph(observation);
				observationParagraph.SetFontColor(observation == "APR" ? ColorConstants.GREEN : ColorConstants.RED);

				table.AddCell(new Cell().Add(observationParagraph)
					.SetTextAlignment(TextAlignment.CENTER)
					.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)));
			}

			document.Add(table);

			// *********************************************************************

			// AM: Promedio final
			if (studentCourse.FinalNote >= courseSetting.MinimumGrade) 
			{
				document.Add(new Paragraph($"Su promedio final es de {studentCourse.FinalNote}\n¡Felicidades, aprobó la clase!")
					.SetFontSize(12)
					.SetMarginTop(20)
					.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
					.SetTextAlignment(TextAlignment.CENTER));
			}
			else
			{
				document.Add(new Paragraph($"Su promedio final es de: {studentCourse.FinalNote}%\nUsted ha reprobado la clase, la nota minima para pasar era de {courseSetting.MinimumGrade}%")
					.SetFontSize(12)
					.SetMarginTop(20)
					.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
					.SetTextAlignment(TextAlignment.CENTER));
			}

			// AM: Linea horizontal
			document.Add(new LineSeparator(new SolidLine(2f))
				.SetWidth(UnitValue.CreatePercentValue(100))
				.SetMarginTop(10)
				.SetMarginBottom(5));

			// AM: Pie de pagina
			document.Add(new Paragraph("Este reporte fue brindado por la plataforma académica ClassNotes\nPara más información comunicarse a: classnotes.service@gmail.com")
				.SetFontSize(10)
				.SetTextAlignment(TextAlignment.CENTER)
				.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE))
				.SetMarginTop(15)
				.SetMarginBottom(15));

			// AM: Linea horizontal
			document.Add(new LineSeparator(new SolidLine(2f))
				.SetWidth(UnitValue.CreatePercentValue(100))
				.SetMarginTop(5)
				.SetMarginBottom(5));

			document.Close();
			return stream.ToArray();
		}
	}
}
