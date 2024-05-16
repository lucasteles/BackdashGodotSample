using SpaceWar.Models;

namespace SpaceWar.Logic;

using System.Text;
using Backdash;
using Backdash.Data;

public enum PlayerConnectState
{
    Connecting,
    Synchronizing,
    Running,
    Disconnected,
    Disconnecting,
}

public sealed class PlayerConnectionInfo
{
    public string Name;
    public PlayerHandle Handle;
    public PlayerConnectState State;
    public int ConnectProgress;
    public DateTime DisconnectStart;
    public TimeSpan DisconnectTimeout;
    public PeerNetworkStats PeerNetworkStats = new();
}

public sealed class NonGameState
{
    public PlayerConnectionInfo[] Players { get; private set; }
    public PlayerHandle? LocalPlayerHandle { get; private set; }
    public int NumberOfPlayers { get; private set; }
    public ShipNode[] ShipNodes { get; private set; }
    public Label[] NameLabels { get; private set; }

    public readonly StringBuilder StatusText = new();

    public FrameSpan RollbackFrames;
    public double SleepTime;
    public bool Sleeping => SleepTime > 0f;

    public void Init(
        Ship[] ships,
        IReadOnlyCollection<PlayerHandle> players,
        PackedScene shipPrefab,
        Node rootNode
    )
    {
        ArgumentNullException.ThrowIfNull(ships);
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(shipPrefab);
        if (GlobalConfig.Instance.LobbyInfo is not { Players: { } peersInfo })
            throw new InvalidOperationException("Invalid players info");

        NumberOfPlayers = ships.Length;
        InitPlayerInfo(players, peersInfo);
        InitNameLabels(rootNode);
        InitShipNodes(ships, shipPrefab, rootNode);
    }

    void InitShipNodes(Ship[] ships, PackedScene shipPrefab, Node rootNode)
    {
        ShipNodes = new ShipNode[NumberOfPlayers];
        for (var i = 0; i < ShipNodes.Length; i++)
        {
            var shipNode = shipPrefab.Instantiate<ShipNode>();
            shipNode.Initialize(ships[i], Players[i]);
            rootNode.AddChild(shipNode);
            ShipNodes[i] = shipNode;
        }
    }

    void InitNameLabels(Node rootNode)
    {
        NameLabels = new Label[GameConstants.MaxShips];
        for (var i = 0; i < NameLabels.Length; i++)
        {
            NameLabels[i] = rootNode.GetNode<Label>($"%lblPlayer{i + 1}");
            NameLabels[i].Visible = i < NumberOfPlayers;
        }
    }

    void InitPlayerInfo(IReadOnlyCollection<PlayerHandle> players, Peer[] peersInfo)
    {
        Players = new PlayerConnectionInfo[NumberOfPlayers];
        foreach (var player in players)
        {
            PlayerConnectionInfo playerInfo = new();
            Players[player.Index] = playerInfo;
            playerInfo.Handle = player;
            playerInfo.Name = peersInfo[player.Index].Username;
            if (player.IsLocal())
            {
                playerInfo.ConnectProgress = 100;
                LocalPlayerHandle = player;
                SetConnectState(player, PlayerConnectState.Connecting);
            }

            StatusText.Clear();
            StatusText.Append("Connecting to peers ...");
        }
    }

    public void Update(GameState gameState)
    {
        for (var i = 0; i < gameState.Ships.Length; i++)
        {
            ref var ship = ref gameState.Ships[i];
            ShipNodes[i].UpdateShip(gameState.Ships[i]);
            NameLabels[i].Text = $"{Players[i].Name}: {ship.Score}";
        }
    }

    public bool TryGetPlayer(PlayerHandle handle, out PlayerConnectionInfo state)
    {
        for (var i = 0; i < Players.Length; i++)
        {
            if (Players[i].Handle != handle) continue;
            state = Players[i];
            return true;
        }

        state = null!;
        return false;
    }

    public void SetDisconnectTimeout(PlayerHandle handle, DateTime when, TimeSpan timeout)
    {
        if (!TryGetPlayer(handle, out var player)) return;
        player.DisconnectStart = when;
        player.DisconnectTimeout = timeout;
        player.State = PlayerConnectState.Disconnecting;
    }

    public void SetConnectState(in PlayerHandle handle, PlayerConnectState state)
    {
        if (!TryGetPlayer(handle, out var player)) return;
        player.ConnectProgress = 0;
        player.State = state;
    }

    public void SetConnectState(PlayerConnectState state)
    {
        foreach (var player in Players)
            player.State = state;
    }

    public void UpdateConnectProgress(PlayerHandle handle, int progress)
    {
        if (!TryGetPlayer(handle, out var player)) return;
        player.ConnectProgress = progress;
    }
}
