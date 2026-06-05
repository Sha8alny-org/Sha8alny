using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sh8lny.Abstraction.Repositories;
using Sh8lny.Abstraction.Services;
using Sh8lny.Persistence.Contexts;
using Sh8lny.Persistence.Repositories;
using Sh8lny.Persistence.Seeding;
using Sh8lny.Persistence;
using Sh8lny.Service;
using Sh8lny.Shared.Options;
using Sh8lny.Web.Hubs;
using Sh8lny.Web.Logging;
using Sh8lny.Web.Mappings;
using Sh8lny.Web.Services;

namespace Sh8lny.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<ILoggerProvider, DiscordWebhookLoggerProvider>();
            builder.Logging.AddFilter<DiscordWebhookLoggerProvider>(null, LogLevel.Information);
            builder.Logging.AddFilter<DiscordWebhookLoggerProvider>("Microsoft.AspNetCore", LogLevel.Warning);
            builder.Logging.AddFilter<DiscordWebhookLoggerProvider>("System.Net.Http", LogLevel.Warning);
            builder.Logging.AddFilter<DiscordWebhookLoggerProvider>("Microsoft.Extensions.Http", LogLevel.Warning);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.SetIsOriginAllowed(_ => true) 
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials(); 
                });
            });

            // Swagger/OpenAPI - using Swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Sha8alny API",
                    Version = "v1",
                    Description = "Freelancing Platform API"
                });

                // Add JWT Authentication to Swagger
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter 'Bearer' followed by your JWT token"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // AutoMapper
            builder.Services.AddAutoMapper(typeof(MappingProfile));

            // JWT Configuration
            var jwtSettings = builder.Configuration.GetSection(JwtOptions.SectionName);
            builder.Services.Configure<JwtOptions>(jwtSettings);

            // Mail (SMTP) Configuration
            builder.Services.Configure<MailSettings>(
                builder.Configuration.GetSection(MailSettings.SectionName));

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
                };

                // SignalR JWT Authentication - read token from query string for WebSockets
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        // If the request is for our SignalR hub, read the token from query string
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddAuthorization();

            // SignalR
            builder.Services.AddSignalR();

            // Database Context
            builder.Services.AddDbContext<Sha8lnyDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Application Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IStudentService, StudentService>();
            builder.Services.AddScoped<ICompanyService, CompanyService>();
            builder.Services.AddScoped<IFileService, FileService>();
            builder.Services.AddScoped<IVirusScanService, ClamAvService>();
            builder.Services.AddScoped<IMailService, MailService>();
            builder.Services.AddScoped<IProjectService, ProjectService>();
            builder.Services.AddScoped<IApplicationService, ApplicationService>();
            builder.Services.AddScoped<IMasterDataService, MasterDataService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<ICertificateService, CertificateService>();
            builder.Services.AddScoped<IProjectExecutionService, ProjectExecutionService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IReviewService, ReviewService>();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();

            // Real-time notification service (SignalR)
            builder.Services.AddScoped<INotifier, SignalRNotifier>();

            // Database Backup
            builder.Services.AddScoped<IBackupService, BackupService>();
            builder.Services.AddHostedService<BackupWorker>();

            // App Configuration (Maintenance)
            builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();

            // Announcements
            builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();

            // Field Training Submissions
            builder.Services.AddScoped<ITrainingSubmissionService, TrainingSubmissionService>();

            var app = builder.Build();

            // Apply pending migrations and seed database with demo data
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Sha8lnyDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                try
                {
                    logger.LogInformation("Applying database migrations...");
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied successfully.");

                    logger.LogInformation("Seeding database...");
                    await DbInitializer.SeedAsync(context);
                    logger.LogInformation("Database seeding completed.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while migrating or seeding the database.");
                    throw;
                }
            }

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sha8alny API v1");
                options.RoutePrefix = string.Empty; // Swagger at root URL
            });

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            // Enable serving static files from wwwroot
            app.UseStaticFiles();

            app.Use(async (context, next) =>
            {
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    await next();
                }
                finally
                {
                    stopwatch.Stop();

                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation(
                        "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:0.0000} ms",
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode,
                        stopwatch.Elapsed.TotalMilliseconds);
                }
            });

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Map SignalR Hubs
            app.MapHub<NotificationHub>("/hubs/notifications");

            await app.RunAsync();
        }
    }
}
