using AutoMapper;
using ClassNotes.API.Constants;
using ClassNotes.API.Database;
using ClassNotes.API.Dtos.Common;
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
