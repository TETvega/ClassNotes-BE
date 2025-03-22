using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using ClassNotes.API.Database.Entities;

namespace ClassNotes.API.Database.Configuration
{
    public class CourseSettingConfiguration : IEntityTypeConfiguration<CourseSettingEntity>
    {
        public void Configure(EntityTypeBuilder<CourseSettingEntity> builder)
        {
            builder.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .HasPrincipalKey(e => e.Id);

            builder.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .HasPrincipalKey(e => e.Id);

            builder.HasOne(cs => cs.Course)
                .WithOne(c => c.CourseSetting) // relación uno a uno
                .HasForeignKey<CourseEntity>(c => c.SettingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
