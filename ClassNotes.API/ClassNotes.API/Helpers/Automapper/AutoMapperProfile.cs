using AutoMapper;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Activities;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.Courses;
using ClassNotes.API.Dtos.CourseSettings;
using ClassNotes.API.Dtos.DashboardCourses;
using ClassNotes.API.Dtos.Students;
using ClassNotes.API.Dtos.TagsActivities;
using ClassNotes.API.Dtos.Users;
using ClassNotes.API.Services.Audit;

namespace ClassNotes.API.Helpers.Automapper
{
	public class AutoMapperProfile : Profile
	{
		public AutoMapperProfile()
		{
			MapsForActivities();
			MapsForAttendances();
			MapsForCenters();
			MapsForCourses();
			MapsForStudents();
			MapsForCourseNotes();
			MapsForCourseSettings();
			MapsForUsers();
			MapsForTagsActivities();
		}

        // Si escala a mas de 3 y son complejos se escalara a archvivos individuales  TIPO [Carpeta] Maps/[Entidad].cs
        private void MapsForActivities()
        {
            CreateMap<ActivityEntity, ActivityDto>();
            CreateMap<ActivityCreateDto, ActivityEntity>();
            CreateMap<ActivityEditDto, ActivityEntity>();
        }

        private void MapsForAttendances()
        {
            CreateMap<AttendanceEntity, AttendanceDto>();
            CreateMap<AttendanceCreateDto, AttendanceEntity>();
            CreateMap<AttendanceEditDto, AttendanceEntity>();
        }

        private void MapsForCenters()
        {
            //(Ken)
            //Aparentemente no se puede hacer con ForAllMembers esto, ya vere como simplificar...
            CreateMap<CenterEntity, CenterDto>();
            CreateMap<CenterCreateDto, CenterEntity>()
                .ForMember(dest => dest.IsArchived, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
                //.ForMember(dest => dest.Logo, opt => opt.MapFrom(src => src.Logo.Trim()))
                .ForMember(dest => dest.Abbreviation, opt => opt.MapFrom(src => src.Abbreviation.Trim()));
            CreateMap<CenterEditDto, CenterEntity>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
                //.ForMember(dest => dest.Logo, opt => opt.MapFrom(src => src.Logo.Trim()))
                .ForMember(dest => dest.Abbreviation, opt => opt.MapFrom(src => src.Abbreviation.Trim()));
        }

        private void MapsForCourses()
        {
            CreateMap<CourseEntity, CourseDto>();
            CreateMap<CourseCreateDto, CourseEntity>();
            CreateMap<CourseEditDto, CourseEntity>();
        }

        private void MapsForStudents()
        {
            CreateMap<StudentEntity, StudentDto>();
            CreateMap<StudentCreateDto, StudentEntity>();
            CreateMap<StudentEditDto, StudentEntity>();
        }

        private void MapsForCourseNotes()
        {
            CreateMap<CourseNoteEntity, CourseNoteDto>();
            CreateMap<CourseNoteCreateDto, CourseNoteEntity>();
            CreateMap<CourseNoteEditDto, CourseNoteEntity>();
        }

        private void MapsForCourseSettings()
        {
            CreateMap<CourseSettingEntity, CourseSettingDto>();
            CreateMap<CourseSettingCreateDto, CourseSettingEntity>();
            CreateMap<CourseSettingEditDto, CourseSettingEntity>();
        }

        private void MapsForUsers()
        {
            CreateMap<UserEntity, UserDto>();
            CreateMap<UserEditDto, UserEntity>();
        }


		private void MapsForTagsActivities()
		{
			CreateMap<TagActivityEntity, TagActivityDto>();
			CreateMap<TagActivityCreateDto, TagActivityEntity>();
			CreateMap<TagActivityEditDto, TagActivityEntity>();
		}
        // Mapeo del dashboard de cursos
        private void MapsForDashboardCourses()
        {
            CreateMap<ActivityEntity, DashboardCourseActivityDto>();
            CreateMap<StudentEntity, DashboardCourseStudentDto>()
                .ForMember(
                dest => dest.FullName,
                opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}") // Este mappeo es para concatenar firstName y  
            );                                                               // lastName para asi obtener el nombre completo 
        }
    }
}
