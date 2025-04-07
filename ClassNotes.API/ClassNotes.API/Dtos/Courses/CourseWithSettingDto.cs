using ClassNotes.API.Dtos.CourseSettings;

namespace ClassNotes.API.Dtos.Courses
{
    public class CourseWithSettingDto
    {
        // Props del curso
        public CourseDto Course { get; set; }

        // Props de las configuraciones
        public CourseSettingDto CourseSetting { get; set; }

        //Para devolver unidades...
        public List<UnitDto> Units { get; set; }
    }
}