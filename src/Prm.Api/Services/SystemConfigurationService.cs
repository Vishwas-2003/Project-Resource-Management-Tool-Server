using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Prm.Common.Models.SystemConfigurations;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Prm.Api.Services;

public class SystemConfigurationService(
    ISystemConfigurationRepository _systemConfigurationRepository,
    IMapper _mapper,
    IHangfireJobScheduler _hangfireJobScheduler) : ISystemConfigurationService
{
    public async Task<IReadOnlyList<SystemConfigurationResponse>> GetAllConfigurations(
        CancellationToken cancellationToken = default)
    {
        var configurations = await _systemConfigurationRepository.GetAll(cancellationToken);

        return _mapper.Map<IReadOnlyList<SystemConfigurationResponse>>(configurations);
    }

    public async Task<bool> Update(
        int configurationId,
        string value,
        CancellationToken cancellationToken = default)
    {
        var configuration = await GetConfigurationOrThrow(configurationId, cancellationToken);

        ValidateConfiguration(configurationId, value, configuration);

        configuration.Value = value;
        _systemConfigurationRepository.Update(configuration);
        await _systemConfigurationRepository.SaveChanges(cancellationToken);

        if (configurationId == (int)ConfigurationOptionEnum.SchedulerInterval
            && int.TryParse(value, out var intervalMinutes))
        {
            _hangfireJobScheduler.RescheduleScheduler(intervalMinutes);
        }

        return true;
    }

    private async Task<SystemConfiguration> GetConfigurationOrThrow(int configurationId, CancellationToken cancellationToken)
    {
        var systemConfiguration = await _systemConfigurationRepository.GetById(configurationId, cancellationToken);

        if (systemConfiguration is null)
        {
            throw new KeyNotFoundException(AppConstants.SystemConfiguration.NotFound);
        }

        return systemConfiguration;
    }

    private void ValidateConfiguration(int configurationId, string value, SystemConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(AppConstants.SystemConfiguration.InvalidValue);
        }

        if (configuration.Value == value)
        {
            throw new ArgumentException(AppConstants.SystemConfiguration.ValueUnchanged);
        }

        if (configurationId == (int)ConfigurationOptionEnum.MaxWeeklyHours || configurationId == (int)ConfigurationOptionEnum.SchedulerInterval)
        {
            int convertedValue;
            if (!int.TryParse(value, out convertedValue))
            {
                throw new ValidationException("Value must be a valid integer.");
            }

            if (convertedValue <=0)
            {
                throw new ValidationException("Value must be a greater than 0.");
            }
        }
    }
}
