using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using ClassNotes.API.Database.Entities;

namespace ClassNotes.API.Database.Configuration
{
    public class AttendanceConfiguration : IEntityTypeConfiguration<AttendanceEntity>
    {
        public void Configure(EntityTypeBuilder<AttendanceEntity> builder)
        {
            
            builder.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .HasPrincipalKey(e => e.Id);

           
            builder.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .HasPrincipalKey(e => e.Id);

            //DD: Relación entre AttendanceEntity y CourseEntity
            builder.HasOne(a => a.Course)  
                .WithMany(c => c.Attendances) 
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            //DD: Relación entre AttendanceEntity y StudentEntity
            builder.HasOne(a => a.Student)   
                .WithMany(s => s.Attendances) 
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        }

    }
}
