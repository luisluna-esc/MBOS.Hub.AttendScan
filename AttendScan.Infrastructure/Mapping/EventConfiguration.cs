using AttendScan.core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendScan.Infrastructure.Mapping;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> entity)
    {
        entity.ToTable("event");

        entity.HasKey(e => e.EventId);
        entity.Property(e => e.EventId)
              .HasColumnName("event_id")
              .ValueGeneratedOnAdd();

        entity.Property(e => e.Name)
              .IsRequired()
              .HasMaxLength(150)
              .HasColumnName("name");

        entity.Property(e => e.Description)
              .HasMaxLength(500)
              .HasColumnName("description");

        entity.Property(e => e.StartDate)
              .IsRequired()
              .HasColumnName("start_date")
              .HasColumnType("date");

        entity.Property(e => e.EndDate)
              .HasColumnName("end_date")
              .HasColumnType("date");

        entity.Property(e => e.IsActive)
              .HasColumnName("is_active");

        entity.Property(e => e.RegistrationDate)
              .IsRequired()
              .HasColumnName("registration_date")
              .HasColumnType("timestamp without time zone");

        entity.Property(e => e.AppUserId)
              .HasColumnName("app_user_id");
    }
}
