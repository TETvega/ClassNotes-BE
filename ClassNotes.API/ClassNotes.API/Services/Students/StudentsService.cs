using AutoMapper;
using AutoMapper.QueryableExtensions;
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
            PAGE_SIZE = configuration.GetValue<int>("PageSize:Students");
            this._emailsService = emailsService;

        }


        // HR 
        // Realize optimizaciones de querys 
        public async Task<ResponseDto<PaginationDto<List<StudentDto>>>> GetStudentsListAsync(
            string searchTerm = "",
            int? pageSize = null,
            int page = 1
            )
        {
            /** HR
             * Si pageSize es -1, se devuelve int.MaxValue
             * -1 significa "obtener todos los elementos", por lo que usamos int.MaxValue 
             *  int.MaxValue es 2,147,483,647, que es el valor máximo que puede tener un int en C#.
             *  Math.Max(1, valor) garantiza que currentPageSize nunca sea menor que 1 excepto el -1 al inicio
             *  si pageSize es nulo toma el valor de PAGE_SIZE
             */
            int currentPageSize = pageSize == -1 ? int.MaxValue : Math.Max(1, pageSize ?? PAGE_SIZE);
            int startIndex = (page - 1) * currentPageSize;


            // Necesitamos obtener el i de quien hace la petición
            var userId = _auditService.GetUserId();

            // Consulta base con filtrado por TeacherId
            var studentEntityQuery = classNotesContext_.Students
                .Where(x => x.TeacherId == userId);

            // Aplicar búsqueda si hay un término
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // https://www.csharptutorial.net/entity-framework-core-tutorial/ef-core-like/
                // Optimizacion de consultas usando EF directamanete que es de SQL 
                studentEntityQuery = studentEntityQuery.Where(x =>
                    EF.Functions.Like(x.FirstName + " " + x.LastName, $"%{searchTerm}%")
                );
            }

            // Obtener total de elementos antes de la paginación
            var totalStudents = await studentEntityQuery.CountAsync();

            // HR
            // Obtener datos paginados y mapear a DTOs directamente en la consulta
            // cosas de yputube pero tambien lo podes ver en la docu de mapper 
            // https://automapperdocs.readthedocs.io/en/latest/Dependency-injection.html
            // https://stackoverflow.com/questions/53528967/how-to-use-projectto-with-automapper-8-0-dependency-injection

            var studentsDtos = await studentEntityQuery
                .OrderByDescending(x => x.CreatedDate)
                .Skip(startIndex)
                .Take(currentPageSize)
                .ProjectTo<StudentDto>(mapper_.ConfigurationProvider) // Directamente mapeamos en la misma consulta
                .ToListAsync();

            // Calcular total de páginas
            int totalPages = (int)Math.Ceiling((double)totalStudents / currentPageSize);

            // Retornamos la respuesta con los datos paginados
            return new ResponseDto<PaginationDto<List<StudentDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.STU_RECORD_FOUND,
                Data = new PaginationDto<List<StudentDto>>
                {
                    CurrentPage = page,
                    PageSize = currentPageSize,
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

        public async Task<ResponseDto<StudentResultDto>> CreateStudentAsync(BulkStudentCreateDto bulkStudentCreateDto, bool strictMode)
        {
            // Listas para almacenar los resultados del proceso
            var successfulStudents = new List<StudentDto>(); // Alumnos que fueron registrados con éxito
            var duplicateStudents = new List<StudentDto>();  // Alumnos que ya existían y no se pudieron registrar
            var modifiedEmailStudents = new List<StudentDto>(); // Alumnos cuyo email fue modificado para evitar duplicados

            // Verificar si hay estudiantes en el dto
            if (bulkStudentCreateDto?.Students == null || !bulkStudentCreateDto.Students.Any())
            {
                return new ResponseDto<StudentResultDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = MessagesConstant.STU_RECORDS_NOT_FOUND,
                    Data = null
                };
            }

            // Iterar sobre cada estudiante que se intenta agregar
            foreach (var studentDto in bulkStudentCreateDto.Students)
            {
                // Verificar si el correo ya existe en la base de datos
                var existingStudent = await classNotesContext_.Students
                    .FirstOrDefaultAsync(s => s.Email == studentDto.Email);

                if (existingStudent != null)
                {
                    // Si ya existe un estudiante con el mismo correo, y además tiene el mismo nombre y apellido
                    if (existingStudent.FirstName == studentDto.FirstName &&
                        existingStudent.LastName == studentDto.LastName)
                    {
                        duplicateStudents.Add(mapper_.Map<StudentDto>(existingStudent));
                        continue; // Pasar al siguiente estudiante sin agregarlo
                    }

                    // Si estamos en modo estricto, no se permite la modificación del correo, solo se marca como duplicado
                    if (strictMode)
                    {
                        duplicateStudents.Add(mapper_.Map<StudentDto>(existingStudent));
                        continue;
                    }

                    // Si strictMode es falso, modificar el email para evitar duplicados
                    string baseEmail = studentDto.Email.Split('@')[0];
                    string domain = studentDto.Email.Split('@')[1];
                    int counter = 1;
                    string newEmail = studentDto.Email;

                    while (await classNotesContext_.Students.AnyAsync(s => s.Email == newEmail))
                    {
                        newEmail = $"{baseEmail}+{counter}@{domain}";
                        counter++;
                    }

                    studentDto.Email = newEmail;
                    modifiedEmailStudents.Add(mapper_.Map<StudentDto>(existingStudent));
                }

                // TODO : establecer la relacion con el id del curso por medio de la trabla intermedia
                // Probablemente sea porque quite el CourseID en el CreateDto, se lo agregue BulkDto, no estoy seguro pero pueda que tenga que ver en algo con ese error 
                // Mapear y agregar el estudiante a la base de datos
                var studentEntity = mapper_.Map<StudentEntity>(studentDto);
                studentEntity.TeacherId = bulkStudentCreateDto.TeacherId;
                studentEntity.CourseId = bulkStudentCreateDto.CourseId;

                classNotesContext_.Students.Add(studentEntity);
                await classNotesContext_.SaveChangesAsync();

                // Asociar el estudiante al curso
                var studentCourse = new StudentCourseEntity
                {
                    StudentId = studentEntity.Id,
                    CourseId = bulkStudentCreateDto.CourseId
                };
                classNotesContext_.StudentsCourses.Add(studentCourse);
                await classNotesContext_.SaveChangesAsync();

                // Enviar correo de bienvenida
                string emailContent = $"Hola {studentEntity.FirstName},\\n\\n" +
                                       $"Has sido inscrito en el curso{studentEntity.Courses}.\\n\\n" +
                                       "¡Bienvenido!";
                await _emailsService.SendEmailAsync(new EmailDto
                {
                    To = studentEntity.Email,
                    Subject = "Inscripción en curso",
                    Content = emailContent
                });

                successfulStudents.Add(mapper_.Map<StudentDto>(studentEntity));
            }

            // Si no hubo estudiantes exitosos retornar un error
            if (!successfulStudents.Any())
            {
                return new ResponseDto<StudentResultDto>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = MessagesConstant.OPERATION_FAILED,
                    Data = new StudentResultDto
                    {
                        SuccessfulStudents = successfulStudents,
                        DuplicateStudents = duplicateStudents,
                        ModifiedEmailStudents = modifiedEmailStudents
                    }
                };
            }

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

        public async Task<ResponseDto<List<Guid>>> DeleteStudentsInBatchAsync(List<Guid> studentIds, Guid courseId)
        {
            // Obtener el ID del usuario que realiza la petición
            var userId = _auditService.GetUserId();

            // Filtrar estudiantes que coincidan con los IDs proporcionados y el TeacherId del usuario
            var studentsToDelete = await classNotesContext_.Students
                .Where(s => studentIds.Contains(s.Id) && s.TeacherId == userId)
                .ToListAsync();

            // Si no se encuentra ningún estudiante, devolver un error
            if (!studentsToDelete.Any())
            {
                return new ResponseDto<List<Guid>>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.STU_RECORD_NOT_FOUND,
                };
            }

            // Obtener la cantidad de cursos en los que están inscritos los estudiantes
            var studentCourseRelations = await classNotesContext_.StudentsCourses
                .Where(sc => studentIds.Contains(sc.StudentId))
                .GroupBy(sc => sc.StudentId)
                .Select(g => new { StudentId = g.Key, CourseCount = g.Count() })
                .ToListAsync();

            // Eliminar los registros en StudentsCourses SOLO del curso específico
            var relatedRecords = await classNotesContext_.StudentsCourses
                .Where(sc => studentIds.Contains(sc.StudentId) && sc.CourseId == courseId)
                .ToListAsync();
            classNotesContext_.StudentsCourses.RemoveRange(relatedRecords);

            // Determinar qué estudiantes pueden eliminarse de la tabla Students
            var studentsToKeep = studentCourseRelations
                .Where(sc => sc.CourseCount > 1) // Están inscritos en más de un curso
                .Select(sc => sc.StudentId)
                .ToHashSet();

            var studentsToRemoveFromStudents = studentsToDelete
                .Where(s => !studentsToKeep.Contains(s.Id))
                .ToList();

            // Eliminar de la tabla Students solo los que no tienen más cursos
            if (studentsToRemoveFromStudents.Any())
            {
                classNotesContext_.Students.RemoveRange(studentsToRemoveFromStudents);
            }

            await classNotesContext_.SaveChangesAsync();

            // Retornar respuesta con los IDs eliminados de StudentsCourses y Students
            return new ResponseDto<List<Guid>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.STU_DELETE_SUCCESS,
                Data = studentsToRemoveFromStudents.Select(s => s.Id).ToList()
            };
        }



    }
}