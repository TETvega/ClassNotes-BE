using ClassNotes.API.Database.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using ClassNotes.API.Database.Configuration;
using ClassNotes.API.Services.Audit;
using System.Diagnostics;
using Serilog;
using NetTopologySuite.Geometries;


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
        /// <summary>
        /// Guarda los cambios realizados en el contexto de base de datos de forma asincrónica, incluyendo registros detallados de auditoría (logs).
        /// </summary>
        /// <param name="cancellationToken">
        /// Token de cancelación que puede usarse para cancelar la operación de guardado.
        /// </param>
        /// <returns>
        /// Un <see cref="Task{TResult}"/> que representa la operación asincrónica. El resultado contiene el número de entidades escritas en la base de datos.
        /// </returns>
        /// <remarks>
        /// Este método extiende el comportamiento por defecto de <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> para incluir auditoría y logs
        /// Existe otro metodo sin los registros de auditoria , ya que en el estan siendo insertados manualmente y tienen que extraerse
        /// detallados de las entidades que han sido agregadas, modificadas o eliminadas.
        ///
        /// Para cada entidad que herede de <see cref="BaseEntity"/> y se encuentre en los estados <see cref="EntityState.Added"/>, 
        /// <see cref="EntityState.Modified"/> o <see cref="EntityState.Deleted"/>, se registran los siguientes detalles:
        ///
        /// - Para entidades agregadas: se asignan los campos <c>CreatedBy</c> y <c>CreatedDate</c>, y se logean todos sus valores actuales.
        /// - Para entidades modificadas: se asignan los campos <c>UpdatedBy</c> y <c>UpdatedDate</c>, y se registran únicamente las propiedades cuyo valor cambió.
        /// - Para entidades eliminadas: se logean los valores originales antes de ser eliminadas.
        ///
        /// Además, se hace un tratamiento especial para propiedades de tipo <see cref="NetTopologySuite.Geometries.Geometry"/> como <see cref="Point"/>,
        /// extrayendo únicamente coordenadas relevantes (X, Y) o el tipo de geometría para evitar problemas al serializar o registrar estructuras complejas.
        /// En el metodo de guardado se observo un bucle infinito y al ejecutar paso por paso se llego al momento de guardado con los registros de tallados (logs) los cuales intentaban descomponer este objeto typo BSON , pero entrabajn en un bucle infinito
        /// </remarks>
        /// 

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
                        // diccionario seguro
                        var safeValues = entry.CurrentValues.Properties.ToDictionary(p => p.Name, p =>
                        {
                            // guarga los mismos valores
                            var val = entry.CurrentValues[p];
                            // pero si es de tipo geometry solo extraemos X y Y
                            if (val is Geometry geometry)
                            {
                                if (geometry is Point point)
                                {
                                    return new { point.X, point.Y };
                                }
                                return geometry.GeometryType; 
                            }
                            return val;
                        });

                        Log.Information("Entidad agregada - {Entity}, Id: {Id}, Usuario: {UserId}, Valores: {@Values}",
                            entityName,
                            primaryKey ?? "Desconocido",
                            userId ?? "Anonimo",
                            safeValues);
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
                                object oldValue = prop.OriginalValue;
                                object newValue = prop.CurrentValue;

                                // Evitar serialización compleja de geometrías
                                if (prop.OriginalValue is Geometry gOld)
                                    oldValue = gOld is Point pOld ? new { pOld.X, pOld.Y } : gOld.GeometryType;
                                if (prop.CurrentValue is Geometry gNew)
                                    newValue = gNew is Point pNew ? new { pNew.X, pNew.Y } : gNew.GeometryType;

                                changes.Add(new
                                {
                                    Property = prop.Metadata.Name,
                                    OldValue = oldValue,
                                    NewValue = newValue
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
                        var safeOldValues = entry.OriginalValues.Properties.ToDictionary(p => p.Name, p =>
                        {
                            var val = entry.OriginalValues[p];
                            if (val is Geometry geometry)
                            {
                                if (geometry is Point point)
                                {
                                    return new { point.X, point.Y };
                                }
                                return geometry.GeometryType;
                            }
                            return val;
                        });

                        Log.Information("Entidad eliminada - {Entity}, Id: {Id}, Usuario: {UserId}, Valores anteriores: {@OldValues}",
                            entityName,
                            primaryKey ?? "Desconocido",
                            userId ?? "Anonimo",
                            safeOldValues);
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
