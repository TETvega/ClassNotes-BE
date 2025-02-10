using ClassNotes.API.Database.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using ClassNotes.API.Database.Configuration;
using ClassNotes.API.Services.Audit;
using System.Diagnostics;

namespace ClassNotes.API.Database
{
    public class ClassNotesContext : IdentityDbContext<UserEntity>
    {
		private readonly IAuditService _auditService;

		public ClassNotesContext(DbContextOptions options, IAuditService auditService) : base(options)
        {
			this._auditService = auditService;
		}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //(Ken)
            //Configuracion basica para prueba, podria cambiar...
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");
            modelBuilder.HasDefaultSchema("security");

            modelBuilder.Entity<UserEntity>().ToTable("users");
            modelBuilder.Entity<IdentityRole>().ToTable("roles");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("users_roles");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("users_claims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("users_logins");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("roles_claims");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("users_tokens");

            modelBuilder.ApplyConfiguration(new ActivityConfiguration());
            modelBuilder.ApplyConfiguration(new AttendanceConfiguration());
            modelBuilder.ApplyConfiguration(new CenterConfiguration());
            modelBuilder.ApplyConfiguration(new CourseConfiguration());
            modelBuilder.ApplyConfiguration(new CourseNoteConfiguration());
            modelBuilder.ApplyConfiguration(new CourseSettingConfiguration());
            modelBuilder.ApplyConfiguration(new StudentActivityNoteConfiguration());
            modelBuilder.ApplyConfiguration(new StudentConfiguration());
            modelBuilder.ApplyConfiguration(new StudentCourseConfiguration());


            //(Ken)
            //Configuracion basica para evitar eliminacion en cascada, lo pongo de un solo para que no se nos olvide...
            var eTypes = modelBuilder.Model.GetEntityTypes();
            foreach (var type in eTypes)
            {
                var foreignKeys = type.GetForeignKeys();
                foreach (var foreignKey in foreignKeys)
                {
                    foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }
<<<<<<< HEAD
            // AM : Relacione ntre CenterEntity y UserEntity
            modelBuilder.Entity<CenterEntity>()
            .HasOne(a => a.Teacher)
            .WithMany(U => U.Centers)
            .HasForeignKey(c => c.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
            //AM : Relacion entre CourseEntity y CurseSettingEntity
            modelBuilder.Entity<CourseEntity>()
            .HasOne(c => c.CourseSetting)
            .WithMany(s => s.Courses)
            .HasForeignKey(c => c.SettingId)
            .OnDelete(DeleteBehavior.Cascade);
            //AM : Relacion entre CourseEntity y CenterEntity
            modelBuilder.Entity<CourseEntity>()
            .HasOne(c => c.Center)
            .WithMany(tp => tp.Courses)
            .HasForeignKey(c => c.CenterId)
            .OnDelete(DeleteBehavior.Cascade);

            // //AM : Relacion entre UserEntity y CourseEntity
            modelBuilder.Entity<UserEntity>()
             .HasOne(u => u.DefaultCourseSettings)
             .WithMany()
             .HasForeignKey(u => u.DefaultCourseSettingId)
             .OnDelete(DeleteBehavior.Restrict);

            //AM Relacion entre StudentsEntity y UserEntity
            modelBuilder.Entity<StudentEntity>()
            .HasOne(s => s.Teacher)
            .WithMany(u => u.Students)
            .HasForeignKey(s => s.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);



            //AM: Relacionentre StudentEntity y CourseEntity
            modelBuilder.Entity<StudentCourseEntity>()
            .HasOne(sc => sc.Course)
            .WithMany(c => c.Students)
            .HasForeignKey(sc => sc.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
            //AM: Relacion entre StudentEntity y ActivityEntity 
            modelBuilder.Entity<StudentActivityNoteEntity>()
            .HasOne(san => san.Student)
            .WithMany(s => s.Activities)
            .HasForeignKey(san => san.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
            //Am: Relacion entre ActivityEntity y StudentActivityENtity
            modelBuilder.Entity<StudentActivityNoteEntity>()
            .HasOne(san => san.Activity)
             .WithMany(a => a.StudentNotes)
            .HasForeignKey(san => san.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

            //Am: Relacion entre AttendanceEntity y CourseEntity 
            modelBuilder.Entity<AttendanceEntity>()
            .HasOne(a => a.Course)
            .WithMany(c => c.Attendances)
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

            //Am: Relación entre AttendanceEntity y StudentEntity
            modelBuilder.Entity<AttendanceEntity>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            //Am: Relación entre CourseNoteEntity y CourseEntity
            modelBuilder.Entity<CourseNoteEntity>()
                .HasOne(cn => cn.Course)
                .WithMany(c => c.CourseNotes)
                .HasForeignKey(cn => cn.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ActivityEntity>()
            .HasOne(a => a.Course)
            .WithMany(c => c.Activities)
             .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
=======
        
>>>>>>> 077fa5de0c18152006001d421f090c06fbce5f54

        }

        //  public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        //  {
        //      var entries = ChangeTracker
        //          .Entries()
        //          .Where(e => e.Entity is BaseEntity && (
        //              e.State == EntityState.Added ||
        //              e.State == EntityState.Modified
        //          ));

        //      foreach (var entry in entries)
        //      {
        //          var entity = entry.Entity as BaseEntity;
        //          if (entity != null)
        //          {
        //              if (entry.State == EntityState.Added)
        //              {
        //                  entity.CreatedBy = _auditService.GetUserId();
        //entity.CreatedDate = DateTime.Now;
        //              }
        //              else
        //              {
        //                  entity.UpdatedBy = _auditService.GetUserId();
        //entity.UpdatedDate = DateTime.Now;
        //              }
        //          }
        //      }

        //      return base.SaveChangesAsync(cancellationToken);
        //  }

        public DbSet<ActivityEntity> Activities { get; set; }
        public DbSet<AttendanceEntity> Attendances { get; set; }
        public DbSet<CenterEntity> Centers { get; set; }
        public DbSet<CourseEntity> Courses { get; set; }
        public DbSet<CourseNoteEntity> CoursesNotes { get; set; }
        public DbSet<CourseSettingEntity> CoursesSettings { get; set; }
        public DbSet<StudentActivityNoteEntity> StudentsActivitiesNotes { get; set; }
        public DbSet<StudentCourseEntity> StudentsCourses { get; set; }
        public DbSet<StudentEntity> Students { get; set; }

    }
}
