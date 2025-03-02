using Microsoft.AspNetCore.Identity;
using OtpNet;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassNotes.API.Database.Entities
{

    public class UserEntity : IdentityUser
    {

        //(Ken)
        //Deje una longitud minima de 2 por si acaso, en ambas propiedades.
        [Required]
        [StringLength(75, MinimumLength = 2)]
        [Column("first_name")]
        public string FirstName { get; set; }


        [StringLength(70, MinimumLength = 2)]
        [Column("last_name")]
        public string LastName { get; set; }

        [Column("resfesh_token")]
        [StringLength(450)]
        public string RefreshToken { get; set; }
        [Column("resfesh_token_expire")]
        public DateTime RefreshTokenExpire { get; set; }
        
        [Column("default_course_setting_id")]
        public Guid? DefaultCourseSettingId { get; set; }
        [ForeignKey(nameof(DefaultCourseSettingId))]
        public virtual CourseSettingEntity DefaultCourseSettings { get; set; }

        // CG: se eliminaron los atributos relacionados a lo del OTP porque se maneja a traves de memoria

		public virtual ICollection<CenterEntity> Centers { get; set; }
        public virtual ICollection<CourseEntity> Courses { get; set; }
        public virtual ICollection<StudentEntity> Students { get; set; }
    }
}
