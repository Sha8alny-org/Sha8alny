using Microsoft.Extensions.Logging;
using Sh8lny.Abstraction.Repositories;
using Sh8lny.Abstraction.Services;
using Sh8lny.Domain.Models;
using Sh8lny.Shared.DTOs.Common;
using Sh8lny.Shared.DTOs.Maintenance;

namespace Sh8lny.Service;

/// <summary>
/// Manages the singleton <see cref="AppConfig"/> row (maintenance mode, version gate).
/// </summary>
public class MaintenanceService : IMaintenanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MaintenanceService> _logger;

    public MaintenanceService(IUnitOfWork unitOfWork, ILogger<MaintenanceService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<AppConfigDto>> GetAppConfigAsync()
    {
        try
        {
            var config = await GetOrCreateConfigAsync();

            var dto = new AppConfigDto
            {
                IsMaintenanceMode = config.IsMaintenanceMode,
                MaintenanceTitle = config.MaintenanceTitle,
                MaintenanceMessage = config.MaintenanceMessage,
                MinSupportedVersion = config.MinSupportedVersion
            };

            return ServiceResponse<AppConfigDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving app configuration.");
            return ServiceResponse<AppConfigDto>.Failure("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> UpdateAppConfigAsync(UpdateAppConfigDto dto)
    {
        try
        {
            var config = await GetOrCreateConfigAsync();

            config.IsMaintenanceMode = dto.IsMaintenanceMode;
            config.MaintenanceTitle = dto.MaintenanceTitle;
            config.MaintenanceMessage = dto.MaintenanceMessage;
            config.MinSupportedVersion = dto.MinSupportedVersion;
            config.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.AppConfigs.Update(config);
            await _unitOfWork.SaveAsync();

            return ServiceResponse<bool>.Success(true, "App configuration updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating app configuration.");
            return ServiceResponse<bool>.Failure("An unexpected error occurred.");
        }
    }

    /// <summary>
    /// Fetches the singleton AppConfig row (Id = 1). Creates a default if none exists.
    /// </summary>
    private async Task<AppConfig> GetOrCreateConfigAsync()
    {
        var config = await _unitOfWork.AppConfigs.FindSingleAsync(_ => true);

        if (config is null)
        {
            config = new AppConfig
            {
                IsMaintenanceMode = false,
                MaintenanceTitle = "Under Maintenance",
                MaintenanceMessage = "We are currently performing scheduled maintenance. Please try again later.",
                MinSupportedVersion = "1.0.0",
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.AppConfigs.AddAsync(config);
            await _unitOfWork.SaveAsync();
            _logger.LogInformation("Created default AppConfig row.");
        }

        return config;
    }
}
