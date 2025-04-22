using AutoMapper;
using AutoMapper.QueryableExtensions;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.DashboardCourses;
using ClassNotes.API.Services.Audit;
using Microsoft.EntityFrameworkCore;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace ClassNotes.API.Services.CourseNotes
{
    public class CourseNotesService : ICourseNotesService
    {
        private readonly ClassNotesContext _context;
        private readonly IAuditService _auditService;
        private readonly IMapper _mapper;
        private readonly int PAGE_SIZE;

        public CourseNotesService(
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





        public async Task<ResponseDto<PaginationDto<List<CourseNoteDto>>>> GetAllCourseNotesAsync(
            FilterCourseNotes dto
            )

        {


            /** HR
            * Si pageSize es -1, se devuelve int.MaxValue
            * -1 significa "obtener todos los elementos", por lo que usamos int.MaxValue 
            *  int.MaxValue es 2,147,483,647, que es el valor máximo que puede tener un int en C#.
            *  Math.Max(1, valor) garantiza que currentPageSize nunca sea menor que 1 excepto el -1 al inicio
            *  si pageSize es nulo toma el valor de PAGE_SIZE
            */
            int currentPageSize =  Math.Max(1, dto.PageSize ?? PAGE_SIZE);
            int startIndex = (dto.Page - 1) * currentPageSize;

            var userId = _auditService.GetUserId();

            var courseNoteQuery = _context.CoursesNotes
                   .Where(c => c.CourseId == dto.CourseId && c.CreatedBy == userId)
                   .AsQueryable();

            if (!string.IsNullOrEmpty(dto.SearchTerm))
            {
                string searchTerm = dto.SearchTerm.ToLower();
                courseNoteQuery = courseNoteQuery.Where(c =>
                    c.Title.ToLower().Contains(searchTerm) ||
                    c.Content.ToLower().Contains(searchTerm));
            }

            DateTime yesterday = DateTime.UtcNow.Date.AddDays(-1);


            if (dto.Filter.ToUpper() == "PENDING")
            {
                courseNoteQuery = courseNoteQuery
                    .Where(c => !c.isView)
                    .OrderBy(c => c.UseDate); // Más cercano en fecha de uso primero
            }
            else if (dto.Filter.ToUpper() == "HISTORY")
            {
                courseNoteQuery = courseNoteQuery
                    .Where(c => c.isView)
                    .OrderByDescending(c => c.UseDate); // Más reciente primero
            }

            int totalItems = await courseNoteQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / currentPageSize);
            // aplicar paginacion 

            // HR 
            // optimizacion directa aplicando el mapeo directamente
            var courseNoteDtos = await courseNoteQuery
                .Skip(startIndex)
                .Take(currentPageSize)
                .ProjectTo<CourseNoteDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return new ResponseDto<PaginationDto<List<CourseNoteDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.CNS_RECORDS_FOUND,
                Data = new PaginationDto<List<CourseNoteDto>>
                {
                    CurrentPage = dto.Page,
                    PageSize = currentPageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = courseNoteDtos,
                    HasPreviousPage = dto.Page > 1,
                    HasNextPage = dto.Page < totalPages
                }
            };

        }

        public async Task<ResponseDto<CourseNoteDto>> GetCourseNoteByIdAsync(Guid id)
        {
            var userId = _auditService.GetUserId(); // Id de quien hace la petición

            var courseNoteEntity = await _context.CoursesNotes.FirstOrDefaultAsync(c => c.Id == id && c.CreatedBy == userId);

            if (courseNoteEntity == null)
            {
                return new ResponseDto<CourseNoteDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.CNS_RECORD_NOT_FOUND
                };
            }

            var courseNoteDto = _mapper.Map<CourseNoteDto>(courseNoteEntity);

            return new ResponseDto<CourseNoteDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.CNS_RECORDS_FOUND,
                Data = courseNoteDto
            };

        }
        public async Task<ResponseDto<CourseNoteDto>> CreateAsync(CourseNoteCreateDto dto)
        {
            var courseNoteEntity = _mapper.Map<CourseNoteEntity>(dto);

            _context.CoursesNotes.Add(courseNoteEntity);

            await _context.SaveChangesAsync();

            var courseNoteDto = _mapper.Map<CourseNoteDto>(courseNoteEntity);

            return new ResponseDto<CourseNoteDto>
            {
                StatusCode = 201,
                Status = true,
                Message = MessagesConstant.CNS_CREATE_SUCCESS,
                Data = courseNoteDto
            };
        }

        public async Task<ResponseDto<CourseNoteDto>> EditAsync(CourseNoteEditDto dto, Guid id)
        {
            var userId = _auditService.GetUserId(); // Id de quien hace la petición

            var courseNoteEntity = await _context.CoursesNotes.FirstOrDefaultAsync(x => x.Id == id && x.CreatedBy == userId); // El docente solo puede editar sus notas

            if (courseNoteEntity == null)
            {
                return new ResponseDto<CourseNoteDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.CNS_RECORD_NOT_FOUND
                };
            }

            _mapper.Map<CourseNoteEditDto, CourseNoteEntity>(dto, courseNoteEntity);

            _context.CoursesNotes.Update(courseNoteEntity);
            await _context.SaveChangesAsync();

            var courseNoteDto = _mapper.Map<CourseNoteDto>(courseNoteEntity);

            return new ResponseDto<CourseNoteDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.CNS_UPDATE_SUCCESS,
                Data = courseNoteDto
            };

        }

        public async Task<ResponseDto<CourseNoteDto>> DeleteAsync(Guid id)
        {
            var userId = _auditService.GetUserId(); // Id de quien hace la petición

            var courseNoteEntity = await _context.CoursesNotes.FirstOrDefaultAsync(c => c.Id == id && c.CreatedBy == userId); // El docente solo puede borrar sus notas

            if (courseNoteEntity == null)
            {
                return new ResponseDto<CourseNoteDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.CNS_RECORD_NOT_FOUND
                };

            }

            _context.CoursesNotes.Remove(courseNoteEntity);
            await _context.SaveChangesAsync();

            return new ResponseDto<CourseNoteDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.CNS_DELETE_SUCCESS,
            };

        }

        public async Task<ResponseDto<List<CoursesNotesDtoViews>>> EditListNotesViews(List<Guid> notesList)
        {
            if (notesList == null || !notesList.Any()) 
            {
                return new ResponseDto<List<CoursesNotesDtoViews>>
                {
                    StatusCode = 400,
                    Status = false,
                    Message = "Error: La lista de notas es nula o vacía.",
                    Data = null
                };
            }

            var userId = _auditService.GetUserId();


            var notes = await _context.CoursesNotes
                .Where(c => notesList.Contains(c.Id) && c.CreatedBy == userId)
                .ToListAsync();

            if (notes.Count != notesList.Count)
            {
                return new ResponseDto<List<CoursesNotesDtoViews>>
                {
                    StatusCode = 403,  
                    Status = false,
                    Message = "Error: No tienes permisos suficientes para editar estas notas.",
                    Data = null
                };
            }

            notes.ForEach(n => n.isView = !n.isView);
            await _context.SaveChangesAsync();  

            var data = _mapper.Map<List<CoursesNotesDtoViews>>(notes);  

            return new ResponseDto<List<CoursesNotesDtoViews>>
            {
                StatusCode = 200,
                Status = true,
                Message = "Éxito: Notas actualizadas correctamente.",
                Data = data
            };
        }

    }
}
