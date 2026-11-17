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

        if (options.Networking.LoginPort is < 1 or > 65_535)
        {
            failures.Add("InfiniteSky:Networking:LoginPort must be a valid TCP port.");
        }

        if (options.Networking.WorldPort is < 1 or > 65_535)
        {
            failures.Add("InfiniteSky:Networking:WorldPort must be a valid TCP port.");
        }

        if (options.Networking.Enabled)
        {
            failures.Add("Networking cannot be enabled in the foundation stage; no legacy protocol listener exists yet.");
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
