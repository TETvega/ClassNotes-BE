using ClassNotes.API.Database.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using ClassNotes.API.Database.Configuration;
using ClassNotes.API.Services.Audit;
using System.Diagnostics;
using Serilog;


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
            modelBuilder.ApplyConfiguration(new StudentUnitConfiguration());
            modelBuilder.ApplyConfiguration(new TagActivityConfiguration());
            modelBuilder.ApplyConfiguration(new UnitConfiguration());


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
        }

        /*El siguiente Codigo Sive para los Campos de Auditoria, saber quien esta mandando las peticiones editando o creando*/
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity &&
                    (e.State == EntityState.Added ||
                     e.State == EntityState.Modified ||
                     e.State == EntityState.Deleted));

            foreach (var entry in entries)
            {
                var entity = entry.Entity as BaseEntity;
                var userId = _auditService.GetUserId();
                var entityName = entry.Entity.GetType().Name;
                var primaryKey = entry.Properties
                    .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString();

                if (entity != null)
                {
                    if (entry.State == EntityState.Added)
                    {
                        entity.CreatedBy = userId;
                        entity.CreatedDate = DateTime.Now;

                        Log.Information("Entidad agregada - {Entity}, Id: {Id}, Usuario: {UserId}, Valores: {@Values}",
                            entityName,
                            primaryKey ?? "Desconocido",
                            userId ?? "Anonimo",
                            entry.CurrentValues.Properties.ToDictionary(p => p.Name, p => entry.CurrentValues[p]));
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        entity.UpdatedBy = userId;
                        entity.UpdatedDate = DateTime.Now;

                        var changes = new List<object>();

                        foreach (var prop in entry.Properties)
                        {
                            if (!Equals(prop.OriginalValue, prop.CurrentValue))
                            {
                                changes.Add(new
                                {
                                    Property = prop.Metadata.Name,
                                    OldValue = prop.OriginalValue,
                                    NewValue = prop.CurrentValue
                                });
                            }
                        }

                        if (changes.Any())
                        {
                            Log.Information("Entidad modificada - {Entity}, Id: {Id}, Usuario: {UserId}, Cambios: {@Changes}",
                                entityName,
                                primaryKey ?? "Desconocido",
                                userId ?? "Anonimo",
                                changes);
                        }
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        Log.Information("Entidad eliminada - {Entity}, Id: {Id}, Usuario: {UserId}, Valores anteriores: {@OldValues}",
                            entityName,
                            primaryKey ?? "Desconocido",
                            userId ?? "Anonimo",
                            entry.OriginalValues.Properties.ToDictionary(p => p.Name, p => entry.OriginalValues[p]));
                    }
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }


        // AM: Funcion SaveChangesAsync pero que omite el AuditService que se puede usar cuando el usuario no esta autenticado
        // Por ejemplo, se puede utilizar en el seeder ya que los campos de auditoria se pasan manualmente
        public async Task<int> SaveChangesWithoutAuditAsync( CancellationToken cancellationToken = default)
		{
         
            // AM: Omite cualquier lógica relacionada con AuditService.
            return await base.SaveChangesAsync(cancellationToken);
		}
       

        public DbSet<ActivityEntity> Activities { get; set; }
        public DbSet<AttendanceEntity> Attendances { get; set; }
        public DbSet<CenterEntity> Centers { get; set; }
        public DbSet<CourseEntity> Courses { get; set; }
        public DbSet<CourseNoteEntity> CoursesNotes { get; set; }
        public DbSet<CourseSettingEntity> CoursesSettings { get; set; }
        public DbSet<StudentActivityNoteEntity> StudentsActivitiesNotes { get; set; }
        public DbSet<StudentCourseEntity> StudentsCourses { get; set; }
        public DbSet<StudentUnitEntity> StudentsUnits { get; set; }
        public DbSet<StudentEntity> Students { get; set; }
		public DbSet<UnitEntity> Units { get; set; }
		public DbSet<TagActivityEntity> TagsActivities { get; set; }

    }
}
