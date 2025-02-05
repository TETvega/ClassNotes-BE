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

namespace ClassNotes.API.Services.Emails
{
	public class EmailsService : IEmailsService
	{
		private readonly IConfiguration _configuration;

		public EmailsService(IConfiguration configuration)
        {
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
		public async Task<ResponseDto<EmailDto>> SendEmailWithPdfAsync(EmailDto dto)
		{
			var email = new MimeMessage();
			email.From.Add(MailboxAddress.Parse(_configuration["Smtp:Username"]));
			email.To.Add(MailboxAddress.Parse(dto.To));
			email.Subject = dto.Subject;

			// AM: Crear el PDF en memoria
			// var pdfBytes = GeneratePdf();
			var pdfBytes = GenerateGradesReport("Juan Perez", "Anthony Miranda", DateTime.Now);

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
				FileName = "archivo.pdf"
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
				Message = "Correo enviado con PDF adjunto",
				Data = dto
			};
		}

		// AM: Ejemplo de como se podría generar un pdf para las calificaciones de estudiantes
		public static byte[] GenerateGradesReport(string teacher, string student, DateTime date)
		{
			using var stream = new MemoryStream();
			using var writer = new PdfWriter(stream);
			using var pdf = new PdfDocument(writer);
			var document = new Document(pdf);

			// Título
			var title = new Paragraph("Boletín de Calificaciones")
				.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
				.SetFontSize(16)
				.SetUnderline()
				//.SetFontColor(ColorConstants.BLUE)
				.SetTextAlignment(TextAlignment.CENTER);
			document.Add(title);

			// Datos generales
			document.Add(new Paragraph($"Docente: {teacher}\nEstudiante: {student}\nFecha: {date:dd 'de' MMMM 'de' yyyy}")
				.SetFontSize(12));

			// Tabla de calificaciones
			Table table = new Table(new float[] { 3, 1, 1, 1, 1, 1, 2 });
			table.SetWidth(UnitValue.CreatePercentValue(100));

			// Encabezado
			string[] headers = { "Clases", "U1", "U2", "U3", "U4", "Promedio", "Observación" };
			foreach (var header in headers)
			{
				table.AddHeaderCell(new Cell().Add(new Paragraph(header)));
			}

			// Datos ficticios (esto debería ser dinámico)
			string[,] data = {
			{ "Matemáticas", "87", "90", "87", "--", "88", "APR" },
			{ "Español", "88", "94", "84", "100", "91.5", "APR" },
			{ "Biología", "70", "80", "56", "45", "62.7", "REP" },
			{ "Música", "86", "79", "90", "75", "82.5", "APR" },
			{ "Física", "90", "92", "85", "91", "89.5", "APR" }
			};

			int rows = data.GetLength(0);
			int cols = data.GetLength(1);

			for (int i = 0; i < rows; i++)
			{
				for (int j = 0; j < cols; j++)
				{
					table.AddCell(new Cell().Add(new Paragraph(data[i, j])));
				}
			}


			document.Add(table);

			// Retroalimentación
			document.Add(new Paragraph("Retroalimentación:")
				//.SetBold()
				.SetUnderline()
				.SetFontSize(12)
				.SetMarginTop(10));
			document.Add(new Paragraph("El estudiante no se presentó a los últimos dos exámenes de biología. Se espera que en el siguiente parcial asista para no volver a reprobar la clase."));

			// Pie de página
			document.Add(new Paragraph("Este reporte fue brindado por la plataforma académica ClassNotes\nPara más información comunicarse a: classnotes-support@gmail.com")
				.SetFontSize(10)
				//.SetItalic()
				.SetTextAlignment(TextAlignment.CENTER)
				.SetMarginTop(20));

			document.Close();
			return stream.ToArray();
		}

		// AM: Función de prueba para generar un PDF en memoria
		private byte[] GeneratePdf()
		{
			using var stream = new MemoryStream();
			using var writer = new PdfWriter(stream);
			using var pdf = new PdfDocument(writer);
			var document = new Document(pdf);

			// AM: Agregar contenido al PDF
			document.Add(new Paragraph("Documento PDF generado"));
			document.Add(new Paragraph("Holaa este es el contenido del pdf generado desde el backend :)"));

			document.Close();
			return stream.ToArray();
		}
	}
}
