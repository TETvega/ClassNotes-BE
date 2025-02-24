using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.Courses;
using Microsoft.EntityFrameworkCore;

namespace ClassNotes.API.Services.Courses
{
	public class CoursesService : ICoursesService
	{
		private readonly ClassNotesContext _context;
        private readonly IMapper _mapper;
        private readonly int PAGE_SIZE;

        public CoursesService(
            ClassNotesContext context,
            IMapper mapper,
            IConfiguration configuration
        )
        {
            _context = context;
            _mapper = mapper;
            PAGE_SIZE = configuration.GetValue<int>("PageSize");
        }

        // EG -> Enlistar todos los cursos, paginacion

        public async Task<ResponseDto<PaginationDto<List<CourseDto>>>> GetCoursesListAsync(string searchTerm = "", int page = 1) 
        { 
           int startIndex = (page - 1 ) * PAGE_SIZE;

            var coursesQuery = _context.Courses.AsQueryable();

            // buscar por nombre o codgio del curso 
            if (!string.IsNullOrEmpty(searchTerm))
            {
                coursesQuery = coursesQuery.Where(c =>
               c.Name.ToLower().Contains(searchTerm.ToLower()) ||
               c.Code.ToLower().Contains(searchTerm.ToLower()));
            }

            int totalItems = await coursesQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / PAGE_SIZE);

            // aplicar paginacion 

            var courseEntities = await coursesQuery
                .OrderByDescending(n => n.Section) //Ordenara por seccion   
                .Skip(startIndex)
                .Take(PAGE_SIZE)
                .ToListAsync();

            var coursesDto = _mapper.Map<List<CourseDto>>(courseEntities);

            return new ResponseDto<PaginationDto<List<CourseDto>>>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.RECORDS_FOUND,
                Data = new PaginationDto<List<CourseDto>>
                {
                    CurrentPage = page,
                    PageSize = PAGE_SIZE,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = coursesDto,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                }
            };
        }


        // CP -> Para listar un curso mediante su nombre 
        public async Task<ResponseDto<CourseDto>> GetCourseByNameAsync(string name)
		{
			var courseEntity = await _context.Courses
                .FirstOrDefaultAsync(a => a.Name == name);
            if (courseEntity == null)
            {
                return new ResponseDto<CourseDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.RECORD_NOT_FOUND
                };
            }
            var courseDto = _mapper.Map<CourseDto>(courseEntity);
            return new ResponseDto<CourseDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.RECORD_FOUND,
                Data = courseDto
            };
		}

        // CP -> Eliminar un curso
        public async Task<ResponseDto<CourseDto>> DeleteAsync(Guid id)
        {
            var courseEntity = await _context.Courses
                .FirstOrDefaultAsync(a => a.Id == id);

            if (courseEntity == null)
            {
                return new ResponseDto<CourseDto>
                {
                    StatusCode = 404,
                    Status = false,
                    Message = MessagesConstant.RECORD_NOT_FOUND
                };
            }
            _context.Courses.Remove(courseEntity);

            await _context.SaveChangesAsync();

            return new ResponseDto<CourseDto>
            {
                StatusCode = 200,
                Status = true,
                Message = MessagesConstant.DELETE_SUCCESS
            };
        }
	}
}
