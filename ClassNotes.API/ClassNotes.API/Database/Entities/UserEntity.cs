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

        
        [Column("default_course_setting_id")]
        public Guid? DefaultCourseSettingId { get; set; }
        [ForeignKey(nameof(DefaultCourseSettingId))]
        public virtual CourseSettingEntity DefaultCourseSettings { get; set; }

		[Column("secret_key")] // AM: Llave secreta para la generación del OTP
		[Required]
		public string SecretKey { get; set; } = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

		[Column("otp_code")]
		public string? OtpCode { get; set; }

		[Column("otp_expiration")]
		public DateTime? OtpExpiration { get; set; }

		public virtual ICollection<CenterEntity> Centers { get; set; }
        public virtual ICollection<StudentEntity> Students { get; set; }
    }
}
