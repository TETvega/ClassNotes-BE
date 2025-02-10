using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using ClassNotes.API.Database.Entities;

namespace ClassNotes.API.Database.Configuration
{
    public class ActivityConfiguration : IEntityTypeConfiguration<ActivityEntity>
    {
        public void Configure(EntityTypeBuilder<ActivityEntity> builder)
        {
           
            builder.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .HasPrincipalKey(e => e.Id);

            
            builder.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .HasPrincipalKey(e => e.Id);

            //DD: Relación entre ActivityEntity y StudentActivityNoteEntity
            builder.HasMany(a => a.StudentNotes)  
                .WithOne(san => san.Activity)      
                .HasForeignKey(san => san.ActivityId)
                .OnDelete(DeleteBehavior.Restrict);

            //DD: Relación entre ActivityEntity y CourseEntity
            builder.HasOne(a => a.Course)   
                .WithMany(c => c.Activities)
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
