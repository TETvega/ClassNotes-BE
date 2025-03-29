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

        private void MapsForActivities()
        {
            // Mapeo para el get all (ActivitySummaryDto)
            CreateMap<ActivityEntity, ActivitySummaryDto>()
                .ForMember(dest => dest.Course, opt => opt.MapFrom(src => new ActivitySummaryDto.CourseInfo
                {
                    Id = src.Unit.Course.Id, // Se le pasan el id del curso al que pertenece la actividad
                    Name = src.Unit.Course.Name // Se pasa el nombre del curso al que pertenece la actividad
                }))
                .ForMember(dest => dest.CenterId, opt => opt.MapFrom(src => src.Unit.Course.CenterId)); // Se pasa el id del centro al que pertenece

            // Mapeo para el get by id (ActivityDto)
            CreateMap<ActivityEntity, ActivityDto>()
                .ForMember(dest => dest.Unit, opt => opt.MapFrom(src => new ActivityDto.UnitInfo
                {
                    Id = src.Unit.Id, // El id de la unidad a la que pertenece
                    Number = src.Unit.UnitNumber // El numero de esa unidad
                }))
                .ForMember(dest => dest.Course, opt => opt.MapFrom(src => new ActivityDto.CourseInfo
                {
                    Id = src.Unit.Course.Id, // El id del curso al que pertenece
                    Name = src.Unit.Course.Name // El nombre del curso
                }));

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
            CreateMap<CourseEntity, CourseDto>()
                .ForMember(dest => dest.SettingName, opt => opt.MapFrom(src => src.CourseSetting.Name)) // Mapear el nombre de la configuración
                .ForMember(dest => dest.ScoreType, opt => opt.MapFrom(src => src.CourseSetting.ScoreType)) // Mapear el tipo de puntuación
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.CourseSetting.StartDate)) // Mapear la fecha de inicio
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.CourseSetting.EndDate)) // Mapear la fecha de fin
                .ForMember(dest => dest.MinimumGrade, opt => opt.MapFrom(src => src.CourseSetting.MinimumGrade)) // Mapear la nota mínima
                .ForMember(dest => dest.MaximumGrade, opt => opt.MapFrom(src => src.CourseSetting.MaximumGrade)) // Mapear la nota máxima
                .ForMember(dest => dest.MinimumAttendanceTime, opt => opt.MapFrom(src => src.CourseSetting.MinimumAttendanceTime)) // Mapear el tiempo mínimo de asistencia
                .ForMember(dest => dest.IsOriginal, opt => opt.MapFrom(src => src.CourseSetting.IsOriginal)); // Mapear si es original;
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
