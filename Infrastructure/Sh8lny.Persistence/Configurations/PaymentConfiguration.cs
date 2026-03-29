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
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            // Primary Key
            builder.HasKey(p => p.PaymentID);

            // Money Precision
            builder.Property(p => p.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(p => p.Currency)
                .HasMaxLength(3)
                .IsRequired();

            // --- FIX: Match the property names from your Entity ---

            // Paymob Order ID (was PaymentReference)
            builder.Property(p => p.PaymobOrderId)
                .HasMaxLength(100);

            // Paymob Transaction ID (was TransactionID)
            builder.Property(p => p.PaymobTransactionId)
                .HasMaxLength(100);

            // --- FIX: Index the correct property ---
            // Critical for finding the payment when Paymob sends the webhook
            builder.HasIndex(p => p.PaymobOrderId)
                .IsUnique(false);

            // Enums as Strings
            builder.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(p => p.PaymentMethod)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Relationships
            builder.HasOne(p => p.Project)
                .WithMany(project => project.Payments)
                .HasForeignKey(p => p.ProjectID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Student)
                .WithMany(student => student.Payments)
                .HasForeignKey(p => p.StudentID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Company)
                .WithMany(company => company.Payments)
                .HasForeignKey(p => p.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
