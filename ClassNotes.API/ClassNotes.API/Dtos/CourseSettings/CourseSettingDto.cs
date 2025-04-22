using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using NetTopologySuite.Geometries;
using ClassNotes.API.Dtos.Common;

namespace ClassNotes.API.Dtos.CourseSettings
{
    // --------------------- CP --------------------- //
    public class CourseSettingDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } // El nombre que se le dara a la configuración

        public string ScoreType { get; set; } // El tipo de puntuación por si es ponderado u oro

        public DateTime StartDate { get; set; } // Inicio de periodo

        public DateTime EndDate { get; set; } // Fin de periodo
        public LocationDto GeoLocation { get; set; } // Sistema de geolocalizacion
        public int ValidateRangeMeters { get; set; }

        public float MinimumGrade { get; set; } // Nota minima en una clase

        public float MaximumGrade { get; set; } // Nota maxima en una clase

        public int MinimumAttendanceTime { get; set; } // El tiempo en el cual se debe de mandar la asistencia

        public bool IsOriginal { get; set; } // Para separar las configuraciones originales de las replicas (copias)
    }
}