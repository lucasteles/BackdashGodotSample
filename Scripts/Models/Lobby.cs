namespace SpaceWar.Models;

public sealed class Lobby
{
    public required string Name { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public bool Ready { get; init; }
    public required Peer[] Players { get; init; }
    public required Peer[] Spectators { get; init; }
    public required SpectatorMapping[] SpectatorMapping { get; init; }

    public Peer[] GetPeers(PlayerMode mode) =>
        mode switch
        {
            PlayerMode.Player => Players,
            PlayerMode.Spectator => Spectators,
            _ => throw new ArgumentOutOfRangeException(),
        };
}

public sealed record SpectatorMapping(Guid Host, Guid[] Watchers);
