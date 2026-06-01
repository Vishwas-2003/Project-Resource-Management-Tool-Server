using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Moq;
using Prm.Api.Services;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Api.Tests.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Prm.Api.Tests.Services;

public class SystemConfigurationServiceTests
{
    private readonly Mock<ISystemConfigurationRepository> _repository = new();
    private readonly IPasswordHasher<SystemConfiguration> _hasher = new PasswordHasher<SystemConfiguration>();
    private readonly IMapper _mapper = MapperTestHelper.CreateMapper();

    [Fact]
    public async Task Update_WhenConfigurationNotFound_ThrowsKeyNotFoundException()
    {
        _repository
            .Setup(x => x.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemConfiguration?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.Update(1, "new-value"));

        Assert.Equal(AppConstants.SystemConfiguration.NotFound, exception.Message);
    }

    [Fact]
    public async Task Update_WhenValueIsEmpty_ThrowsArgumentException()
    {
        var configuration = ApiTestData.CreateConfiguration((int)ConfigurationOptionEnum.Provider, "SMTP");
        _repository
            .Setup(x => x.GetById(configuration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Update(configuration.Id, "   "));

        Assert.Equal(AppConstants.SystemConfiguration.InvalidValue, exception.Message);
    }

    [Fact]
    public async Task Update_WhenValueUnchanged_ThrowsArgumentException()
    {
        var configuration = ApiTestData.CreateConfiguration((int)ConfigurationOptionEnum.Provider, "SMTP");
        _repository
            .Setup(x => x.GetById(configuration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Update(configuration.Id, "SMTP"));

        Assert.Equal(AppConstants.SystemConfiguration.ValueUnchanged, exception.Message);
    }

    [Fact]
    public async Task Update_WhenMaxWeeklyHoursIsNotInteger_ThrowsValidationException()
    {
        var configuration = ApiTestData.CreateConfiguration((int)ConfigurationOptionEnum.MaxWeeklyHours, "40");
        _repository
            .Setup(x => x.GetById(configuration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        var sut = CreateSut();

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.Update(configuration.Id, "not-a-number"));
    }

    [Fact]
    public async Task Update_WhenMaxWeeklyHoursIsZero_ThrowsValidationException()
    {
        var configuration = ApiTestData.CreateConfiguration((int)ConfigurationOptionEnum.MaxWeeklyHours, "40");
        _repository
            .Setup(x => x.GetById(configuration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        var sut = CreateSut();

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.Update(configuration.Id, "0"));
    }

    [Fact]
    public async Task Update_WhenMaxWeeklyHoursValid_UpdatesValue()
    {
        var configuration = ApiTestData.CreateConfiguration((int)ConfigurationOptionEnum.MaxWeeklyHours, "40");
        _repository
            .Setup(x => x.GetById(configuration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        var sut = CreateSut();
        var result = await sut.Update(configuration.Id, "45");

        Assert.True(result);
        Assert.Equal("45", configuration.Value);
        _repository.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WhenApiKey_StoresHashedValue()
    {
        var configuration = ApiTestData.CreateConfiguration(
            (int)ConfigurationOptionEnum.ApiKey,
            _hasher.HashPassword(
                new SystemConfiguration { Id = (int)ConfigurationOptionEnum.ApiKey, ConfigurationType = "ApiKey" },
                "old-key"),
            "ApiKey");

        _repository
            .Setup(x => x.GetById(configuration.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        var sut = CreateSut();
        var result = await sut.Update(configuration.Id, "new-secret-key");

        Assert.True(result);
        Assert.NotEqual("new-secret-key", configuration.Value);
        Assert.Equal(PasswordVerificationResult.Success,
            _hasher.VerifyHashedPassword(configuration, configuration.Value, "new-secret-key"));
    }

    [Fact]
    public async Task GetAllConfigurations_ReturnsMappedList()
    {
        var configurations = new List<SystemConfiguration>
        {
            ApiTestData.CreateConfiguration((int)ConfigurationOptionEnum.Provider, "SMTP", "Provider"),
        };

        _repository
            .Setup(x => x.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configurations);

        var sut = CreateSut();
        var result = await sut.GetAllConfigurations();

        Assert.Single(result);
        Assert.Equal("SMTP", result[0].Value);
        Assert.Equal("Provider", result[0].ConfigurationType);
    }

    private SystemConfigurationService CreateSut() =>
        new(_repository.Object, _hasher, _mapper);
}
