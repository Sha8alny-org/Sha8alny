using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sh8lny.Domain.Models;

namespace Sh8lny.Persistence.Configurations
{
    public class DashboardMetricConfiguration : IEntityTypeConfiguration<DashboardMetric>
    {
        public void Configure(EntityTypeBuilder<DashboardMetric> builder)
        {
            // Table mapping
            builder.ToTable("DashboardMetrics");

            // Primary key
            builder.HasKey(dm => dm.MetricID);

            // User Statistics
            builder.Property(dm => dm.TotalStudents)
                .HasDefaultValue(0);

            builder.Property(dm => dm.TotalCompanies)
                .HasDefaultValue(0);

            builder.Property(dm => dm.TotalUsers)
                .HasDefaultValue(0);

            builder.Property(dm => dm.ActiveUsers)
                .HasDefaultValue(0);

            builder.Property(dm => dm.BannedUsers)
                .HasDefaultValue(0);

            // Project Statistics
            builder.Property(dm => dm.TotalProjects)
                .HasDefaultValue(0);

            builder.Property(dm => dm.ActiveProjects)
                .HasDefaultValue(0);

            builder.Property(dm => dm.ClosedProjects)
                .HasDefaultValue(0);

            // Application Statistics
            builder.Property(dm => dm.TotalApplications)
                .HasDefaultValue(0);

            builder.Property(dm => dm.CompletedApplications)
                .HasDefaultValue(0);

            // Financial Statistics
            builder.Property(dm => dm.TotalTransactionVolume)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m);

            builder.Property(dm => dm.TotalTransactions)
                .HasDefaultValue(0);

            // Recent Activity
            builder.Property(dm => dm.NewUsersLast30Days)
                .HasDefaultValue(0);

            builder.Property(dm => dm.NewProjectsLast30Days)
                .HasDefaultValue(0);

            // Timestamps
            builder.Property(dm => dm.MetricDate)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(dm => dm.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Indexes - unique on MetricDate to enforce one snapshot per day
            builder.HasIndex(dm => dm.MetricDate)
                .IsUnique()
                .HasDatabaseName("IDX_DashboardMetrics_MetricDate");
        }
    }
}
