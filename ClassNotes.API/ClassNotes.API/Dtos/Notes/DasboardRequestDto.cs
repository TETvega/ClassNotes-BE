using ClassNotes.API.Dtos.Common;
using ClassNotes.API.Dtos.Notes.QualificationDasboard;

namespace ClassNotes.API.Dtos.Notes
{
    public class DasboardRequestDto
    { 
        public DateTime? EndDate { get; set; }
        public StadisticStudentsDto StadisticStudents { get; set; }
        public PaginationDto<List<StudentQualificationDto>> StudentQualifications { get; set; }
    }
}
