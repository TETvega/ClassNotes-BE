using ClassNotes.API.Constants;
using ClassNotes.API.Database.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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
                await LoadCoursesSettingsAsync(loggerFactory, context);
                await LoadCentersAsync(loggerFactory, context);
                await LoadCoursesAsync(loggerFactory, context);
                await LoadCourseNotesAsync(loggerFactory, context);
                await LoadTagsActivitiesAsync(loggerFactory, context);
                await LoadUnitsAsync(loggerFactory, context);
                await LoadActivitiesAsync(loggerFactory, context);
                await LoadStudentsAsync(loggerFactory, context);
                await LoadAttendancesAsync(loggerFactory, context);
                await LoadStudentsCoursesAsync(loggerFactory, context);
                await LoadStudentsActivitiesNotesAsync(loggerFactory, context);
                await LoadStudentsUnitsAsync(loggerFactory, context);
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
                logger.LogError(ex, "Error al inicializar la Data del API.");
            }
        }

        // AM: Cargar usuarios desde users.json
		public static async Task LoadUsersAndRolesAsync(UserManager<UserEntity> userManager, RoleManager<IdentityRole> roleManager, ILoggerFactory loggerFactory)
		{
			try
			{
				if (!await roleManager.Roles.AnyAsync())
				{
					// AM: Creamos el único rol que manejaremos que sería USER (docente)
					await roleManager.CreateAsync(new IdentityRole(RolesConstant.USER));
				}

                // JA: Cargar los usuarios desde el archivo JSON
                var jsonFilePath = "SeedData/users.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var usersFromFile = JsonConvert.DeserializeObject<List<UserEntity>>(jsonContent);

                if (usersFromFile != null && usersFromFile.Any())
                {
                    // JA: Iteramos sobre cada usuario del archivo JSON
                    foreach (var user in usersFromFile)
                    {
                        // JA: Verificar si el usuario ya existe en la base de datos
                        var existingUser = await userManager.FindByIdAsync(user.Id);
                        if (existingUser == null)
                        {
                            // JA: Si el usuario no existe, crear uno nuevo con la contraseña
                            var createUserResult = await userManager.CreateAsync(user, "Temporal01*");

                            if (createUserResult.Succeeded)
                            {
                                // JA: Asignar el rol USER al usuario recién creado
                                await userManager.AddToRoleAsync(user, RolesConstant.USER);
                            }
                            else
                            {
                                var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
                                foreach (var error in createUserResult.Errors)
                                {
                                    logger.LogError($"Error al crear usuario {user.UserName}: {error.Description}");
                                }
                            }
                        }
                    }
                }
            }
			catch (Exception e)
			{
				var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
				logger.LogError(e.Message);
			}
		}
	
        // AM: Cargar estudiantes desde students.json
        public static async Task LoadStudentsAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/students.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var students = JsonConvert.DeserializeObject<List<StudentEntity>>(jsonContent);

                foreach (var student in students)
                {
					// AM: Asignar a Juan Perez 
					var user = await context.Users.FirstOrDefaultAsync(u => u.Id == "41e958ea-a9e3-4deb-bccb-e17a987164c7");
                    bool exists = await context.Students.AnyAsync(s => s.Id == student.Id);
                    if (!exists)
                    {
                        student.CreatedDate = DateTime.Now;
                        student.UpdatedDate = DateTime.Now;
                        student.CreatedBy = user.Id;
                        student.UpdatedBy = user.Id;
                        await context.Students.AddAsync(student);
                    }
                }

                await context.SaveChangesWithoutAuditAsync();
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
                logger.LogError(e, "Error al ejecutar el Seed de Estudiantes");
            }
        }

		// AM: Cargar unidades de estudiantes desde students_units.json
		public static async Task LoadStudentsUnitsAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
		{
			try
			{
				var jsonFilePath = "SeedData/students_units.json";
				var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
				var studentsUnits = JsonConvert.DeserializeObject<List<StudentUnitEntity>>(jsonContent);

				foreach (var studentUnit in studentsUnits)
				{
					// AM: Asignar a Juan Perez
					var user = await context.Users.FirstOrDefaultAsync(u => u.Id == "41e958ea-a9e3-4deb-bccb-e17a987164c7"); 
					bool exists = await context.StudentsUnits.AnyAsync(t => t.Id == studentUnit.Id);
					if (!exists)
					{
						studentUnit.CreatedDate = DateTime.Now;
						studentUnit.UpdatedDate = DateTime.Now;
						studentUnit.CreatedBy = user.Id;
						studentUnit.UpdatedBy = user.Id;
						await context.StudentsUnits.AddAsync(studentUnit);
					}

					await context.SaveChangesWithoutAuditAsync();
				}
			}
			catch (Exception e)
			{
				var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
				logger.LogError(e, "Error al ejecutar el Seed de Unidades de Estudiantes");
			}
		}

		// JA: Cargar etiquetas desde tags_activities.json
		public static async Task LoadTagsActivitiesAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/tags_activities.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var tags = JsonConvert.DeserializeObject<List<TagActivityEntity>>(jsonContent);
                
                foreach (var tag in tags)
                {
                    // AM: Asignar a Juan Perez
                    var user = await context.Users.FirstOrDefaultAsync(u => u.Id == "41e958ea-a9e3-4deb-bccb-e17a987164c7");  
                    // JA: Verificar si la TagActivity ya existe
                    bool exists = await context.TagsActivities.AnyAsync(t => t.Id == tag.Id);
                    if (!exists)
                    {
                        tag.CreatedDate = DateTime.Now;
                        tag.UpdatedDate = DateTime.Now;
                        tag.CreatedBy = user.Id;
                        tag.UpdatedBy = user.Id;
                        await context.TagsActivities.AddAsync(tag);
                    }
                   
                    await context.SaveChangesWithoutAuditAsync();
                }
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
                logger.LogError(e, "Error al ejecutar el Seed de Tag Activity");
            }
        }

		// JA: Cargar unidades desde units.json
		public static async Task LoadUnitsAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/units.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var units = JsonConvert.DeserializeObject<List<UnitEntity>>(jsonContent);

                foreach (var unit in units)
                {
                    // AM: Asignar a Juan Perez
                    var user = await context.Users.FirstOrDefaultAsync(u => u.Id == "41e958ea-a9e3-4deb-bccb-e17a987164c7"); 
                    // JA: Verificar si la unidad ya existe
                    bool exists = await context.Units.AnyAsync(t => t.Id == unit.Id);
                    if (!exists)
                    {
                        unit.CreatedDate = DateTime.Now;
                        unit.UpdatedDate = DateTime.Now;
                        unit.CreatedBy = user.Id;
                        unit.UpdatedBy = user.Id;
                        await context.Units.AddAsync(unit);
                    }

                    await context.SaveChangesWithoutAuditAsync();
                }
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
                logger.LogError(e, "Error al ejecutar el Seed de Unidades");
            }
        }

		// DD: Cargar actividades desde activities.json
		public static async Task LoadActivitiesAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/activities.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var Activities = JsonConvert.DeserializeObject<List<ActivityEntity>>(jsonContent);

                foreach (var activity in Activities) 
                {
					// AM: Asignar a Juan Perez
					var user = await context.Users.FirstOrDefaultAsync(u => u.Id == "41e958ea-a9e3-4deb-bccb-e17a987164c7");
					bool exists = await context.Activities.AnyAsync(s => s.Id == activity.Id);
                    if (!exists) 
                    {
                        activity.CreatedBy = user.Id;
                        activity.UpdatedBy = user.Id;
                        activity.CreatedDate = DateTime.Now;
                        activity.UpdatedDate = DateTime.Now;
                        await context.Activities.AddAsync(activity);
                    }
                }

                await context.SaveChangesWithoutAuditAsync();
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
                logger.LogError(e, "Error al ejecutar el Seed de Actividades");
            }
        }

		// DD: Cargar centros desde centers.json
		public static async Task LoadCentersAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/centers.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var center = JsonConvert.DeserializeObject<List<CenterEntity>>(jsonContent);
                if (!await context.Centers.AnyAsync())
                {
					// AM: Asignar a Juan Perez
					var user = await context.Users.FirstOrDefaultAsync(u => u.Id == "41e958ea-a9e3-4deb-bccb-e17a987164c7");

					for (int i = 0; i < center.Count; i++)
                    {
                        center[i].CreatedBy = user.Id;
                        center[i].CreatedDate = DateTime.Now;
                        center[i].UpdatedBy = user.Id;
                        center[i].UpdatedDate = DateTime.Now;
                    }

                    await context.Centers.AddRangeAsync(center);
                    await context.SaveChangesWithoutAuditAsync();
                }
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesContext>();
                logger.LogError(e, "Error al ejecutar el Seed de Centros");
            }
        }

		// DD: Cargar clases desde courses.json
		public static async Task LoadCoursesAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/courses.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var courses = JsonConvert.DeserializeObject<List<CourseEntity>>(jsonContent);

                foreach (var course in courses) 
                {
					// AM: Asignar a Juan Perez
					var user = await context.Users.FirstOrDefaultAsync(u => u.Id == "41e958ea-a9e3-4deb-bccb-e17a987164c7");
					bool exist = await context.Courses.AnyAsync(s => s.Id == course.Id);
                    if (!exist) { 
                      course.CreatedBy = user.Id;
                        course.CreatedDate = DateTime.Now;
                        course.UpdatedBy = user.Id;
                        course.UpdatedDate = DateTime.Now;
                        await context.Courses.AddAsync(course);
                    }
                }

                await context.SaveChangesWithoutAuditAsync();
            }

            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesContext>();
                logger.LogError(e, "Error al ejecutar el Seed de Cursos");
            }
        }

		// DD: Cargar asistencias desde attendances.json
		public static async Task LoadAttendancesAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/attendances.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var attendances = JsonConvert.DeserializeObject<List<AttendanceEntity>>(jsonContent);
                
                foreach(var attendace in attendances)
                {
					// AM: Asignar a Juan Perez
					var user = await context.Users.FirstOrDefaultAsync(u => u.Id == "41e958ea-a9e3-4deb-bccb-e17a987164c7");
					bool exist = await context.Attendances.AnyAsync(s =>s.Id == attendace.Id);
                    if (!exist) 
                    {
                        attendace.CreatedBy = user.Id;
                        attendace.CreatedDate = DateTime.Now;
                        attendace.UpdatedBy = user.Id;
                        attendace.UpdatedDate = DateTime.Now;
                        await context.Attendances.AddAsync(attendace);
                    }
                }

                await context.SaveChangesWithoutAuditAsync();

            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesContext>();
                logger.LogError(e, "Error al ejecutar el Seed de Asistencia ");
            }
        }

		// DD: Cargar notas de curso desde course_notes.json
		public static async Task LoadCourseNotesAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/course_notes.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var course_notes = JsonConvert.DeserializeObject<List<CourseNoteEntity>>(jsonContent);

                foreach (var courseNote in course_notes)  
                {
					// AM: Asignar a Juan Perez
					var user = await context.Users.FirstOrDefaultAsync(u => u.Id == "41e958ea-a9e3-4deb-bccb-e17a987164c7");
					bool exist = await context.CoursesNotes.AnyAsync(s => s.Id == courseNote.Id);
                    if (!exist) 
                    {
                        courseNote.CreatedBy = user.Id;
                        courseNote.CreatedDate = DateTime.Now;
                        courseNote.UpdatedBy = user.Id;
                        courseNote.UpdatedDate = DateTime.Now;
                        await context.CoursesNotes.AddAsync(courseNote);
                    }
                }

                await context.SaveChangesWithoutAuditAsync();
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesContext>();
                logger.LogError(e, "Error al ejecutar el Seed de Notas del Curso");
            }
        }

		// DD: Cargar configuraciones de curso desde courses_settings.json
		public static async Task LoadCoursesSettingsAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/courses_settings.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var courseSettings = JsonConvert.DeserializeObject<List<CourseSettingEntity>>(jsonContent);

                foreach( var courseSetting in courseSettings) 
                {
					// AM: Asignar a Juan Perez
					var user = await context.Users.FirstOrDefaultAsync(u => u.Id == "41e958ea-a9e3-4deb-bccb-e17a987164c7");
					bool exists = await context.CoursesSettings.AnyAsync(s => s.Id == courseSetting.Id);
                    if (!exists) 
                    {
                        courseSetting.CreatedBy = user.Id;
                        courseSetting.CreatedDate = DateTime.Now;
                        courseSetting.UpdatedBy = user.Id;
                        courseSetting.UpdatedDate = DateTime.Now;
                       await context.CoursesSettings.AddAsync(courseSetting);
                    }
                }

                await context.SaveChangesWithoutAuditAsync();
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
                logger.LogError(e, "Error al ejecutar el Seed de Configuraciones de Curso");
            }
        }

		// DD: Cargar notas de actividades desde students_activities_notes.json
		public static async Task LoadStudentsActivitiesNotesAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/students_activities_notes.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var students_activities_notes = JsonConvert.DeserializeObject<List<StudentActivityNoteEntity>>(jsonContent);

                foreach (var students_activities_note in students_activities_notes) 
                {
					// AM: Asignar a Juan Perez
					var user = await context.Users.FirstOrDefaultAsync(u => u.Id == "41e958ea-a9e3-4deb-bccb-e17a987164c7");
					bool exists = await context.StudentsActivitiesNotes.AnyAsync(s => s.Id == students_activities_note.Id);
                    if (!exists) 
                    {
                        students_activities_note.CreatedBy = user.Id;
                        students_activities_note.CreatedDate = DateTime.Now;
                        students_activities_note.UpdatedBy = user.Id;
                        students_activities_note.UpdatedDate = DateTime.Now;
                        await context.StudentsActivitiesNotes.AddAsync(students_activities_note);
                    }
                }

                await context.SaveChangesWithoutAuditAsync();
            }
            catch (Exception e)
            {
                var logger = loggerFactory?.CreateLogger<ClassNotesContext>();
                logger.LogError(e, "Error al ejecutar el Seed de Notas de Actividades");
            }
        }

		// JA: Cargar tabla de relación entre estudiantes y cursos desde students_courses.json
		public static async Task LoadStudentsCoursesAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/students_courses.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var student_courses = JsonConvert.DeserializeObject<List<StudentCourseEntity>>(jsonContent);

                foreach (var student_course in student_courses)
                {
					// AM: Asignar a Juan Perez
					var user = await context.Users.FirstOrDefaultAsync(u => u.Id == "41e958ea-a9e3-4deb-bccb-e17a987164c7");
					bool exists = await context.StudentsCourses.AnyAsync(s => s.Id == student_course.Id);

                    if (!exists)
                    {
                        student_course.CreatedBy = user.Id;
                        student_course.CreatedDate = DateTime.Now;
                        student_course.UpdatedBy = user.Id;
                        student_course.UpdatedDate = DateTime.Now;

                        // JA: Realizamos este try para ver que campo no esta generando error y solucionarlo
                        try
                        {
                            await context.StudentsCourses.AddAsync(student_course);
                            await context.SaveChangesWithoutAuditAsync();  // JA: Guardar cada registro individualmente
                        }
                        catch (Exception dbEx)
                        {
                            // JA: Aqui mostramos el error
                            var logger = loggerFactory.CreateLogger<ClassNotesContext>();
                            logger.LogError(dbEx, $"Error al insertar StudentCourse ID: {student_course.Id}, StudentID: {student_course.StudentId}, CourseID: {student_course.CourseId}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesContext>();
                logger.LogError(e, "Error al ejecutar el Seeder de Cursos de Estudiantes");
            }
        }
    }
}
