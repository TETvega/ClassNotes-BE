using AutoMapper;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Activities;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Dtos.Centers;
using ClassNotes.API.Dtos.CourseNotes;
using ClassNotes.API.Dtos.Courses;
using ClassNotes.API.Dtos.CourseSettings;
using ClassNotes.API.Dtos.Students;

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
		}

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
			CreateMap<CenterEntity, CenterDto>();
			CreateMap<CenterCreateDto, CenterEntity>();
			CreateMap<CenterEditDto, CenterEntity>();
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
	}
}
