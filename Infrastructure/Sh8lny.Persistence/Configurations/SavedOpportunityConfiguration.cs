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
    public class SavedOpportunityConfiguration : IEntityTypeConfiguration<SavedOpportunity>
    {
        public void Configure(EntityTypeBuilder<SavedOpportunity> builder)
        {
            builder.ToTable("SavedOpportunities");

            builder.HasKey(s => s.SavedID);

            builder.HasIndex(s => new { s.StudentID, s.ProjectID }).IsUnique();

            builder.Property(s => s.SavedAt).HasDefaultValueSql("GETDATE()");

            builder.HasOne(s => s.Student)
                   .WithMany(st => st.SavedOpportunities) 
                   .HasForeignKey(s => s.StudentID)
                   .OnDelete(DeleteBehavior.Cascade); 

            builder.HasOne(s => s.Project)
                   .WithMany(p => p.SavedOpportunities)
                   .HasForeignKey(s => s.ProjectID)
                   .OnDelete(DeleteBehavior.Cascade); 
        }
    }
}
