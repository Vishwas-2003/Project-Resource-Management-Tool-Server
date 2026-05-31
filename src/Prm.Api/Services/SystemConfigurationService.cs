using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Prm.Api.Services;

public class SystemConfigurationService(
    ISystemConfigurationRepository _systemConfigurationRepository,
    IPasswordHasher<SystemConfiguration> _hasher,
    IMapper mapper) : ISystemConfigurationService
{
    private readonly IMapper _mapper = mapper;
    private readonly ISystemConfigurationRepository _systemConfigurationRepository = _systemConfigurationRepository;
    private readonly IPasswordHasher<SystemConfiguration> _hasher = _hasher;
    public async Task<bool> Update(
        int configurationId,
        string value,
        CancellationToken cancellationToken = default)
    {
        var configuration = await GetConfigurationOrThrow(configurationId, cancellationToken);

        ValidateConfiguration(configurationId, value, configuration);

        if (configuration.ConfigurationType == nameof(ConfigurationOptionEnum.ApiKey))
        {
            value = _hasher.HashPassword(configuration, value);
        }

        configuration.Value = value;
        _systemConfigurationRepository.Update(configuration);
        await _systemConfigurationRepository.SaveChanges(cancellationToken);

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

        if (configuration.ConfigurationType == nameof(ConfigurationOptionEnum.ApiKey))
        {
            if (_hasher.VerifyHashedPassword(configuration, configuration.Value, value)
                != PasswordVerificationResult.Failed)
            {
                throw new InvalidOperationException(AppConstants.SystemConfiguration.ValueUnchanged);
            }
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
