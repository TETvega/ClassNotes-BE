using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Otp;
using MailKit.Security;
using MailKit.Net.Smtp;
using MimeKit;
using OtpNet;
using Microsoft.EntityFrameworkCore;

namespace ClassNotes.API.Services.Otp
{
	public class OtpService : IOtpService
	{
		private readonly ClassNotesContext _context;
		private readonly IConfiguration _configuration;

		// AM: Tiempo de expiración del codigo otp
		private readonly int _otpExpirationSeconds = 120; 

		public OtpService(ClassNotesContext context, IConfiguration configuration)
        {
			this._context = context;
			this._configuration = configuration;
		}

		// AM: Función para generar y enviar el codigo otp por correo
		public async Task<ResponseDto<OtpDto>> CreateAndSendOtpAsync(OtpCreateDto dto)
		{
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
			if (user == null)
			{
				return new ResponseDto<OtpDto>
				{
					StatusCode = 404,
					Status = false,
					Message = "El correo ingresado no está registrado.",
				};
			}

			// AM: Generar código OTP y actualizar usuario
			var otpCode = GenerateOtp(user.SecretKey);
			user.OtpCode = otpCode;
			user.OtpExpiration = DateTime.UtcNow.AddSeconds(_otpExpirationSeconds);

			// AM: Guardar cambios en la BD
			await _context.SaveChangesAsync(); 

			// AM: Generar el correo electronico que se va enviar con Smtp
			var email = new MimeMessage();
			email.From.Add(MailboxAddress.Parse(_configuration.GetSection("Smtp:Username").Value));
			email.To.Add(MailboxAddress.Parse(dto.Email));
			email.Subject = "Tu código OTP de verificación";
			email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
			{
				Text = $"<h2>Código OTP</h2>" +
					$"<p>Tu código de verificación es: <strong>{otpCode}</strong>" +
					$"</p><p>Este código expirará en 2 minutos.</p>"
			};

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

			return new ResponseDto<OtpDto>
			{
				StatusCode = 200,
				Status = true,
				Message = "Código OTP generado y enviado correctamente.",
				Data = new OtpDto
				{
					Email = dto.Email,
					OtpCode = otpCode,
					ExpirationSeconds = _otpExpirationSeconds
				}
			};
		}

		// AM: Función para validar codigos otp
		public async Task<ResponseDto<OtpDto>> ValidateOtpAsync(OtpValidateDto dto)
		{
			var user = await _context.Users.FirstOrDefaultAsync(u => u.OtpCode == dto.OtpCode);

			if (user == null)
			{
				return new ResponseDto<OtpDto>
				{
					StatusCode = 400,
					Status = false,
					Message = "El código OTP ingresado no es válido."
				};
			}

			if (user.OtpExpiration < DateTime.UtcNow)
			{
				return new ResponseDto<OtpDto>
				{
					StatusCode = 400,
					Status = false,
					Message = "El código OTP ingresado ya ha expirado."
				};
			}

			// AM: Limpiar el OTP después de validarlo
			user.OtpCode = null;
			user.OtpExpiration = null;
			await _context.SaveChangesAsync();

			return new ResponseDto<OtpDto>
			{
				StatusCode = 200,
				Status = true,
				Message = "Código OTP validado correctamente.",
			};
		}

		// AM: Generar codigo OTP basado en un secreto unico para cada usuario
		private string GenerateOtp(string secretKey)
		{
			var otpGenerator = new Totp(Base32Encoding.ToBytes(secretKey), step: _otpExpirationSeconds);
			return otpGenerator.ComputeTotp();
		}
	}
}
