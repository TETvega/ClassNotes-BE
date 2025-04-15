using System.Linq;
using System.Net;
using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Activities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseFilter;
using ClassNotes.API.Dtos.Courses;
using ClassNotes.API.Services.Audit;
using iText.Kernel.Geom;
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
            PAGE_SIZE = configuration.GetValue<int>("PageSize:Courses");
        }
        // Metodo para obtener cursos filtrados 
        public async Task<ResponseDto<PaginationDto<List<CourseCenterDto>>>> GetFilteredCourses(CoursesFilterDto filter)
        {
            // Obtener el ID del usuario que realiza la petición
            var userId = _auditService.GetUserId();
            string classType = filter.ClassTypes?.Trim().ToUpper();
            if (classType != "ALL" && classType != "ACTIVE" && classType != "INACTIVE")
            {
                return new ResponseDto<PaginationDto<List<CourseCenterDto>>>
                {
                    StatusCode = 400,
                    Message = $"El tipo de clase '{filter.ClassTypes}' no es válido. Valores permitidos: ALL, ACTIVE, INACTIVE.",
                    Data = null,
                    Status = false
                };
            }
            int currentPageSize = Math.Max(1, filter.PageSize);
            int currentPage = Math.Max(1, filter.Page);
            int startIndex = (currentPage - 1) * currentPageSize;

            var query = _context.Courses.AsQueryable();

            // Filtro por tipo de clase
            if (classType == "ACTIVE")
                query = query.Where(c => c.IsActive);
            if (classType == "INACTIVE")
                query = query.Where(c => !c.IsActive);

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
            var totalCourses = await query.Where(c => c.Center.TeacherId == userId).CountAsync();

            // Si no se encontraron cursos, retorna mensaje 404
            //if (totalCourses == 0)
            //{
            //    return new ResponseDto<PaginationDto<List<CourseCenterDto>>>
            //    {
            //        StatusCode = 404,
            //        Message = MessagesConstant.RECORD_NOT_FOUND,

            //    };
            //}

            // Aplicamos paginación
            var courses =  query
                .Include(c => c.Center)
                .Include(c => c.Units)
                    .ThenInclude(c => c.Activities)
                        .ThenInclude(c => c.StudentNotes)
                .Include(c => c.Students)
                .Where(c => c.Center.TeacherId == userId)
                .OrderByDescending(c => c.Students.Count(s => s.IsActive)) 
                .ThenBy(c => c.Name) 
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Code,
                    c.Center.Abbreviation,
                    c.CenterId,
                    centerName = c.Center.Name,
                    ActiveStudents = c.Students.Count(s => s.IsActive),
                    c.IsActive,
                    Units = c.Units.Select(u => new {
                        Activities = u.Activities.Select(a => new {
                            HasNote = a.StudentNotes.Any() //Se buscan previamente los datos para evitar errores por muchas llamadas anidadas...
                        }).ToList()
                    }).ToList()
                })
                .AsEnumerable() //Se debe materializar la info y cambiarla de iQuery para que funcione el listado de actividades...
                .Select(c => new CourseCenterDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Code,
                    AbbCenter = c.Abbreviation,
                    CenterId = c.CenterId,
                    CenterName = c.centerName,
                    ActiveStudents = c.ActiveStudents,
                    IsActive = c.IsActive,
                    Activities = new ActivitiesDto
                    {
                        Total = c.Units.Sum(u => u.Activities.Count),
                        TotalEvaluated = c.Units.Sum(u => u.Activities.Count(a => a.HasNote))
                    }
                })
                .ToList();


            // Crear la respuesta de paginación con los datos obtenidos 
            int totalPages = (int)Math.Ceiling(totalCourses / (double)currentPageSize);

            var pagination = new PaginationDto<List<CourseCenterDto>>
            {
                TotalItems = totalCourses,
                Items = courses,
                CurrentPage = currentPage,
                PageSize = currentPageSize,
                TotalPages = totalPages,
                HasNextPage = currentPage < totalPages,
                HasPreviousPage = currentPage > 1
            };

            return new ResponseDto<PaginationDto<List<CourseCenterDto>>>
            {
                StatusCode = 200,
                Status = true,
                Data = pagination,
                Message = MessagesConstant.RECORDS_FOUND
                
            };
        }


    }

}

    