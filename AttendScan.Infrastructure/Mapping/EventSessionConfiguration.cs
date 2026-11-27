using AttendScan.core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendScan.Infrastructure.Mapping;

public class EventSessionConfiguration : IEntityTypeConfiguration<EventSession>
{
    public void Configure(EntityTypeBuilder<EventSession> entity)
    {
        entity.ToTable("event_session");

        entity.HasKey(e => e.EventSessionId);
        entity.Property(e => e.EventSessionId)
              .HasColumnName("event_session_id")
              .ValueGeneratedOnAdd();

        entity.Property(e => e.EventId)
              .IsRequired()
              .HasColumnName("event_id");

        entity.Property(e => e.Name)
              .IsRequired()
              .HasMaxLength(100)
              .HasColumnName("name");

        entity.Property(e => e.SessionDate)
              .IsRequired()
              .HasColumnName("session_date")
              .HasColumnType("date");

        entity.Property(e => e.IsOpen)
              .HasColumnName("is_open");

        entity.Property(e => e.RegistrationDate)
              .IsRequired()
              .HasColumnName("registration_date")
              .HasColumnType("timestamp without time zone");

        entity.Property(e => e.AppUserId)
              .HasColumnName("app_user_id");
    }
}
