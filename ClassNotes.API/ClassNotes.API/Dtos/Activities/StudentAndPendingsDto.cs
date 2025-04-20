namespace ClassNotes.API.Dtos.Activities
{
    public class StudentAndPendingsDto 
    {

        public ClassInfo Class { get; set; } 
        public StudentInfo Student { get; set; } 

        public class ClassInfo
        {
            public Guid Id { get; set; }
            public string ClassName { get; set;}
            public Guid CenterId { get; set; }
            public string CenterName { get; set; }
            public string CenterAbb { get; set; }
        }

        public class StudentInfo
        {
            public Guid Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public bool Status { get; set; }
        }
    }
}