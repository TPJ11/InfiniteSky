using InfiniteSky.Application.Authentication;
using InfiniteSky.Application.Characters;
using InfiniteSky.Application.Players;
using InfiniteSky.Domain.Characters;
using InfiniteSky.Domain.Identifiers;
using InfiniteSky.Domain.Items;
using InfiniteSky.Domain.Values;
using InfiniteSky.Domain.Zones;
using InfiniteSky.GameData.Zones;
using InfiniteSky.Protocol.Structures;
using InfiniteSky.World.Players;
using InfiniteSky.World.Zones;

namespace InfiniteSky.Server.IntegrationTests;

public sealed class PlayerActivationServiceTests
{
    [Fact]
    public async Task ActivationRequiresOwnedCharacterAndDisconnectSavesCurrentPosition()
    {
        using var zoneMetrics = new ZoneMetrics();
        using var playerMetrics = new PlayerMetrics();
        using var authenticationMetrics = new AuthenticationMetrics();
        var sessions = new InMemoryAuthenticationSessionStore(authenticationMetrics);
        Guid loginConnectionId = Guid.NewGuid();
        Assert.Equal(
            SessionRegistrationStatus.Created,
            sessions.TryCreate(
                new AccountId(17),
                "FixtureAccount",
                loginConnectionId,
                DateTimeOffset.UtcNow,
                out AuthenticatedSession? session));
        Character character = CreateCharacter(107, 17);
        var repository = new MemoryCharacterRepository(character);
        var zoneManager = await StartZoneManager(zoneMetrics);
        var manager = new PlayerManager(playerMetrics);
        var service = new PlayerActivationService(
            sessions,
            repository,
            manager,
            zoneManager,
            playerMetrics,
            "Development");
        Guid worldConnectionId = Guid.NewGuid();

        IReadOnlyList<Character> listed = await service.GetCharactersAsync(
            loginConnectionId,
            CancellationToken.None);
        Assert.Same(character, Assert.Single(listed));
        Assert.Equal(new AccountId(17), repository.LastRequestedAccount);
        Assert.Equal("Development", repository.LastRequestedServer);

        PlayerRuntime player = await service.ActivateAsync(
            loginConnectionId,
            worldConnectionId,
            new CharacterId(107),
            CancellationToken.None);
        Assert.Equal(session!.SessionId, player.AuthenticationSessionId);
        Assert.True(manager.TryFindByAuthenticationSession(session.SessionId, out PlayerRuntime? bound));
        Assert.Same(player, bound);

        Assert.True(manager.TryUpdatePosition(player, new Position3(3.9, 4.2, -5.8)));
        PlayerRuntimeSnapshot? disconnected = await service.DisconnectAsync(
            worldConnectionId,
            CancellationToken.None);

        Assert.NotNull(disconnected);
        Assert.Equal(new Position3(3.9, 4.2, -5.8), disconnected.Position);
        Assert.Equal(0, manager.Count);
        Assert.Equal(new CharacterId(107), repository.LastSavedCharacterId);
        Assert.Equal(new AccountId(17), repository.LastSavedAccountId);
        Assert.Equal(new ZoneId(1), repository.LastSavedZoneId);
        Assert.Equal(new Position3(3.9, 4.2, -5.8), repository.LastSavedPosition);
        Assert.Null(await service.DisconnectAsync(worldConnectionId, CancellationToken.None));

        repository.CharacterToReturn = CreateCharacter(108, 17);
        _ = await service.ActivateAsync(
            loginConnectionId,
            Guid.NewGuid(),
            new CharacterId(108),
            CancellationToken.None);
        await service.ShutdownAsync(CancellationToken.None);
        Assert.Equal(0, manager.Count);
        Assert.Equal(new CharacterId(108), repository.LastSavedCharacterId);
        Assert.Equal(0, zoneManager.GetZones().Single().SpatialObjectCount);

        await zoneManager.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ActivationRejectsForeignCharactersAndSessionsExpiredDuringLoad()
    {
        using var zoneMetrics = new ZoneMetrics();
        using var playerMetrics = new PlayerMetrics();
        using var authenticationMetrics = new AuthenticationMetrics();
        var sessions = new InMemoryAuthenticationSessionStore(authenticationMetrics);
        Guid loginConnectionId = Guid.NewGuid();
        _ = sessions.TryCreate(
            new AccountId(17),
            "FixtureAccount",
            loginConnectionId,
            DateTimeOffset.UtcNow,
            out _);
        Character foreignCharacter = CreateCharacter(108, 18);
        var repository = new MemoryCharacterRepository(foreignCharacter);
        var zoneManager = await StartZoneManager(zoneMetrics);
        var manager = new PlayerManager(playerMetrics);
        var service = new PlayerActivationService(
            sessions,
            repository,
            manager,
            zoneManager,
            playerMetrics,
            "Development");

        PlayerActivationException foreign = await Assert.ThrowsAsync<PlayerActivationException>(
            async () => await service.ActivateAsync(
                loginConnectionId,
                Guid.NewGuid(),
                new CharacterId(108),
                CancellationToken.None));
        Assert.Equal(PlayerActivationFailure.CharacterUnavailable, foreign.Failure);
        Assert.Equal(0, manager.Count);

        repository.BeforeOwnedRead = () => sessions.RemoveByConnection(loginConnectionId, out _);
        repository.CharacterToReturn = CreateCharacter(109, 17);
        PlayerActivationException expired = await Assert.ThrowsAsync<PlayerActivationException>(
            async () => await service.ActivateAsync(
                loginConnectionId,
                Guid.NewGuid(),
                new CharacterId(109),
                CancellationToken.None));
        Assert.Equal(PlayerActivationFailure.AuthenticationExpired, expired.Failure);
        Assert.Equal(0, manager.Count);

        await zoneManager.StopAsync(CancellationToken.None);
    }

    private static async Task<ZoneManager> StartZoneManager(ZoneMetrics metrics)
    {
        var manager = new ZoneManager(new ZoneRegistry(), new EmptyZoneResourceLoader(), metrics);
        await manager.StartAsync(
            [
                new ZoneDescriptor
                {
                    Id = new ZoneId(1),
                    Name = "Z001",
                    Load = true,
                },
            ],
            new ZoneManagerOptions(),
            CancellationToken.None);
        return manager;
    }

    private static Character CreateCharacter(long characterId, long accountId)
        => new(new CharacterId(characterId), new AccountId(accountId), $"Character{characterId}")
        {
            ServerName = "Development",
            SavedLocation = new SavedCharacterLocation
            {
                ZoneId = new ZoneId(1),
                X = -1.9,
                Y = 4.2,
                Z = -5.8,
            },
            Equipment =
            {
                Armor = new EquipmentItem { ItemId = new ItemDefinitionId(2) },
            },
        };

    private sealed class MemoryCharacterRepository(Character character) : ICharacterRepository
    {
        public Character CharacterToReturn { get; set; } = character;
        public Action? BeforeOwnedRead { get; set; }
        public AccountId? LastRequestedAccount { get; private set; }
        public string? LastRequestedServer { get; private set; }
        public CharacterId? LastSavedCharacterId { get; private set; }
        public AccountId? LastSavedAccountId { get; private set; }
        public ZoneId? LastSavedZoneId { get; private set; }
        public Position3? LastSavedPosition { get; private set; }

        public ValueTask<IReadOnlyList<Character>> GetForAccountAsync(
            AccountId accountId,
            string serverName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastRequestedAccount = accountId;
            LastRequestedServer = serverName;
            return ValueTask.FromResult<IReadOnlyList<Character>>([CharacterToReturn]);
        }

        public ValueTask<Character?> GetOwnedAsync(
            CharacterId characterId,
            AccountId accountId,
            string serverName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastRequestedAccount = accountId;
            LastRequestedServer = serverName;
            BeforeOwnedRead?.Invoke();
            return ValueTask.FromResult<Character?>(
                CharacterToReturn.Id == characterId ? CharacterToReturn : null);
        }

        public ValueTask SaveWorldLocationAsync(
            CharacterId characterId,
            AccountId accountId,
            string serverName,
            ZoneId zoneId,
            Position3 position,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastSavedCharacterId = characterId;
            LastSavedAccountId = accountId;
            LastSavedZoneId = zoneId;
            LastSavedPosition = position;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class EmptyZoneResourceLoader : IZoneResourceLoader
    {
        public Task<ZoneResourceData> LoadAsync(ZoneId zoneId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ZoneResourceData(
                Array.Empty<SpawnDefinition>(),
                Array.Empty<LegacyWorldRegion>(),
                Array.Empty<LegacyWorldRegion>(),
                false,
                false,
                false));
        }
    }
}
