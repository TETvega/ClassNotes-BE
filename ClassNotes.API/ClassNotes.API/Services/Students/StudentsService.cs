using AutoMapper;
using Azure;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.Emails;
using ClassNotes.API.Dtos.Students;
using ClassNotes.API.Services.Audit;
using ClassNotes.API.Services.Emails;
using iText.Commons.Actions.Contexts;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClassNotes.API.Services.Students
{
    public class StudentsService : IStudentsService
    {
        private readonly ClassNotesContext classNotesContext_;
        private readonly IMapper mapper_;
        private readonly IEmailSender email;
        private readonly IAuditService _auditService;
        private readonly int PAGE_SIZE;
        private readonly IEmailsService _emailsService;

        public StudentsService(ClassNotesContext classNotesContext, IAuditService auditService, IMapper mapper, IConfiguration configuration, IEmailsService emailsService)
        {
            classNotesContext_ = classNotesContext;
            mapper_ = mapper;
            _auditService = auditService;
            this._emailsService = emailsService;
            PAGE_SIZE = configuration.GetValue<int>("PageSize");
        }

        public async Task<ResponseDto<PaginationDto<List<StudentDto>>>> GetStudentsListAsync(string searchTerm = "", int page = 1)
        {
            // Calculamos el Indice de inicio para la paginaciOn
            int startIndex = (page - 1) * PAGE_SIZE;

            // Necesitamos obtener el i de quien hace la petición
            var userId = _auditService.GetUserId();

            // Obtenemos la consulta base de los estudiantes registrados en la base de datos
            var studentEntityQuery = classNotesContext_.Students
                .Where(x => x.TeacherId == userId).AsQueryable(); // Solo incluimos estudiantes creados por quien hace la petición

            // Si se proporciona un termino de busqueda, filtramos los estudiantes por nombre
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower(); // Convertimos a minusculas
                studentEntityQuery = studentEntityQuery.Where(x =>
                    (x.FirstName + " " + x.LastName).ToLower().Contains(searchTerm) // podemos buscar por nombre completo
                );
            }

            // Obtenemos el total de estudiantes despues de filtrar
            int totalStudents = await studentEntityQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalStudents / PAGE_SIZE);
            var studentEntity = await studentEntityQuery
                .OrderByDescending(x => x.CreatedDate) // Ordenamos por fecha de creación en orden descendente
                .Skip(startIndex) // Omitimos los registros de páginas anteriores
                .Take(PAGE_SIZE) // Tomamos solo la cantidad definida por PAGE_SIZE
                .ToListAsync();

            // Mapeamos las entidades obtenidas a DTOs
            var studentsDtos = mapper_.Map<List<StudentDto>>(studentEntity);

            // Retornamos la respuesta con los datos paginados
            return new ResponseDto<PaginationDto<List<StudentDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.STU_RECORD_FOUND,
                Data = new PaginationDto<List<StudentDto>>
                {
                    CurrentPage = page,
                    PageSize = PAGE_SIZE,
                    TotalItems = totalStudents,
                    TotalPages = totalPages,
                    Items = studentsDtos,
                    HasPreviousPage = page > 1, // Indica si hay una página anterior disponible
                    HasNextPage = page < totalPages, // Indica si hay una página siguiente disponible
                }
            };
        }

        // EG -> Obtener estudiante por Id

        public async Task<ResponseDto<StudentDto>> GetStudentByIdAsync(Guid id)
        {
            // Necesitamos obtener el i de quien hace la petición
            var userId = _auditService.GetUserId();

            var studentEntity = await classNotesContext_.Students.FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == userId);

            if (studentEntity == null)
            {
                return new ResponseDto<StudentDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.STU_RECORD_NOT_FOUND
                };
            }

            var studentDto = mapper_.Map<StudentDto>(studentEntity);

            return new ResponseDto<StudentDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.STU_RECORD_FOUND,
                Data = studentDto
            };

        }

        public async Task<ResponseDto<StudentResultDto>> CreateStudentAsync(StudentCreateDto studentCreateDto, bool strictMode)
        {
            // Declarar las listas
            var successfulStudents = new List<StudentDto>();      // Lista de estudiantes registrados correctamente
            var duplicateStudents = new List<StudentDto>();       // Lista de estudiantes no ingresados por duplicación
            var modifiedEmailStudents = new List<StudentDto>();   // Lista de estudiantes cuyo correo fue modificado

            // Validar si el correo ya existe en la base de datos
            var existingStudent = await classNotesContext_.Students
                .FirstOrDefaultAsync(s => s.Email == studentCreateDto.Email);

            if (existingStudent != null)
            {
                // Si los nombres coinciden, retornar el estudiante existente
                if (existingStudent.FirstName == studentCreateDto.FirstName &&
                    existingStudent.LastName == studentCreateDto.LastName)
                {
                    duplicateStudents.Add(mapper_.Map<StudentDto>(existingStudent));
                    return new ResponseDto<StudentResultDto>
                    {
                        StatusCode = 200,
                        Status = true,
                        Message = MessagesConstant.STUDENT_EXISTS,
                        Data = new StudentResultDto
                        {
                            SuccessfulStudents = successfulStudents,
                            DuplicateStudents = duplicateStudents,
                            ModifiedEmailStudents = modifiedEmailStudents
                        }
                    };
                }

                // Si strictMode está activado, no se permite modificar el correo y se devuelve un error
                if (strictMode)
                {
                    duplicateStudents.Add(mapper_.Map<StudentDto>(existingStudent)); // Agregar a la lista de duplicados
                    return new ResponseDto<StudentResultDto>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = MessagesConstant.EMAIL_DIFFERENT_NAMES,
                        Data = new StudentResultDto
                        {
                            SuccessfulStudents = successfulStudents,
                            DuplicateStudents = duplicateStudents,
                            ModifiedEmailStudents = modifiedEmailStudents
                        }
                    };
                }

                // Si strictMode es false, se genera un nuevo correo electrónico para evitar duplicados agregando +m
                string baseEmail = studentCreateDto.Email.Split('@')[0];
                string domain = studentCreateDto.Email.Split('@')[1];
                int counter = 1;
                string newEmail = studentCreateDto.Email;

                while (await classNotesContext_.Students.AnyAsync(s => s.Email == newEmail))
                {
                    newEmail = $"{baseEmail}+{counter}@{domain}";
                    counter++;
                }

                studentCreateDto.Email = newEmail;
                modifiedEmailStudents.Add(mapper_.Map<StudentDto>(existingStudent));
            }

            // Crear la entidad del estudiante y guardarla en la base de datos
            var studentEntity = mapper_.Map<StudentEntity>(studentCreateDto);
            classNotesContext_.Students.Add(studentEntity);
            await classNotesContext_.SaveChangesAsync();

            // Asignar el curso al estudiante
            var course = await classNotesContext_.Courses.FindAsync(studentCreateDto.CourseId);
            if (course == null || string.IsNullOrEmpty(course.Name) || string.IsNullOrEmpty(course.Section) || course.StartTime == null)
            {
                return new ResponseDto<StudentResultDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = $"El curso con ID {studentCreateDto.CourseId} no existe o tiene datos incompletos.",
                    Data = null
                };
            }

            var studentCourse = new StudentCourseEntity
            {
                StudentId = studentEntity.Id,
                CourseId = studentCreateDto.CourseId
            };
            classNotesContext_.StudentsCourses.Add(studentCourse);
            await classNotesContext_.SaveChangesAsync();

            // Enviar notificación por correo
            string emailContent = $"Hola {studentEntity.FirstName},\n\n" +
                                  $"Has sido inscrito en el curso {course.Name}.\n" +
                                  $"Sección: {course.Section}\n" +
                                  $"Inicio: {course.StartTime}\n\n" +
                                  "¡Bienvenido!";
            await _emailsService.SendEmailAsync(new EmailDto
            {
                To = studentEntity.Email,
                Subject = "Inscripción en curso",
                Content = emailContent
            });

            // Agregar al listado de estudiantes ingresados correctamente
            successfulStudents.Add(mapper_.Map<StudentDto>(studentEntity));

            // Retornar los estudiantes procesados al frontend
            return new ResponseDto<StudentResultDto>
            {
                StatusCode = 201,
                Status = true,
                Message = MessagesConstant.STU_CREATE_SUCCESS,
                Data = new StudentResultDto
                {
                    SuccessfulStudents = successfulStudents,
                    DuplicateStudents = duplicateStudents,
                    ModifiedEmailStudents = modifiedEmailStudents
                }
            };
        }

        public async Task<ResponseDto<StudentDto>> UpdateStudentAsync(Guid id, StudentEditDto studentEditDto)
        {
            // Necesitamos obtener el id de quien hace la petición
            var userId = _auditService.GetUserId();          

            // Buscar el estudiante por su ID
            var studentEntity = await classNotesContext_.Students
                .FirstOrDefaultAsync(s => s.Id == id && s.TeacherId == userId); // Solo quien lo crea lo puede editar


            // Si el estudiante no existe, retornamos el mensaje que no existe
            if (studentEntity == null)
            {
                return new ResponseDto<StudentDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.STU_RECORD_NOT_FOUND,
                    Data = null
                };
            }

            // Verificar si el nuevo correo ya esta registrado con otro estudiante
            if (studentEditDto.Email != studentEntity.Email)
            {
                var emailExists = await classNotesContext_.Students
                    .AnyAsync(s => s.Email == studentEditDto.Email);

                if (emailExists)
                {
                    return new ResponseDto<StudentDto>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = MessagesConstant.EMAIL_ALREADY_REGISTERED,
                        Data = null
                    };
                }
            }

            // Actualizar los campos del estudiante
            mapper_.Map(studentEditDto, studentEntity);

            // Guardar los cambios en la base de datos
            await classNotesContext_.SaveChangesAsync();

            // Mapear la entidad actualizada a un DTO para la respuesta
            var studentDto = mapper_.Map<StudentDto>(studentEntity);

            // Retornar la respuesta con el DTO
            return new ResponseDto<StudentDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.STU_UPDATE_SUCCESS,
                Data = studentDto
            };
        }

        public async Task<ResponseDto<List<Guid>>> DeleteStudentsInBatchAsync(List<Guid> studentIds)
        {
            // Obtener el ID del usuario que realiza la petición
            var userId = _auditService.GetUserId();

            // Filtrar estudiantes que coincidan con los IDs proporcionados y el TeacherId del usuario
            var studentsToDelete = await classNotesContext_.Students
                .Where(s => studentIds.Contains(s.Id) && s.TeacherId == userId)
                .ToListAsync();

            // Si no se encuentra ningún estudiante
            if (studentsToDelete.Count == 0)
            {
                return new ResponseDto<List<Guid>>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.STU_RECORD_NOT_FOUND,
                };
            }

            // Eliminar registros relacionados en students_courses
            var relatedRecords = await classNotesContext_.StudentsCourses
                .Where(sc => studentIds.Contains(sc.StudentId))
                .ToListAsync();
            classNotesContext_.StudentsCourses.RemoveRange(relatedRecords);

            // Eliminar estudiantes
            classNotesContext_.Students.RemoveRange(studentsToDelete);
            await classNotesContext_.SaveChangesAsync();

            // Retornar una respuesta exitosa con los IDs eliminados
            return new ResponseDto<List<Guid>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.STU_DELETE_SUCCESS,
                Data = studentsToDelete.Select(s => s.Id).ToList()
            };
        }


    }
}