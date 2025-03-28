using AutoMapper;
using AutoMapper.QueryableExtensions;
using Azure;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.Students;
using ClassNotes.API.Services.Audit;
using iText.Commons.Actions.Contexts;
using iText.Layout.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClassNotes.API.Services.Students
{
    public class StudentsService : IStudentsService
    {
        private readonly ClassNotesContext classNotesContext_;
        private readonly IMapper mapper_;
        private readonly IAuditService _auditService;
        private readonly int PAGE_SIZE;

        public StudentsService(ClassNotesContext classNotesContext, IAuditService auditService, IMapper mapper, IConfiguration configuration)
        {
            classNotesContext_ = classNotesContext;
            mapper_ = mapper;
            _auditService = auditService;
            PAGE_SIZE = configuration.GetValue<int>("PageSize:Students");
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

        public async Task<ResponseDto<StudentDto>> CreateStudentAsync(StudentCreateDto studentCreateDto)
        {
            // Verificar si el correo ya esta registrado
            var existingStudent = await classNotesContext_.Students
                .FirstOrDefaultAsync(s => s.Email == studentCreateDto.Email);

            if (existingStudent != null)
            {
                // Si el correo ya existe, verificar si los nombres son iguales
                if (existingStudent.FirstName == studentCreateDto.FirstName &&
                    existingStudent.LastName == studentCreateDto.LastName)
                {
                    // Si los nombres son iguales, apuntamos a ese estudiante
                    var studentDtos = mapper_.Map<StudentDto>(existingStudent);

                    return new ResponseDto<StudentDto>
                    {
                        StatusCode = 200,
                        Status = true,
                        Message = MessagesConstant.STUDENT_EXISTS,
                        Data = studentDtos
                    };
                }
                else
                {
                    // Si los nombres no son iguales, retornar un error porque el correo ya esta registrado
                    return new ResponseDto<StudentDto>
                    {
                        StatusCode = 400,
                        Status = false,
                        Message = MessagesConstant.EMAIL_ALREADY_REGISTERED,
                        Data = null
                    };
                }
            }

            // Si el correo no existe, crear un nuevo estudiante
            var studentEntity = mapper_.Map<StudentEntity>(studentCreateDto);

            // Agregar la entidad al contexto
            classNotesContext_.Students.Add(studentEntity);

            // Guardar los cambios en la base de datos
            await classNotesContext_.SaveChangesAsync();

            // Mapear la entidad guardada a un DTO para la respuesta
            var studentDto = mapper_.Map<StudentDto>(studentEntity);

            // Retornar la respuesta con el DTO
            return new ResponseDto<StudentDto>
            {
                StatusCode = 201,
                Status = true,
                Message = MessagesConstant.STU_CREATE_SUCCESS,
                Data = studentDto
            };
        }

        public async Task<ResponseDto<StudentDto>> UpdateStudentAsync(Guid id, StudentEditDto studentEditDto)
        {
            // Necesitamos obtener el i de quien hace la petición
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
        public async Task<ResponseDto<StudentDto>> DeleteStudentAsync(Guid id)
        {
            // Necesitamos obtener el i de quien hace la petición
            var userId = _auditService.GetUserId();

            // Buscar el estudiante por su ID
            var studentEntity = await classNotesContext_.Students
                .FirstOrDefaultAsync(s => s.Id == id && s.TeacherId == userId); // Solo quien lo crea puede borrarlo

            // Si el estudiante no existe, retornar un error
            if (studentEntity == null)
            {
                return new ResponseDto<StudentDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.STU_RECORD_NOT_FOUND,
                };
            }

            // Eliminar registros relacionados en students_courses
            var relatedRecords = await classNotesContext_.StudentsCourses
                .Where(sc => sc.StudentId == id)
                .ToListAsync();
            classNotesContext_.StudentsCourses.RemoveRange(relatedRecords);

            // Eliminar el estudiante
            classNotesContext_.Students.Remove(studentEntity);
            await classNotesContext_.SaveChangesAsync();

            // Retornarnamos una respuesta exitosa
            return new ResponseDto<StudentDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.STU_DELETE_SUCCESS
            };
        }
    }
}