namespace InfiniteSky.Persistence.Tests;

public sealed class MongoIntegrationFactAttribute : FactAttribute
{
    public MongoIntegrationFactAttribute()
    {
        if (!string.Equals(
            Environment.GetEnvironmentVariable("INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set INFINITESKY_RUN_MONGODB_INTEGRATION_TESTS=true with Docker available to run this isolated test.";
        }
    }
}
