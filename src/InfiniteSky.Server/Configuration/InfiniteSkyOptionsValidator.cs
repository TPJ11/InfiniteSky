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

        if (options.Hosting.StartServicesConcurrently)
        {
            failures.Add(
                "InfiniteSky:Hosting:StartServicesConcurrently must be false because hosted services have startup dependencies.");
        }

        if (options.Hosting.StopServicesConcurrently)
        {
            failures.Add(
                "InfiniteSky:Hosting:StopServicesConcurrently must be false because hosted services have shutdown dependencies.");
        }

        if (string.IsNullOrWhiteSpace(options.Diagnostics.ServiceName))
        {
            failures.Add("InfiniteSky:Diagnostics:ServiceName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Characters.ServerName))
        {
            failures.Add("InfiniteSky:Characters:ServerName is required.");
        }

        if (options.Authentication.TimeoutSeconds is < 1 or > 300)
        {
            failures.Add("InfiniteSky:Authentication:TimeoutSeconds must be between 1 and 300.");
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
            if (!string.Equals(options.Database.Provider, "MongoDB", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("InfiniteSky:Database:Provider must be MongoDB when database persistence is enabled.");
            }

            if (string.IsNullOrWhiteSpace(options.Database.ConnectionString)
                || !(options.Database.ConnectionString.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase)
                    || options.Database.ConnectionString.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase)))
            {
                failures.Add("InfiniteSky:Database:ConnectionString must be a MongoDB URI when database persistence is enabled.");
            }

            if (string.IsNullOrWhiteSpace(options.Database.DatabaseName))
            {
                failures.Add("InfiniteSky:Database:DatabaseName is required when database persistence is enabled.");
            }

            if (string.IsNullOrWhiteSpace(options.Database.AccountCollectionName)
                || options.Database.AccountCollectionName.Contains('\0'))
            {
                failures.Add("InfiniteSky:Database:AccountCollectionName must be a valid non-empty collection name.");
            }

            if (string.IsNullOrWhiteSpace(options.Database.CharacterCollectionName)
                || options.Database.CharacterCollectionName.Contains('\0'))
            {
                failures.Add("InfiniteSky:Database:CharacterCollectionName must be a valid non-empty collection name.");
            }

            if (string.IsNullOrWhiteSpace(options.Database.GuildCollectionName)
                || options.Database.GuildCollectionName.Contains('\0'))
            {
                failures.Add("InfiniteSky:Database:GuildCollectionName must be a valid non-empty collection name.");
            }

            if (options.Database.ServerSelectionTimeoutSeconds is < 1 or > 120)
            {
                failures.Add("InfiniteSky:Database:ServerSelectionTimeoutSeconds must be between 1 and 120.");
            }

            if (options.Database.ConnectTimeoutSeconds is < 1 or > 120)
            {
                failures.Add("InfiniteSky:Database:ConnectTimeoutSeconds must be between 1 and 120.");
            }

            if (options.Database.OperationTimeoutSeconds is < 1 or > 300)
            {
                failures.Add("InfiniteSky:Database:OperationTimeoutSeconds must be between 1 and 300.");
            }

            if (options.Database.HealthCheckIntervalSeconds is < 1 or > 3600)
            {
                failures.Add("InfiniteSky:Database:HealthCheckIntervalSeconds must be between 1 and 3600.");
            }

            if (options.Database.HealthCheckTimeoutSeconds is < 1 or > 120)
            {
                failures.Add("InfiniteSky:Database:HealthCheckTimeoutSeconds must be between 1 and 120.");
            }

            if (options.Database.MinConnectionPoolSize < 0
                || options.Database.MaxConnectionPoolSize is < 1 or > 10_000
                || options.Database.MinConnectionPoolSize > options.Database.MaxConnectionPoolSize)
            {
                failures.Add("InfiniteSky:Database connection-pool limits must satisfy 0 <= Min <= Max <= 10000.");
            }
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

        if (options.Zones.AsyncLoadLimit is < 1 or > 128)
        {
            failures.Add("InfiniteSky:Zones:AsyncLoadLimit must be between 1 and 128.");
        }

        if (options.Zones.QuadTreeDepth is < 1 or > 32)
        {
            failures.Add("InfiniteSky:Zones:QuadTreeDepth must be between 1 and 32.");
        }

        if (options.Zones.QuadTreeLimit < 1)
        {
            failures.Add("InfiniteSky:Zones:QuadTreeLimit must be positive.");
        }

        if (!double.IsFinite(options.Zones.ViewableActionDistance)
            || options.Zones.ViewableActionDistance < 0
            || options.Zones.ViewableActionDistance > int.MaxValue)
        {
            failures.Add(
                "InfiniteSky:Zones:ViewableActionDistance must be finite and between 0 and Int32.MaxValue.");
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
