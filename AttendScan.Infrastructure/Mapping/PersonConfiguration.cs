using AttendScan.core.Entities.People;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendScan.Infrastructure.Mapping;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> entity)
    {
        entity.ToTable("person");

        entity.HasKey(e => e.PersonId);
        entity.Property(e => e.PersonId).HasColumnName("person_id").ValueGeneratedOnAdd();

        entity.Property(e => e.CardCode).IsRequired().HasMaxLength(15).HasColumnName("card_code");
        entity.HasIndex(e => e.CardCode).IsUnique();

        entity.Property(e => e.ListNumber).HasColumnName("list_number");
        entity.Property(e => e.Region).HasMaxLength(50).HasColumnName("region");
        entity.Property(e => e.District).HasMaxLength(100).HasColumnName("district");
        entity.Property(e => e.Church).HasMaxLength(150).HasColumnName("church");
        entity.Property(e => e.PaternalSurname).IsRequired().HasMaxLength(80).HasColumnName("paternal_surname");
        entity.Property(e => e.MaternalSurname).HasMaxLength(80).HasColumnName("maternal_surname");
        entity.Property(e => e.GivenNames).IsRequired().HasMaxLength(120).HasColumnName("given_names");
        entity.Property(e => e.FullName).IsRequired().HasMaxLength(200).HasColumnName("full_name");
        entity.Property(e => e.Position).HasMaxLength(200).HasColumnName("position");
        entity.Property(e => e.DelegateType).HasMaxLength(50).HasColumnName("delegate_type");
        entity.Property(e => e.Age).HasColumnName("age");
        entity.Property(e => e.Gender).HasMaxLength(1).HasColumnName("gender");

        entity.Property(e => e.RegistrationDate)
              .IsRequired()
              .HasColumnName("registration_date")
              .HasColumnType("timestamp without time zone");

        entity.Property(e => e.AppUserId).HasColumnName("app_user_id");
    }
}
