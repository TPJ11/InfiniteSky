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
        Assert.Equal(100, options.Networking.MaxConcurrentConnections);
        Assert.Equal(10_240, options.Networking.MaxReceiveBufferBytes);
        Assert.Equal(10_240, options.Networking.MaxFrameBytes);
        Assert.Equal(64, options.Networking.SendQueueCapacity);
        Assert.Equal(120, options.Networking.IdleTimeoutSeconds);
        Assert.Equal(30, options.Networking.ConnectionShutdownTimeoutSeconds);
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
    public void ValidatorRejectsUnimplementedDatabaseButAllowsValidatedNetworking()
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

    [Fact]
    public void ValidatorRejectsInvalidNetworkingLimits()
    {
        var validator = new InfiniteSkyOptionsValidator();
        var options = new InfiniteSkyOptions
        {
            Networking = new NetworkingOptions
            {
                BindAddress = "not-an-ip",
                LoginPort = 11091,
                WorldPort = 11091,
                MaxConcurrentConnections = 0,
                MaxReceiveBufferBytes = 8,
                MaxFrameBytes = 8,
                SendQueueCapacity = 0,
                IdleTimeoutSeconds = -1,
                ConnectionShutdownTimeoutSeconds = 0,
            },
        };

        ValidateOptionsResult result = validator.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, value => value.Contains("Network", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidatorRejectsFrameLimitAboveReceiveBufferLimit()
    {
        var validator = new InfiniteSkyOptionsValidator();
        var options = new InfiniteSkyOptions
        {
            Networking = new NetworkingOptions
            {
                MaxReceiveBufferBytes = 1024,
                MaxFrameBytes = 2048,
            },
        };

        ValidateOptionsResult result = validator.Validate(Options.DefaultName, options);

        Assert.NotNull(result.Failures);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                "MaxFrameBytes must not exceed MaxReceiveBufferBytes",
                StringComparison.Ordinal));
    }
}
