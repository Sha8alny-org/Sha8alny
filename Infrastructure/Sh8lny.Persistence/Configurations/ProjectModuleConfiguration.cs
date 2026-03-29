using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sh8lny.Domain.Models;

namespace Sh8lny.Persistence.Configurations
{
    public class ProjectModuleConfiguration : IEntityTypeConfiguration<ProjectModule>
    {
        public void Configure(EntityTypeBuilder<ProjectModule> builder)
        {
            builder.ToTable("ProjectModules");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(m => m.Description)
                .HasMaxLength(2000);

            builder.Property(m => m.EstimatedDuration)
                .HasMaxLength(100);

            builder.Property(m => m.OrderIndex)
                .IsRequired();

            builder.Property(m => m.Weight)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.HasIndex(m => new { m.ProjectId, m.OrderIndex })
                .HasDatabaseName("IDX_ProjectModules_Project_Order");

            builder.HasOne(m => m.Project)
                .WithMany(p => p.Modules)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
