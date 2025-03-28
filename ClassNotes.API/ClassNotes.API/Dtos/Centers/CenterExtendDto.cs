namespace ClassNotes.API.Dtos.Centers
{
    public class CenterExtendDto : CenterDto
    {
        public int TotalActiveClasses { get; set; } // Utilizadas en el Get de Centros 

        public int TotalActiveStudents { get; set; } // Utilizada en el get de centros
    }
}
