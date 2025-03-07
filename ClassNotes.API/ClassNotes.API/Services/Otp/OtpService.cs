using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Otp;
using MailKit.Security;
using MailKit.Net.Smtp;
using MimeKit;
using OtpNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ClassNotes.API.Database.Entities;
using System.Security.Cryptography;
using System.Text;

namespace ClassNotes.API.Services.Otp
{
	public class OtpService : IOtpService
	{
		private readonly ClassNotesContext _context;
		private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;

        // AM: Tiempo de expiración del codigo otp (3 minutos)
        private readonly int _otpExpirationSeconds = 180; 

		public OtpService(ClassNotesContext context, IConfiguration configuration, IMemoryCache memoryCache)
        {
			this._context = context;
			this._configuration = configuration;
            this._memoryCache = memoryCache;
        }

		// AM: Función para generar y enviar el codigo otp por correo
		public async Task<ResponseDto<OtpGenerateResponseDto>> CreateAndSendOtpAsync(OtpCreateDto dto)
		{
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
			if (user == null)
			{
				return new ResponseDto<OtpGenerateResponseDto>
				{
					StatusCode = 404,
					Status = false,
					Message = "El correo ingresado no está registrado.",
				};
			}

			// CG: Si existe usuario, crear su secreto de Otp en base a su id y contraseña en base32
			var secretKey = GenerateSecretKey(user);

            // CG: Guardar el OTP en memoria
            var otpCode = GenerateOtp(secretKey);
            var cacheKey = $"OTP_{user.Email}";
            var otpData = new { Code = otpCode, Expiration = DateTime.UtcNow.AddSeconds(_otpExpirationSeconds) };
            
            _memoryCache.Set(cacheKey, otpData, TimeSpan.FromSeconds(_otpExpirationSeconds));

            // AM: Generar el correo electronico que se va enviar con Smtp
            var email = new MimeMessage();
			email.From.Add(MailboxAddress.Parse(_configuration.GetSection("Smtp:Username").Value));
			email.To.Add(MailboxAddress.Parse(dto.Email));
			email.Subject = "Tu código de verificación";
			email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
			{
				Text = $@"
				<div style='font-family: Arial, sans-serif; text-align: center; padding: 20px; background-color: #f4f4f4;'>
					<div style='background-color: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 0 10px rgba(0, 0, 0, 0.2);'>
						<h2 style='color: #333;'>Código de Verificación</h2>
						<p style='font-size: 14px; color: #555;'>Hola {user.FirstName}, este es tu código de verificación de un solo uso:</p>
						<div style='display: inline-block; padding: 10px 20px; font-size: 24px; color: #ffffff; background-color: #198F3D; border-radius: 5px; margin: 20px 0;'>
							<strong>{otpCode}</strong>
						</div>
						<p style='font-size: 14px; color: #777;'>Este código expirará en <strong>{_otpExpirationSeconds/60} minutos</strong>.</p>
						<p style='font-size: 12px; color: #999;'>Es importante que no compartas este código con nadie más.<br>Si no lo solicitaste, por favor ignora este mensaje.</p>
					</div>
					<p style='font-size: 12px; color: #aaa; margin-top: 20px;'>© ClassNotes 2025 | Todos los derechos reservados</p>
				</div>"
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

			return new ResponseDto<OtpGenerateResponseDto>
			{
				StatusCode = 200,
				Status = true,
				Message = "Código OTP generado y enviado correctamente.",
				Data = new OtpGenerateResponseDto
				{
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

			// AM: Obtener el usuario a partir del email para retornar el ID en la respuesta
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            return new ResponseDto<OtpDto>
			{
				StatusCode = 200,
				Status = true,
				Message = "Código OTP validado correctamente.",
				Data = new OtpDto
				{
					UserId = user.Id,
				}
			};
		}

		// AM: Generar codigo OTP basado en un secreto unico para cada usuario
		private string GenerateOtp(string secretKey)
		{
			var otpGenerator = new Totp(Base32Encoding.ToBytes(secretKey), step: _otpExpirationSeconds);
			return otpGenerator.ComputeTotp();
		}

        // CG: Generar SecretKey dinamicamente en base a la contraseña e id del usuario
        private string GenerateSecretKey(UserEntity user)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(user.PasswordHash));
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(user.Id.ToString()));


            return Base32Encoding.ToString(hashBytes);

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
					Data = null,
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
