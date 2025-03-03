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
			/****** AM: Validar el OTP ingresado (OBSOLETO POR CAMBIO DE LÓGICA A PETICIÓN DE FRONTEND) ******/

			//var otpValidationResult = await _otpService.ValidateOtpAsync(new OtpValidateDto { Email = dto.Email, OtpCode = dto.OtpCode });
			//if (!otpValidationResult.Status)
			//{
			//	return new ResponseDto<UserDto>
			//	{
			//		StatusCode = 400,
			//		Status = false,
			//		Message = otpValidationResult.Message
			//	};
			//}

			// AM: Buscar al usuario por email
			//var user = await _userManager.FindByEmailAsync(dto.Email);
			//if (user is null)
			//{
			//	return new ResponseDto<UserDto>
			//	{
			//		StatusCode = 404,
			//		Status = false,
			//		Message = "El correo ingresado no está registrado."
			//	};
			//}

			// AM: Buscar al usuario por id
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
			if (user is null)
			{
				return new ResponseDto<UserDto>
				{
					StatusCode = 404,
					Status = false,
					Message = "El usuario ingresado no está registrado."
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

			// AM: Notificamos a la nueva dirección de correo sobre el cambio
			await _emailsService.SendEmailAsync(new EmailDto
			{
				To = dto.NewEmail,
				Subject = "Tu correo ha sido actualizado",
				Content = $@"
				<div style='font-family: Arial, sans-serif; text-align: center; padding: 20px; background-color: #f4f4f4;'>
					<div style='background-color: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 0 10px rgba(0, 0, 0, 0.2);'>
						<h2 style='color: #333;'>Correo Actualizado</h2>
						<p style='font-size: 16px; color: #555;'>¡Hola {userEntity.FirstName}!</p>
						<p style='font-size: 16px; color: #555;'>Tu correo electrónico ha sido actualizado correctamente.<br>Ahora utilizaremos la dirección de correo actual para los servicios que te ofrecemos en nuestra plataforma.</p>
						<p style='font-size: 14px; color: #777;'>Si no realizaste esta acción, por favor ponte en contacto con nuestro equipo de soporte en <a href='mailto:classnotes.service@gmail.com' style='color: #007BFF;'>classnotes.service@gmail.com</a>.</p>
						<p style='font-size: 14px; color: #777;'>Gracias por confiar en <strong>ClassNotes</strong>.</p>
					</div>
					<p style='font-size: 12px; color: #aaa; margin-top: 20px;'>© ClassNotes 2025 | Todos los derechos reservados</p>
				</div>"
			});

			// AM: Y también notificamos a la dirección de correo antigua sobre el cambio
			await _emailsService.SendEmailAsync(new EmailDto
			{
				To = userEntity.Email,
				Subject = "Tu correo ha sido actualizado",
				Content = $@"
				<div style='font-family: Arial, sans-serif; text-align: center; padding: 20px; background-color: #f4f4f4;'>
					<div style='background-color: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 0 10px rgba(0, 0, 0, 0.2);'>
						<h2 style='color: #333;'>Correo Actualizado</h2>
						<p style='font-size: 16px; color: #555;'>¡Hola {userEntity.FirstName}!</p>
						<p style='font-size: 16px; color: #555;'>Tu dirección de correo electrónico ha sido actualizada a <strong>{dto.NewEmail}</strong>.<br>Por lo tanto la dirección <strong>{userEntity.Email}</strong> dejará de ser utilizada en nuestra plataforma.</p>
						<p style='font-size: 14px; color: #777;'>Si no realizaste esta acción, por favor ponte en contacto con nuestro equipo de soporte en <a href='mailto:classnotes.service@gmail.com' style='color: #007BFF;'>classnotes.service@gmail.com</a> lo antes posible.</p>
						<p style='font-size: 14px; color: #777;'>Gracias por confiar en <strong>ClassNotes</strong>.</p>
					</div>
					<p style='font-size: 12px; color: #aaa; margin-top: 20px;'>© ClassNotes 2025 | Todos los derechos reservados</p>
				</div>"
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

		// AM: Función para eliminar el usuario
		public async Task<ResponseDto<UserDto>> DeleteAsync(string id)
		{
			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
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

					/*** AM: En esta parte se tienen que eliminar todos los registros relacionados al usuario ***/
					// AM: Eliminar entidades creadas por el usuario
					await _context.Centers.Where(c => c.TeacherId == id).ExecuteDeleteAsync();
					await _context.Students.Where(s => s.TeacherId == id).ExecuteDeleteAsync();
					await _context.Courses.Where(c => c.CreatedBy == id).ExecuteDeleteAsync();
					await _context.CoursesSettings.Where(cs => cs.CreatedBy == id).ExecuteDeleteAsync();

					// AM: Eliminar relaciones en tablas intermedias
					await _context.StudentsCourses.Where(sc => sc.CreatedBy == id).ExecuteDeleteAsync();
					await _context.Attendances.Where(a => a.CreatedBy == id).ExecuteDeleteAsync();
					await _context.StudentsActivitiesNotes.Where(sa => sa.CreatedBy == id).ExecuteDeleteAsync();

					// AM: Eliminar actividades y notas asociadas a cursos del usuario
					var userCourses = await _context.Courses.Where(c => c.CreatedBy == id).Select(c => c.Id).ToListAsync();
					await _context.Units.Where(a => userCourses.Contains(a.Id)).ExecuteDeleteAsync();
					await _context.CoursesNotes.Where(cn => userCourses.Contains(cn.CourseId)).ExecuteDeleteAsync();

					// AM: Notificar al correo
					await _emailsService.SendEmailAsync(new EmailDto
					{
						To = userEntity.Email,
						Subject = "Tu cuenta ha sido eliminada",
						Content = $@"
						<div style='font-family: Arial, sans-serif; text-align: center; padding: 20px; background-color: #f4f4f4;'>
							<div style='background-color: #ffffff; padding: 30px; border-radius: 10px; box-shadow: 0 0 10px rgba(0, 0, 0, 0.2);'>
								<h2 style='color: #333;'>Cuenta Eliminada</h2>
								<p style='font-size: 16px; color: #555;'>¡Hola {userEntity.FirstName}!</p>
								<p style='font-size: 16px; color: #555;'>Tu cuenta de <strong>ClassNotes</strong> ha sido eliminada correctamente.</p>
								<p style='font-size: 14px; color: #777;'>Si en algún momento decides volver, estaremos encantados de recibirte nuevamente.</p>
								<p style='font-size: 14px; color: #777;'>Mientras tanto, si necesitas asistencia o tienes alguna pregunta, no dudes en ponerte en contacto con nuestro equipo de soporte en <a href='mailto:classnotes.service@gmail.com' style='color: #007BFF;'>classnotes.service@gmail.com</a>.</p>
								<p style='font-size: 14px; color: #777;'>¡Gracias por haber sido parte de nuestra comunidad!</p>
							</div>
							<p style='font-size: 12px; color: #aaa; margin-top: 20px;'>© ClassNotes 2025 | Todos los derechos reservados</p>
						</div>"
					});

					// AM: Remover los roles del usuario
					var currentRoles = await _userManager.GetRolesAsync(userEntity);
					if (currentRoles.Any())
					{
						await _userManager.RemoveFromRolesAsync(userEntity, currentRoles);
					}

					// AM: Eliminar el usuario
					var result = await _userManager.DeleteAsync(userEntity);
					if (!result.Succeeded)
					{
						return new ResponseDto<UserDto>
						{
							StatusCode = 400,
							Status = false,
							Message = "No se pudo eliminar el usuario."
						};
					}

					// AM: Guardar cambios y confirmar la transacción
					await _context.SaveChangesAsync();
					await transaction.CommitAsync();

					return new ResponseDto<UserDto>
					{
						StatusCode = 200,
						Status = true,
						Message = MessagesConstant.DELETE_SUCCESS
					};
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
