using ClassNotes.API.Database.Entities;

namespace ClassNotes.Models
{
    public class EmailFeedBackRequest
    {

        public UserEntity TeacherEntity { get; set; }
        public ActivityEntity ActivityEntity { get; set; }
        public List<StudentInfo> Students { get; set; }


        public class StudentInfo
        {
            public string Name { get; set; }
            public float Score { get; set; }
            public string FeedBack { get; set; }
            public string Email { get; set; }

        }

    }
}