using AttendScan.core.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendScan.Infrastructure.Mapping;

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> entity)
    {
        entity.ToTable("attendance");

        entity.HasKey(e => e.AttendanceId);
        entity.Property(e => e.AttendanceId)
              .HasColumnName("attendance_id")
              .ValueGeneratedOnAdd();

        entity.Property(e => e.EventSessionId)
              .IsRequired()
              .HasColumnName("event_session_id");

        entity.Property(e => e.PersonId)
              .IsRequired()
              .HasColumnName("person_id");

        // Una persona, una asistencia por sesion (tambien en la base: uq_attendance_session_person).
        entity.HasIndex(e => new { e.EventSessionId, e.PersonId }).IsUnique();

        entity.Property(e => e.ScannedAt)
              .IsRequired()
              .HasColumnName("scanned_at")
              .HasColumnType("timestamp without time zone");

        entity.Property(e => e.AppUserId)
              .HasColumnName("app_user_id");
    }
}
