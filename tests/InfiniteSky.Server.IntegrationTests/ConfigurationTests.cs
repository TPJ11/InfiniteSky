using InfiniteSky.Server.Configuration;
using InfiniteSky.Server.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InfiniteSky.Server.IntegrationTests;

public sealed class ConfigurationTests
{
    [Fact]
    public void DefaultFoundationConfigurationBindsAndValidates()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["InfiniteSky:Application:Name"] = "ConfigurationTest",
            ["InfiniteSky:Zones:Load:0"] = "1",
        });
        using IHost host = builder.Build();

        InfiniteSkyOptions options = host.Services.GetRequiredService<IOptions<InfiniteSkyOptions>>().Value;

        Assert.Equal("ConfigurationTest", options.Application.Name);
        Assert.Equal(100, options.World.TickMilliseconds);
        Assert.Equal([1], options.Zones.Load);
        Assert.Equal("LegacyParity", options.Gameplay.CompatibilityProfile);
        Assert.False(options.Hosting.StartServicesConcurrently);
        Assert.False(options.Hosting.StopServicesConcurrently);
        Assert.Equal("InfiniteSky", options.Diagnostics.ServiceName);
        Assert.False(options.Networking.Enabled);
        Assert.False(options.Database.Enabled);
    }

    [Fact]
    public void ShutdownTimeoutConfiguresTheGenericHost()
    {
        HostApplicationBuilder builder = ServerHostBuilder.Create();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["InfiniteSky:Application:ShutdownTimeoutSeconds"] = "7",
        });
        using IHost host = builder.Build();

        HostOptions options = host.Services.GetRequiredService<IOptions<HostOptions>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(7), options.ShutdownTimeout);
    }

    [Fact]
    public void ValidatorRejectsUnimplementedExternalSubsystems()
    {
        var validator = new InfiniteSkyOptionsValidator();
        var options = new InfiniteSkyOptions
        {
            Networking = new NetworkingOptions { Enabled = true },
            Database = new DatabaseOptions { Enabled = true },
        };

        ValidateOptionsResult result = validator.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("Networking cannot be enabled", StringComparison.Ordinal));
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("Database access cannot be enabled", StringComparison.Ordinal));
    }

    [Fact]
    public void EnvironmentVariablesOverrideBaseConfiguration()
    {
        const string variableName = "INFINITESKY_InfiniteSky__Application__Name";
        Environment.SetEnvironmentVariable(variableName, "EnvironmentName");

        try
        {
            HostApplicationBuilder builder = ServerHostBuilder.Create();
            using IHost host = builder.Build();
            InfiniteSkyOptions options = host.Services.GetRequiredService<IOptions<InfiniteSkyOptions>>().Value;

            Assert.Equal("EnvironmentName", options.Application.Name);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }
}
