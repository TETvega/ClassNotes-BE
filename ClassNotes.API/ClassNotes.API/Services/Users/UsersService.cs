using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Emails;
using ClassNotes.API.Dtos.Otp;
using ClassNotes.API.Dtos.Users;
using ClassNotes.API.Services.Emails;
using ClassNotes.API.Services.Otp;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClassNotes.API.Services.Users
{
	public class UsersService : IUsersService
	{
		private readonly ClassNotesContext _context;
		private readonly UserManager<UserEntity> _userManager;
		private readonly IMapper _mapper;
		private readonly ILogger<UsersService> _logger;
		private readonly IOtpService _otpService;
		private readonly IEmailsService _emailsService;

		public UsersService(
			ClassNotesContext context, 
			UserManager<UserEntity> userManager, 
			IMapper mapper, 
			ILogger<UsersService> logger,
			IOtpService otpService,
			IEmailsService emailsService
			)
        {
			this._context = context;
			this._userManager = userManager;
			this._mapper = mapper;
			this._logger = logger;
			this._otpService = otpService;
			this._emailsService = emailsService;
		}

		// AM: Función para editar información del usuario (nombre completo)
		public async Task<ResponseDto<UserDto>> EditAsync(UserEditDto dto, string id)
		{
			// AM: Obtener usuario y validar su existencia
			var userEntity = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
			if (userEntity is null)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 404,
					Status = false,
					Message = MessagesConstant.RECORD_NOT_FOUND
				};
			}

			// AM: Actualizar los datos del usuario
			userEntity.FirstName = dto.FirstName;
			userEntity.LastName = dto.LastName;

			// AM: Guardar los cambios
			var result = await _userManager.UpdateAsync(userEntity);
			await _context.SaveChangesAsync();

			if (!result.Succeeded)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 400,
					Status = false,
					Message = MessagesConstant.UPDATE_ERROR
				};
			}

			// AM: Mapear Entity a Dto para la respuesta
			var userDto = _mapper.Map<UserDto>(userEntity);

			return new ResponseDto<UserDto>
			{
				StatusCode = 200,
				Status = true,
				Message = MessagesConstant.UPDATE_SUCCESS,
				Data = userDto
			};
		}

		// AM: Función para cambiar la contraseña ingresando la actual
		public async Task<ResponseDto<UserDto>> ChangePasswordAsync(UserEditPasswordDto dto, string id)
		{
			// AM: Obtener usuario y validar su existencia
			var userEntity = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
			if (userEntity is null)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 404,
					Status = false,
					Message = MessagesConstant.RECORD_NOT_FOUND
				};
			}

			// AM: Validar que la contraseña ingresada coincide con la contraseña actual
			var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(userEntity, dto.CurrentPassword);
			if (!isCurrentPasswordValid)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 400,
					Status = false,
					Message = "La contraseña actual es incorrecta."
				};
			}

			// AM: Actualizar la contraseña
			var passwordChangeResult = await _userManager.ChangePasswordAsync(userEntity, dto.CurrentPassword, dto.NewPassword);
			if (!passwordChangeResult.Succeeded)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 400,
					Status = false,
					Message = "No se pudo cambiar la contraseña."
				};
			}

			// AM: Mapear Entity a Dto para la respuesta
			var userDto = _mapper.Map<UserDto>(userEntity);

			return new ResponseDto<UserDto>
			{
				StatusCode = 200,
				Status = true,
				Message = "La contraseña fue actualizada satisfactoriamente.",
				Data = userDto
			};
		}

		// AM: Función para cambiar la contraseña mediante validación OTP
		public async Task<ResponseDto<UserDto>> ChangePasswordWithOtpAsync(UserEditPasswordOtpDto dto)
		{
			// AM: Validar el OTP ingresado
			var otpValidationResult = await _otpService.ValidateOtpAsync(new OtpValidateDto { Email = dto.Email, OtpCode = dto.OtpCode });

			if (!otpValidationResult.Status)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 400,
					Status = false,
					Message = otpValidationResult.Message
				};
			}

			// AM: Buscar al usuario por email
			var user = await _userManager.FindByEmailAsync(dto.Email);
			if (user is null)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 404,
					Status = false,
					Message = "El correo ingresado no está registrado."
				};
			}

			// AM: Cambiar la contraseña sin necesidad de la actual
			var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
			var passwordChangeResult = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);

			if (!passwordChangeResult.Succeeded)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 400,
					Status = false,
					Message = "No se pudo cambiar la contraseña."
				};
			}

			// AM: Mapear Entity a Dto para la respuesta
			var userDto = _mapper.Map<UserDto>(user);

			return new ResponseDto<UserDto>
			{
				StatusCode = 200,
				Status = true,
				Message = "La contraseña fue actualizada satisfactoriamente.",
				Data = userDto
			};
		}

		// AM: Función para cambiar el correo electrónico
		public async Task<ResponseDto<UserDto>> ChangeEmailAsync(UserEditEmailDto dto, string id)
		{
			// AM: Obtener usuario y validar su existencia
			var userEntity = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
			if (userEntity is null)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 404,
					Status = false,
					Message = MessagesConstant.RECORD_NOT_FOUND
				};
			}

			// AM: Validar que el nuevo correo electrónico no este registrado
			var existingEmail = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == dto.NewEmail);
			if (existingEmail is not null)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 400,
					Status = false,
					Message = "El correo electrónico ingresado ya está registrado."
				};
			}

			// AM: Notificar al nuevo y antiguo correo
			await _emailsService.SendEmailAsync(new EmailDto
			{
				To = dto.NewEmail,
				Subject = "Correo Actualizado",
				Content = $"Hola {userEntity.FirstName}! Tu correo electrónico ha sido actualizado correctamente."
			});
			await _emailsService.SendEmailAsync(new EmailDto
			{
				To = userEntity.Email,
				Subject = "Correo Actualizado",
				Content = $"Tu dirección de correo electrónico fue actualizada a {dto.NewEmail} Si tu no realizaste este cambio, contacta a soporte."
			});

			// AM: Actualizar el nuevo correo
			var token = await _userManager.GenerateChangeEmailTokenAsync(userEntity, dto.NewEmail);
			var result = await _userManager.ChangeEmailAsync(userEntity, dto.NewEmail, token);
			if (!result.Succeeded)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 400,
					Status = false,
					Message = "No se pudo cambiar el correo electrónico."
				};
			}

			// AM: Actualizar el username del correo
			userEntity.UserName = dto.NewEmail;
			userEntity.NormalizedEmail = _userManager.NormalizeEmail(dto.NewEmail);
			await _userManager.UpdateAsync(userEntity);

			// AM: Mapear Entity a Dto para la respuesta
			var userDto = _mapper.Map<UserDto>(userEntity);

			return new ResponseDto<UserDto>
			{
				StatusCode = 200,
				Status = true,
				Message = "El correo electrónico fue actualizado satisfactoriamente.",
				Data = userDto
			};
		}

		// AM: Función para borrar el usuario
		public async Task<ResponseDto<UserDto>> DeleteAsync(string id)
		{
			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					throw new NotImplementedException();
				}
				catch (Exception ex)
				{
					await transaction.RollbackAsync();
					_logger.LogError(ex, MessagesConstant.DELETE_ERROR);
					return new ResponseDto<UserDto>
					{
						StatusCode = 500,
						Status = false,
						Message = MessagesConstant.DELETE_ERROR
					};
				}
			}
		}
	}
}
