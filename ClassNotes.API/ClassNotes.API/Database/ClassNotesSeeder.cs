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
                await LoadCourseSenttingsAsync(loggerFactory, context);
                await LoadCenterAsync(loggerFactory, context);
                await LoadCourseAsync(loggerFactory, context);
                await LoadCourseNotesAsync(loggerFactory, context);
                await LoadTagActivityAsync(loggerFactory, context);
                await LoadUnitAsync(loggerFactory, context);
                await LoadActivitiesAsync(loggerFactory, context);
                await LoadStudentsAsync(loggerFactory, context);
                await LoadAttendanceAsync(loggerFactory, context);
                await LoadStudentCourseAsync(loggerFactory, context);
                await LoadStudentsActivitesNotesAsync(loggerFactory, context);

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
	
        // AM: Aqui se llama el json para cargar los Estudiantes 
        public static async Task LoadStudentsAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/students.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var students = JsonConvert.DeserializeObject<List<StudentEntity>>(jsonContent);

                // Verificar si los estudiantes son null o vacíos
                foreach (var student in students)
                {
                    var user = await context.Users.FirstOrDefaultAsync();
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

                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
                logger.LogError(e, "Error al ejecutar el Seed de Estudiantes");
            }
        }

        // JA: Cargamos las TagasActivity
        public static async Task LoadTagActivityAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/tag_activity.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var tags = JsonConvert.DeserializeObject<List<TagActivityEntity>>(jsonContent);
                
                    foreach (var tag in tags)
                    {
                        // JA: Asignamos el usuario creador, en este caso el primero en la lista
                        var user = await context.Users.FirstOrDefaultAsync();
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
                   
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
                logger.LogError(e, "Error al ejecutar el Seed de Tag Activity");
            }
        }

        // JA: Cargamos la unidad
        public static async Task LoadUnitAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/unit.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var units = JsonConvert.DeserializeObject<List<UnitEntity>>(jsonContent);

                    foreach (var unit in units)
                    {
                        // JA: Asignamos el usuario creador, en este caso el primero en la lista
                        var user = await context.Users.FirstOrDefaultAsync();
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

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
                logger.LogError(e, "Error al ejecutar el Seed de Unidades");
            }
        }


        // DD : Aqui se llama el json de Actividades 
        public static async Task LoadActivitiesAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/activities.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var Activities = JsonConvert.DeserializeObject<List<ActivityEntity>>(jsonContent);
              foreach (var activity in Activities) 
                {
                    var user = await context.Users.FirstOrDefaultAsync();
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
              await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
                logger.LogError(e, "Error al ejecutar el Seed de Actividades");
            }
        }
        // DD: Aqui se llama el json de Center
        public static async Task LoadCenterAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/centers.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var center = JsonConvert.DeserializeObject<List<CenterEntity>>(jsonContent);
                if (!await context.Centers.AnyAsync())
                {
                    var user = await context.Users.FirstOrDefaultAsync();

                    for (int i = 0; i < center.Count; i++)
                    {
                        center[i].CreatedBy = user.Id;
                        center[i].CreatedDate = DateTime.Now;
                        center[i].UpdatedBy = user.Id;
                        center[i].UpdatedDate = DateTime.Now;
                    }

                    await context.Centers.AddRangeAsync(center);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesContext>();
                logger.LogError(e, "Error al ejecutar el Seed de Centros");
            }
        }
        //DD: Aqui se llama el json de Cursos 
        public static async Task LoadCourseAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/courses.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var courses = JsonConvert.DeserializeObject<List<CourseEntity>>(jsonContent);
                foreach (var course in courses) 
                {
                    var user = await context.Users.FirstOrDefaultAsync();
                    bool exist = await context.Courses.AnyAsync(s => s.Id == course.Id);
                    if (!exist) { 
                      course.CreatedBy = user.Id;
                        course.CreatedDate = DateTime.Now;
                        course.UpdatedBy = user.Id;
                        course.UpdatedDate = DateTime.Now;
                        await context.Courses.AddAsync(course);
                    }
                }
                await context.SaveChangesAsync();
            }

            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesContext>();
                logger.LogError(e, "Error al ejecutar el Seed de Cursos");
            }
        }

       
        
        //DD: Aqui se llama el json de Asistencias 
        public static async Task LoadAttendanceAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/attendance.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var attendances = JsonConvert.DeserializeObject<List<AttendanceEntity>>(jsonContent);
              foreach(var attendace in attendances)
                {
                    var user = await context.Users.FirstOrDefaultAsync();
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
                await context.SaveChangesAsync();

            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesContext>();
                logger.LogError(e, "Error al ejecutar el Seed de Asistencia ");
            }
        }
        //DD: Aqui se llama el json de Notas de Cursos 

        public static async Task LoadCourseNotesAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/course_notes.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var course_notes = JsonConvert.DeserializeObject<List<CourseNoteEntity>>(jsonContent);
                foreach (var courseNote in course_notes)  
                {
                    var user = await context.Users.FirstOrDefaultAsync();
                    
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
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesContext>();
                logger.LogError(e, "Error al ejecutar el Seed de Notas del Curso");
            }
        }

        //DD: Aqui se llama el json de Ajustes de los Cursos 

        public static async Task LoadCourseSenttingsAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/courses_settings.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var courseSettings = JsonConvert.DeserializeObject<List<CourseSettingEntity>>(jsonContent);

                foreach( var courseSetting in courseSettings) 
                {
                    var user = await context.Users.FirstOrDefaultAsync();
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
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<ClassNotesSeeder>();
                logger.LogError(e, $"Error al ejecutar el Seed de Configuraciones de Curso: {e.Message}\n{e.StackTrace}");
            }
        }


        // DD: Aqui se llama el json de Notas de Actividades 
        public static async Task LoadStudentsActivitesNotesAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/students_activities_notes.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var students_activities_notes = JsonConvert.DeserializeObject<List<StudentActivityNoteEntity>>(jsonContent);
                foreach (var students_activities_note in students_activities_notes) 
                {
                    var user = await context.Users.FirstOrDefaultAsync();
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
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                var logger = loggerFactory?.CreateLogger<ClassNotesContext>();
                logger.LogError(e, "Error al ejecutar el Seed de Notas de Actividades");
            }
        }
        //DD: Aqui se llama el json de Curso de Estudiantes 
        public static async Task LoadStudentCourseAsync(ILoggerFactory loggerFactory, ClassNotesContext context)
        {
            try
            {
                var jsonFilePath = "SeedData/students_courses.json";
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var student_courses = JsonConvert.DeserializeObject<List<StudentCourseEntity>>(jsonContent);

                foreach (var student_course in student_courses)
                {
                    var user = await context.Users.FirstOrDefaultAsync();
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
                            await context.SaveChangesAsync();  // JA: Guardar cada registro individualmente
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
