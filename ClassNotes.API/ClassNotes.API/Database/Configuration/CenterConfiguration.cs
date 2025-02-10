using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using ClassNotes.API.Database.Entities;

namespace ClassNotes.API.Database.Configuration
{
    public class CenterConfiguration : IEntityTypeConfiguration<CenterEntity>
    {
        public void Configure(EntityTypeBuilder<CenterEntity> builder)
        {
          
            builder.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .HasPrincipalKey(e => e.Id);

            
            builder.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .HasPrincipalKey(e => e.Id);

            //DD: Relación entre CenterEntity y Teacher (Profesor)
            builder.HasOne(c => c.Teacher)   
                .WithMany(t => t.Centers)    
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // DD: Relación entre CenterEntity y CourseEntity (Cursos que se imparten en el centro)
            builder.HasMany(c => c.Courses)  
                .WithOne(c => c.Center)     
                .HasForeignKey(c => c.CenterId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
