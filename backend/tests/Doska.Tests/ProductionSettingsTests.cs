using Doska.Api.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Doska.Tests;

/// <summary>Проверяет обязательные настройки production.</summary>
public sealed class ProductionSettingsTests
{
    [Fact]
    public void Development_does_not_require_production_settings()
    {
        var configuration = BuildConfiguration();

        ProductionSettings.Validate(configuration, new TestEnvironment("Development"), "Log");
    }

    [Fact]
    public void Production_requires_public_base_url()
    {
        var configuration = BuildConfiguration(
            ("Email:Mode", "Smtp"),
            ("Email:Host", "smtp.example.com"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionSettings.Validate(configuration, new TestEnvironment("Production"), "Smtp"));

        Assert.Contains("App:PublicBaseUrl", exception.Message);
    }

    [Fact]
    public void Production_requires_absolute_public_base_url()
    {
        var configuration = BuildConfiguration(
            ("App:PublicBaseUrl", "doska.example.com"),
            ("Email:Mode", "Smtp"),
            ("Email:Host", "smtp.example.com"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionSettings.Validate(configuration, new TestEnvironment("Production"), "Smtp"));

        Assert.Contains("абсолютным", exception.Message);
    }

    [Fact]
    public void Production_requires_smtp()
    {
        var configuration = BuildConfiguration(
            ("App:PublicBaseUrl", "https://doska.example.com"),
            ("Email:Mode", "Log"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionSettings.Validate(configuration, new TestEnvironment("Production"), "Log"));

        Assert.Contains("SMTP", exception.Message);
    }

    [Fact]
    public void Valid_production_settings_pass()
    {
        var configuration = BuildConfiguration(
            ("App:PublicBaseUrl", "https://doska.example.com"),
            ("Email:Mode", "Smtp"),
            ("Email:Host", "smtp.example.com"));

        ProductionSettings.Validate(configuration, new TestEnvironment("Production"), "Smtp");
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();
    }

    private sealed class TestEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Doska.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}