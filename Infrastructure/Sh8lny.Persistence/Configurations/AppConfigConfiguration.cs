using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sh8lny.Domain.Models;

namespace Sh8lny.Persistence.Configurations;

/// <summary>
/// EF Fluent API configuration for the AppConfig singleton table.
/// </summary>
public class AppConfigConfiguration : IEntityTypeConfiguration<AppConfig>
{
    public void Configure(EntityTypeBuilder<AppConfig> builder)
    {
        builder.ToTable("AppConfigs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.IsMaintenanceMode)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.MaintenanceTitle)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.MaintenanceMessage)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.MinSupportedVersion)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.UpdatedAt)
            .HasDefaultValueSql("GETDATE()");
    }
}
