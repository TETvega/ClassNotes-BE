using ClassNotes.API.Constants;
using ClassNotes.API.Database.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClassNotes.API.Database
{
	public class ClassNotesSeeder
	{
		public static async Task LoadDataAsync(
			ClassNotesContext context,
			ILoggerFactory loggerFactory,
			UserManager<UserEntity> userManager,
			RoleManager<IdentityRole> roleManager)
		{
			try
			{
				await LoadUsersAndRolesAsync(userManager, roleManager, loggerFactory);

				// AM: Aquí se cargarían los datos del SeedData que por ahora no se encuentran en la rama development

				//await LoadCentersAsync(context, loggerFactory);
				//await LoadStudentsAsync(context, loggerFactory);
				//await LoadCoursesAsync(context, loggerFactory);
			}
			catch (Exception ex)
			{
				var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
				logger.LogError(ex, "Error al inicializar la Data del API.");
			}
		}

		public static async Task LoadUsersAndRolesAsync(
			UserManager<UserEntity> userManager,
			RoleManager<IdentityRole> roleManager,
			ILoggerFactory loggerFactory)
		{
			try
			{
				if (!await roleManager.Roles.AnyAsync())
				{
					// AM: Creamos el único rol que manejaremos que sería USER (docente)
					await roleManager.CreateAsync(new IdentityRole(RolesConstant.USER));
				}

				if (!await userManager.Users.AnyAsync())
				{
					// AM: Creación del usuario de prueba
					var normalUser = new UserEntity
					{
						Id = "41e958ea-a9e3-4deb-bccb-e17a987164c7",
						Email = "jperez@me.com",
						UserName = "jperez@me.com",
						FirstName = "Juan",
						LastName = "Perez",
						//DefaultCourseSettingId = Guid.NewGuid(),
					};

					// AM: Aquí se asigna la contraseña "Temporal01*"
					await userManager.CreateAsync(normalUser, "Temporal01*");

					// AM: Aquí se aigna el rol al usuario
					await userManager.AddToRoleAsync(normalUser, RolesConstant.USER);
				}
			}
			catch (Exception e)
			{
				var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
				logger.LogError(e.Message);
			}
		}
	}
}
