using ClassNotes.API.Database.Entities;

namespace ClassNotes.Models
{
	public class EmailStudentListRequest
	{
		//Esta informacion es necesaria para el servicio de crear pdf de emailservice, tambien para extraer informacion de estudiante para el envio...
		public CenterEntity centerEntity { get; set; }
		public UserEntity teacherEntity { get; set; }
        public string Content { get; set; }//Este es el contenido que irá en el email...
        public CourseEntity courseEntity { get; set; }
		public List<studentInfo> students { get; set; }
		public CourseSettingEntity courseSettingEntity { get; set; }//Uso una lista de la clase anidada para poder tener info de varios estudiantes...
		public class studentInfo //Clase anidada, incluye la lista de estudiantes, relacion con curso y el propio estudiante
		{
			public StudentEntity studentEntity { get; set; }
			public StudentCourseEntity studentCourseEntity { get; set; }
			public List<StudentUnitEntity> unitEntities { get; set; }

		}

	}
}
