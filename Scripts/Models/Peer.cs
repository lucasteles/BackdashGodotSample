namespace SpaceWar.Models;

using System.Net;

public sealed class Peer
{
    public required Guid PeerId { get; init; }
    public required string Username { get; init; }
    public required IPEndPoint Endpoint { get; init; }
    public IPEndPoint LocalEndpoint { get; init; }
    public bool Connected { get; init; }
    public bool Ready { get; init; }
}
