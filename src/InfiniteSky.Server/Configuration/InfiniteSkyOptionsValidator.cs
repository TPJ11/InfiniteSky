using System.Net;

using Microsoft.Extensions.Options;

namespace InfiniteSky.Server.Configuration;

/// <summary>
/// Validates foundation configuration and prevents unimplemented subsystems from being reported as active.
/// </summary>
public sealed class InfiniteSkyOptionsValidator : IValidateOptions<InfiniteSkyOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, InfiniteSkyOptions options)
    {
        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.Application.Name))
        {
            failures.Add("InfiniteSky:Application:Name is required.");
        }

        if (options.Application.ShutdownTimeoutSeconds is < 1 or > 300)
        {
            failures.Add("InfiniteSky:Application:ShutdownTimeoutSeconds must be between 1 and 300.");
        }

        if (string.IsNullOrWhiteSpace(options.Diagnostics.ServiceName))
        {
            failures.Add("InfiniteSky:Diagnostics:ServiceName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Networking.BindAddress))
        {
            failures.Add("InfiniteSky:Networking:BindAddress is required.");
        }
        else if (!IPAddress.TryParse(options.Networking.BindAddress, out _))
        {
            failures.Add("InfiniteSky:Networking:BindAddress must be a valid IP address.");
        }

        if (options.Networking.LoginPort is < 1 or > 65_535)
        {
            failures.Add("InfiniteSky:Networking:LoginPort must be a valid TCP port.");
        }

        if (options.Networking.WorldPort is < 1 or > 65_535)
        {
            failures.Add("InfiniteSky:Networking:WorldPort must be a valid TCP port.");
        }

        if (options.Networking.MaxConcurrentConnections is < 1 or > 100_000)
        {
            failures.Add("InfiniteSky:Networking:MaxConcurrentConnections must be between 1 and 100000.");
        }

        if (options.Networking.MaxReceiveBufferBytes is < 9 or > 16_777_216)
        {
            failures.Add("InfiniteSky:Networking:MaxReceiveBufferBytes must be between 9 and 16777216.");
        }

        if (options.Networking.MaxFrameBytes is < 9 or > 16_777_216)
        {
            failures.Add("InfiniteSky:Networking:MaxFrameBytes must be between 9 and 16777216.");
        }

        if (options.Networking.MaxFrameBytes > options.Networking.MaxReceiveBufferBytes)
        {
            failures.Add("InfiniteSky:Networking:MaxFrameBytes must not exceed MaxReceiveBufferBytes.");
        }

        if (options.Networking.SendQueueCapacity is < 1 or > 10_000)
        {
            failures.Add("InfiniteSky:Networking:SendQueueCapacity must be between 1 and 10000.");
        }

        if (options.Networking.IdleTimeoutSeconds < 0 || options.Networking.IdleTimeoutSeconds > 86_400)
        {
            failures.Add("InfiniteSky:Networking:IdleTimeoutSeconds must be between 0 and 86400.");
        }

        if (options.Networking.ConnectionShutdownTimeoutSeconds is < 1 or > 300)
        {
            failures.Add("InfiniteSky:Networking:ConnectionShutdownTimeoutSeconds must be between 1 and 300.");
        }

        if (options.Networking.LoginPort == options.Networking.WorldPort)
        {
            failures.Add("InfiniteSky:Networking:LoginPort and WorldPort must be different.");
        }

        if (string.IsNullOrWhiteSpace(options.Database.Provider))
        {
            failures.Add("InfiniteSky:Database:Provider is required.");
        }

        if (options.Database.Enabled)
        {
            failures.Add("Database access cannot be enabled in the foundation stage; no persistence adapter exists yet.");
        }

        if (options.World.TickMilliseconds is < 1 or > 10_000)
        {
            failures.Add("InfiniteSky:World:TickMilliseconds must be between 1 and 10000.");
        }

        if (options.Zones.Load.Any(zoneId => zoneId <= 0))
        {
            failures.Add("InfiniteSky:Zones:Load may contain only positive zone IDs.");
        }

        if (options.Zones.Load.Distinct().Count() != options.Zones.Load.Length)
        {
            failures.Add("InfiniteSky:Zones:Load may not contain duplicate zone IDs.");
        }

        if (!string.Equals(options.Gameplay.CompatibilityProfile, "LegacyParity", StringComparison.Ordinal)
            && !string.Equals(options.Gameplay.CompatibilityProfile, "Corrected", StringComparison.Ordinal))
        {
            failures.Add("InfiniteSky:Gameplay:CompatibilityProfile must be LegacyParity or Corrected.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
