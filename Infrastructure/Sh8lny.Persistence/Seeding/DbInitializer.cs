using Microsoft.EntityFrameworkCore;
using Sh8lny.Domain.Models;
using Sh8lny.Persistence.Contexts;
using BC = BCrypt.Net.BCrypt;

namespace Sh8lny.Persistence.Seeding;

/// <summary>
/// Database initializer for seeding demo data.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Seeds the database with initial demo data.
    /// </summary>
    /// <param name="context">The database context.</param>
    public static async Task SeedAsync(Sha8lnyDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // ==================== MASTER DATA ====================

        // Seed Skills
        if (!await context.Skills.AnyAsync())
        {
            var skills = new List<Skill>
            {
                new Skill { SkillName = "C#", SkillCategory = SkillCategory.Backend, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Skill { SkillName = "ASP.NET Core", SkillCategory = SkillCategory.Backend, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Skill { SkillName = "React", SkillCategory = SkillCategory.Frontend, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Skill { SkillName = "Angular", SkillCategory = SkillCategory.Frontend, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Skill { SkillName = "SQL", SkillCategory = SkillCategory.Data, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Skill { SkillName = "Python", SkillCategory = SkillCategory.Backend, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Skill { SkillName = "JavaScript", SkillCategory = SkillCategory.Frontend, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Skill { SkillName = "TypeScript", SkillCategory = SkillCategory.Frontend, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Skill { SkillName = "Node.js", SkillCategory = SkillCategory.Backend, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Skill { SkillName = "Flutter", SkillCategory = SkillCategory.Mobile, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Skill { SkillName = "Docker", SkillCategory = SkillCategory.Backend, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Skill { SkillName = "Azure", SkillCategory = SkillCategory.Backend, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Skill { SkillName = "Machine Learning", SkillCategory = SkillCategory.AIML, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Skill { SkillName = "Figma", SkillCategory = SkillCategory.UIUX, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Skill { SkillName = "Git", SkillCategory = SkillCategory.Other, IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            await context.Skills.AddRangeAsync(skills);
            await context.SaveChangesAsync();
        }

        // Seed Universities
        if (!await context.Universities.AnyAsync())
        {
            var universities = new List<University>
            {
                new University
                {
                    UniversityName = "Cairo University",
                    ContactEmail = "info@cu.edu.eg",
                    Website = "https://cu.edu.eg",
                    City = "Giza",
                    Country = "Egypt",
                    UniversityType = UniversityType.Public,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new University
                {
                    UniversityName = "German University in Cairo (GUC)",
                    ContactEmail = "info@guc.edu.eg",
                    Website = "https://guc.edu.eg",
                    City = "New Cairo",
                    Country = "Egypt",
                    UniversityType = UniversityType.Private,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new University
                {
                    UniversityName = "American University in Cairo (AUC)",
                    ContactEmail = "info@aucegypt.edu",
                    Website = "https://aucegypt.edu",
                    City = "New Cairo",
                    Country = "Egypt",
                    UniversityType = UniversityType.International,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Universities.AddRangeAsync(universities);
            await context.SaveChangesAsync();
        }

        // ==================== USERS ====================

        var passwordHash = BC.HashPassword("Password123!");

        // Seed Admin User
        User? adminUser = null;
        if (!await context.Users.AnyAsync(u => u.Email == "admin@sha8alny.com"))
        {
            adminUser = new User
            {
                Email = "admin@sha8alny.com",
                PasswordHash = passwordHash,
                FirstName = "System",
                LastName = "Admin",
                UserType = UserType.Admin,
                IsEmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.Users.AddAsync(adminUser);
            await context.SaveChangesAsync();
        }
        else
        {
            adminUser = await context.Users.FirstAsync(u => u.Email == "admin@sha8alny.com");
        }

        // Seed Company User
        User? companyUser = null;
        Company? techCorpCompany = null;
        if (!await context.Users.AnyAsync(u => u.Email == "techcorp@test.com"))
        {
            companyUser = new User
            {
                Email = "techcorp@test.com",
                PasswordHash = passwordHash,
                FirstName = "TechCorp",
                LastName = "HR",
                UserType = UserType.Company,
                IsEmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.Users.AddAsync(companyUser);
            await context.SaveChangesAsync();

            // Create Company Profile
            techCorpCompany = new Company
            {
                UserID = companyUser.UserID,
                CompanyName = "TechCorp Solutions",
                ContactEmail = "techcorp@test.com",
                ContactPhone = "+20 123 456 7890",
                Website = "https://techcorp.example.com",
                Address = "123 Tech Street",
                City = "Cairo",
                State = "Cairo",
                Country = "Egypt",
                Industry = "Software Development",
                Description = "TechCorp Solutions is a leading software development company specializing in enterprise solutions, web applications, and mobile development.",
                AverageRating = 4.5m,
                TotalReviews = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.Companies.AddAsync(techCorpCompany);
            await context.SaveChangesAsync();
        }
        else
        {
            companyUser = await context.Users.FirstAsync(u => u.Email == "techcorp@test.com");
            techCorpCompany = await context.Companies.FirstAsync(c => c.UserID == companyUser.UserID);
        }

        // Seed Student User
        User? studentUser = null;
        Student? studentProfile = null;
        if (!await context.Users.AnyAsync(u => u.Email == "student@test.com"))
        {
            studentUser = new User
            {
                Email = "student@test.com",
                PasswordHash = passwordHash,
                FirstName = "Ahmed",
                LastName = "Hassan",
                UserType = UserType.Student,
                IsEmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.Users.AddAsync(studentUser);
            await context.SaveChangesAsync();

            // Get Cairo University for the student
            var cairoUniversity = await context.Universities.FirstAsync(u => u.UniversityName == "Cairo University");

            // Create Student Profile
            studentProfile = new Student
            {
                UserID = studentUser.UserID,
                FirstName = "Ahmed",
                LastName = "Hassan",
                Bio = "Passionate software engineering student with experience in full-stack development. Eager to learn and contribute to innovative projects.",
                Phone = "+20 100 123 4567",
                Country = "Egypt",
                City = "Cairo",
                UniversityID = cairoUniversity.UniversityID,
                AcademicYear = AcademicYear.ThirdYear,
                Status = StudentStatus.Active,
                ProfileCompleteness = 85,
                AverageRating = 4.8m,
                TotalReviews = 5,
                GitHubProfile = "https://github.com/ahmedhassan",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.Students.AddAsync(studentProfile);
            await context.SaveChangesAsync();

            // Link Skills to Student
            var csharpSkill = await context.Skills.FirstAsync(s => s.SkillName == "C#");
            var aspnetSkill = await context.Skills.FirstAsync(s => s.SkillName == "ASP.NET Core");
            var reactSkill = await context.Skills.FirstAsync(s => s.SkillName == "React");
            var sqlSkill = await context.Skills.FirstAsync(s => s.SkillName == "SQL");
            var gitSkill = await context.Skills.FirstAsync(s => s.SkillName == "Git");

            var studentSkills = new List<StudentSkill>
            {
                new StudentSkill { StudentID = studentProfile.StudentID, SkillID = csharpSkill.SkillID, ProficiencyLevel = ProficiencyLevel.Advanced, CreatedAt = DateTime.UtcNow },
                new StudentSkill { StudentID = studentProfile.StudentID, SkillID = aspnetSkill.SkillID, ProficiencyLevel = ProficiencyLevel.Intermediate, CreatedAt = DateTime.UtcNow },
                new StudentSkill { StudentID = studentProfile.StudentID, SkillID = reactSkill.SkillID, ProficiencyLevel = ProficiencyLevel.Intermediate, CreatedAt = DateTime.UtcNow },
                new StudentSkill { StudentID = studentProfile.StudentID, SkillID = sqlSkill.SkillID, ProficiencyLevel = ProficiencyLevel.Advanced, CreatedAt = DateTime.UtcNow },
                new StudentSkill { StudentID = studentProfile.StudentID, SkillID = gitSkill.SkillID, ProficiencyLevel = ProficiencyLevel.Advanced, CreatedAt = DateTime.UtcNow }
            };

            await context.StudentSkills.AddRangeAsync(studentSkills);
            await context.SaveChangesAsync();
        }
        else
        {
            studentUser = await context.Users.FirstAsync(u => u.Email == "student@test.com");
            studentProfile = await context.Students.FirstAsync(s => s.UserID == studentUser.UserID);
        }

        // ==================== PROJECTS ====================

        // Project 1: Active Internship
        Project? internshipProject = null;
        if (!await context.Projects.AnyAsync(p => p.ProjectName == "Backend Internship"))
        {
            internshipProject = new Project
            {
                CompanyID = techCorpCompany!.CompanyID,
                ProjectName = "Backend Internship",
                ProjectCode = "INT-2026-001",
                Description = "Join our backend team for a 3-month internship program. You'll work on real-world projects using C#, ASP.NET Core, and SQL Server. Great opportunity to learn enterprise development practices.",
                ProjectType = ProjectType.Internship,
                Deadline = DateTime.UtcNow.AddMonths(2),
                Duration = "3 months",
                RequiredSkills = "C#, ASP.NET Core, SQL",
                MinAcademicYear = "SecondYear",
                MaxApplicants = 5,
                Status = ProjectStatus.Active,
                IsVisible = true,
                CreatedBy = companyUser!.UserID,
                CreatedByName = "TechCorp HR",
                ViewCount = 150,
                ApplicationCount = 3,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow
            };

            await context.Projects.AddAsync(internshipProject);
            await context.SaveChangesAsync();

            // Add required skills to the internship project
            var csharpSkill = await context.Skills.FirstAsync(s => s.SkillName == "C#");
            var aspnetSkill = await context.Skills.FirstAsync(s => s.SkillName == "ASP.NET Core");
            var sqlSkill = await context.Skills.FirstAsync(s => s.SkillName == "SQL");

            var projectSkills = new List<ProjectRequiredSkill>
            {
                new ProjectRequiredSkill { ProjectID = internshipProject.ProjectID, SkillID = csharpSkill.SkillID, IsRequired = true, CreatedAt = DateTime.UtcNow },
                new ProjectRequiredSkill { ProjectID = internshipProject.ProjectID, SkillID = aspnetSkill.SkillID, IsRequired = true, CreatedAt = DateTime.UtcNow },
                new ProjectRequiredSkill { ProjectID = internshipProject.ProjectID, SkillID = sqlSkill.SkillID, IsRequired = false, CreatedAt = DateTime.UtcNow }
            };

            await context.ProjectRequiredSkills.AddRangeAsync(projectSkills);
            await context.SaveChangesAsync();
        }

        // Project 2: Completed Fixed Price Project
        Project? ecommerceProject = null;
        Application? completedApplication = null;
        if (!await context.Projects.AnyAsync(p => p.ProjectName == "Build E-commerce"))
        {
            ecommerceProject = new Project
            {
                CompanyID = techCorpCompany!.CompanyID,
                ProjectName = "Build E-commerce",
                ProjectCode = "PROJ-2025-042",
                Description = "Build a complete e-commerce platform with product catalog, shopping cart, checkout, and admin dashboard. Technologies: React frontend, ASP.NET Core backend, SQL Server database.",
                ProjectType = ProjectType.FullTime,
                StartDate = DateTime.UtcNow.AddMonths(-3),
                EndDate = DateTime.UtcNow.AddDays(-5),
                Deadline = DateTime.UtcNow.AddDays(-5),
                Duration = "3 months",
                RequiredSkills = "React, C#, ASP.NET Core, SQL",
                Status = ProjectStatus.Closed,
                IsVisible = false,
                CreatedBy = companyUser!.UserID,
                CreatedByName = "TechCorp HR",
                ViewCount = 320,
                ApplicationCount = 12,
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            };

            await context.Projects.AddAsync(ecommerceProject);
            await context.SaveChangesAsync();

            // ==================== WORKFLOW DATA (Completed Project) ====================

            // Create completed Application
            completedApplication = new Application
            {
                ProjectID = ecommerceProject.ProjectID,
                StudentID = studentProfile!.StudentID,
                CoverLetter = "I am excited to apply for this e-commerce project. With my experience in React and ASP.NET Core, I am confident I can deliver a high-quality solution.",
                Resume = "/uploads/resumes/ahmed_hassan_cv.pdf",
                StudentCvUrl = "/uploads/resumes/ahmed_hassan_cv.pdf",
                ProposalFileUrl = "/uploads/proposals/ahmed_hassan_ecommerce_proposal.pdf",
                PortfolioURL = "https://github.com/ahmedhassan",
                BidAmount = 15000m,
                Status = ApplicationStatus.Completed,
                ReviewedBy = companyUser.UserID,
                ReviewedAt = DateTime.UtcNow.AddMonths(-3).AddDays(2),
                ReviewNotes = "Strong candidate with relevant experience. Accepted.",
                CompletedAt = DateTime.UtcNow.AddDays(-5),
                CompanyFeedbackNote = "Excellent work! Ahmed delivered the project on time with high quality. Highly recommended.",
                FinalDeliverableUrl = "https://github.com/techcorp/ecommerce-platform",
                IsPaid = true,
                PaidAt = DateTime.UtcNow.AddDays(-4),
                AppliedAt = DateTime.UtcNow.AddMonths(-3)
            };

            await context.Applications.AddAsync(completedApplication);
            await context.SaveChangesAsync();

            // Create Transaction (Paid)
            var transaction = new Transaction
            {
                ApplicationId = completedApplication.ApplicationID,
                PayerId = companyUser.UserID,
                PayeeId = studentUser!.UserID,
                Amount = 15000m,
                TransactionDate = DateTime.UtcNow.AddDays(-4),
                PaymentMethod = "Credit Card",
                ReferenceId = $"TXN-{Guid.NewGuid().ToString("N").ToUpper().Substring(0, 12)}",
                Status = TransactionStatus.Completed
            };

            await context.Transactions.AddAsync(transaction);
            await context.SaveChangesAsync();

            // Create Certificate
            var certificate = new Certificate
            {
                StudentID = studentProfile.StudentID,
                ProjectID = ecommerceProject.ProjectID,
                CompanyID = techCorpCompany.CompanyID,
                CertificateNumber = $"CERT-{Guid.NewGuid().ToString("N").ToUpper().Substring(0, 12)}",
                CertificateTitle = "Certificate of Completion - Full Time Project",
                Description = $"This certificate is awarded to Ahmed Hassan for successfully completing the full time project 'Build E-commerce' with TechCorp Solutions.",
                CertificateURL = "/certificates/verify/",
                IssuedAt = DateTime.UtcNow.AddDays(-4)
            };

            // Update certificate URL with the number
            certificate.CertificateURL = $"/certificates/verify/{certificate.CertificateNumber}";

            await context.Certificates.AddAsync(certificate);
            await context.SaveChangesAsync();
        }

        Console.WriteLine("✅ Database seeding completed successfully!");
    }
}
