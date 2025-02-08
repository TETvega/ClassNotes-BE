using ClassNotes.API.Database.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using ClassNotes.API.Database.Configuration;
using ClassNotes.API.Services.Audit;

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

            /*
             Aqui se encuentran las propiedades y tablas necesarias para IDENTITY
             */
            modelBuilder.Entity<UserEntity>().ToTable("users");
            modelBuilder.Entity<IdentityRole>().ToTable("roles");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("users_roles");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("users_claims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("users_logins");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("roles_claims");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("users_tokens");

            //Aplican las Configuraciones de LLaves Foraneas
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
            //Configuracion basica para evitar eliminacion en cascada
            //
            var eTypes = modelBuilder.Model.GetEntityTypes();
            foreach (var type in eTypes)
            {
                var foreignKeys = type.GetForeignKeys();
                foreach (var foreignKey in foreignKeys)
                {
                    foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }

        }

        /*El siguiente Codigo Sive para los Campos de Auditoria, saber quien esta mandando las peticiones editando o creando*/
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (
                    e.State == EntityState.Added ||
                    e.State == EntityState.Modified
                ));

            foreach (var entry in entries)
            {
                var entity = entry.Entity as BaseEntity;
                if (entity != null)
                {
                    //El usuario esta creando 
                    if (entry.State == EntityState.Added)
                    {
                        entity.CreatedBy = _auditService.GetUserId();
						entity.CreatedDate = DateTime.Now;
                    }
                      //El usuario esta editando 
                    else
                    {
                        entity.UpdatedBy = _auditService.GetUserId();
						entity.UpdatedDate = DateTime.Now;
                    }
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        //Entidades de la Base de Datos SQL 
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
