using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using ClassNotes.API.Database.Entities;

namespace ClassNotes.API.Database.Configuration
{
    public class CourseConfiguration : IEntityTypeConfiguration<CourseEntity>
    {
        public void Configure(EntityTypeBuilder<CourseEntity> builder)
        {
            builder.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .HasPrincipalKey(e => e.Id);

            builder.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .HasPrincipalKey(e => e.Id);

            //DD:  Relación entre CourseEntity y CourseSettingEntity
            builder.HasOne(c => c.CourseSetting)
                .WithOne(cs => cs.Course) // Cambio a relación uno a uno
                .HasForeignKey<CourseEntity>(c => c.SettingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación entre CourseEntity y CenterEntity
            builder.HasOne(c => c.Center)
                .WithMany(ce => ce.Courses)
                .HasForeignKey(c => c.CenterId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación entre CourseEntity y StudentCourseEntity (Estudiantes inscritos en el curso)
            builder.HasMany(c => c.Students)
                .WithOne(sc => sc.Course)
                .HasForeignKey(sc => sc.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación entre CourseEntity y UnitEntity (Unidades del curso)
            builder.HasMany(c => c.Units)
                .WithOne(a => a.Course)
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación entre CourseEntity y CourseNoteEntity (Notas del curso)
            builder.HasMany(c => c.CourseNotes)
                .WithOne(cn => cn.Course)
                .HasForeignKey(cn => cn.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación entre CourseEntity y AttendanceEntity (Asistencias registradas en el curso)
            builder.HasMany(c => c.Attendances)
                .WithOne(a => a.Course)
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

