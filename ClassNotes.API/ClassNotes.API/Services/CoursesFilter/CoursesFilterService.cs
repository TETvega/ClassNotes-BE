using System.Linq;
using System.Net;
using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Activities;
using ClassNotes.API.Dtos.Allcourses;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseFilter;
using ClassNotes.API.Dtos.Courses;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;
using static ClassNotes.API.Services.AllCourses.CoursesFilterService;

namespace ClassNotes.API.Services.AllCourses
{
    public class CoursesFilterService : ICoursesFilterService   
    {
        private readonly ClassNotesContext _context;
        private readonly IAuditService _auditService;
        private readonly IMapper _mapper;
        private readonly int PAGE_SIZE;

        public CoursesFilterService(
            ClassNotesContext context,
            IAuditService auditService,
            IConfiguration configuration,
            IMapper mapper)
        {
            _context = context;
            _auditService = auditService;
            _mapper = mapper;
            PAGE_SIZE = configuration.GetValue<int>("PageSize:CourseNotes");
        }
        // Metodo para obtener cursos filtrados 
        public async Task<ResponseDto<PaginationDto<List<CourseCenterDto>>>> GetFilteredCourses(CoursesFilterDto filter)
        {
            // Obtener el ID del usuario que realiza la petición
            var userId = _auditService.GetUserId();

            // consulta base sobre los cursos
            var query = _context.Courses.AsQueryable();

            // Filtro por tipo de clase
            if (filter.ClassTypes != "all")
            {
                if (filter.ClassTypes.ToLower() == "active")
                {
                    // Filtro para cursos activos
                    query = query.Where(c => c.IsActive == true);
                }
                else if (filter.ClassTypes.ToLower() == "inactive")
                {
                    // Filtro para cursos inactivos
                    query = query.Where(c => c.IsActive == false);
                }
            }

            // Filtro por centros
            if (filter.Centers.Any())
            {
                query = query.Where(c => filter.Centers.Contains(c.CenterId));
            }

            // Filtro por término de búsqueda 
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(c =>
                   c.Name.Contains(filter.SearchTerm) ||  // Busca por nombre del curso
                   c.Code.Contains(filter.SearchTerm) ||  // Busca por código del curso
                   c.Center.Abbreviation.Contains(filter.SearchTerm) // Busca por abreviatura del centro
               );
            }

            //  total de cursos que cumplen con los filtros
            var totalCourses = await query.CountAsync();

            // Si no se encontraron cursos, retorna mensaje 404
            if (totalCourses == 0)
            {
                return new ResponseDto<PaginationDto<List<CourseCenterDto>>>
                {
                    StatusCode = 404,
                    Message = MessagesConstant.RECORD_NOT_FOUND,

                };
            }

            // Aplicamos paginación
            var courses = await query
                .Include(c => c.Center)
                .Include(a => a.Activities)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => new CourseCenterDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Code,
                    AbbCenter = c.Center.Abbreviation, 
                    ActiveStudents = c.Students.Count(),
                    IsActive = c.IsActive,
                    Activities =  new ActivitiesDto
                    {
                        Total = c.Activities.Count(),
                        TotalEvaluated = c.Activities.Count(a => a.StudentNotes.Any())
                    }
                })
                .ToListAsync();

            // Crear la respuesta de paginación con los datos obtenidos 
            var pagination = new PaginationDto<List<CourseCenterDto>>
            {
                TotalItems = totalCourses,
                Items = courses 
            };

            return new ResponseDto<PaginationDto<List<CourseCenterDto>>>
            {
                StatusCode = 200,
                Data = pagination,
                Message = MessagesConstant.RECORDS_FOUND
                
            };
        }


    }

}

    