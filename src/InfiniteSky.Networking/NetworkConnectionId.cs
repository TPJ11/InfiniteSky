namespace InfiniteSky.Networking;

public readonly record struct NetworkConnectionId(Guid Value)
{
    public static NetworkConnectionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
