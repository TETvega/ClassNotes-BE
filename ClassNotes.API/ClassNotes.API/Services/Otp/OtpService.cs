using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Otp;
using MailKit.Security;
using MailKit.Net.Smtp;
using MimeKit;
using OtpNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClassNotes.API.Services.Otp
{
	public class OtpService : IOtpService
	{
		private readonly ClassNotesContext _context;
		private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;

        // AM: Tiempo de expiración del codigo otp
        private readonly int _otpExpirationSeconds = 120; 

		public OtpService(ClassNotesContext context, IConfiguration configuration, IMemoryCache memoryCache)
        {
			this._context = context;
			this._configuration = configuration;
            this._memoryCache = memoryCache;
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

			// CG: Guardar el OTP en memoria
			var otpCode = GenerateOtp(user.SecretKey);
            var cacheKey = $"OTP_{user.Email}";
            var otpData = new { Code = otpCode, Expiration = DateTime.UtcNow.AddSeconds(_otpExpirationSeconds) };
            
            _memoryCache.Set(cacheKey, otpData, TimeSpan.FromSeconds(_otpExpirationSeconds));

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
            //Verificar OTP desde memoria
            var cacheKey = $"OTP_{dto.Email}";

            if (!_memoryCache.TryGetValue(cacheKey, out dynamic otpData))
            {
                return new ResponseDto<OtpDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "El código OTP ingresado no es válido o ha expirado."
                };
            }

            if (otpData.Code != dto.OtpCode)
            {
                return new ResponseDto<OtpDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "El código OTP ingresado no es válido."
                };
            }

            // CG: Eliminar OTP después de validarlo
            _memoryCache.Remove(cacheKey);

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

		// CG: Este metodo solo sirve para verificar que la cache esta siendo limpiada tras usar o expirar un OTP
		public async Task<ResponseDto<OtpDto>> GetCachedOtpAsync(string email)
		{

            var cacheKey = $"OTP_{email}";

			if(_memoryCache.TryGetValue(cacheKey, out dynamic otpData))
			{
				return new ResponseDto<OtpDto>
				{
					Message = "OTP encontrando en caché",
					Data = new OtpDto 
					{ 
						Email = email, 
						OtpCode = otpData.Code,
                        // ExpirationSeconds = otpData.Expiration
                        // ExpirationSeconds es entero y Expiration es DateTime
                    },
					Status = true,
					StatusCode = 200,
				};
			}

            return new ResponseDto<OtpDto>
            {
                StatusCode = 404,
                Status = false,
                Message = "OTP no encontrado o expirado",
            };
        }

    }
}
