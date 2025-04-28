using SpaceWar.Models;
using SpaceWar.Services;

namespace SpaceWar.Logic;

using System.Text;
using Backdash;

public enum PlayerConnectState
{
    Connecting,
    Synchronizing,
    Running,
    Disconnected,
    Disconnecting,
}

public sealed class PlayerInfo
{
    public string Name;
    public NetcodePlayer Handle;
    public PlayerConnectState State;
    public int ConnectProgress;
    public DateTime DisconnectStart;
    public TimeSpan DisconnectTimeout;
}

public sealed class NonGameState
{
    public PlayerInfo[] Players { get; private set; }
    public NetcodePlayer LocalPlayerHandle { get; private set; }
    public int NumberOfPlayers { get; private set; }
    public ShipNode[] ShipNodes { get; private set; }
    public Label[] NameLabels { get; private set; }

    public readonly StringBuilder StatusText = new();

    public FrameSpan RollbackFrames;
    public double SleepTime;
    public bool Sleeping => SleepTime > 0f;

    public void Init(
        Ship[] ships,
        IReadOnlyCollection<NetcodePlayer> players,
        Node rootNode
    )
    {
        ArgumentNullException.ThrowIfNull(ships);
        ArgumentNullException.ThrowIfNull(players);
        if (GlobalConfig.Instance.LobbyInfo is not { Players: { } peersInfo })
            throw new InvalidOperationException("Invalid players info");

        NumberOfPlayers = ships.Length;
        InitPlayerInfo(players, peersInfo);
        InitNameLabels(rootNode);
        InitShipNodes(ships, rootNode);
    }

    void InitShipNodes(Ship[] ships, Node rootNode)
    {
        ShipNodes = new ShipNode[NumberOfPlayers];
        for (var i = 0; i < ShipNodes.Length; i++)
        {
            var shipNode = Prefab.Instantiate<ShipNode>("ship");
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

    void InitPlayerInfo(IReadOnlyCollection<NetcodePlayer> players, Peer[] peersInfo)
    {
        Players = new PlayerInfo[NumberOfPlayers];
        foreach (var player in players)
        {
            PlayerInfo playerInfo = new();
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

    public bool TryGetPlayer(NetcodePlayer handle, out PlayerInfo state)
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

    public void SetDisconnectTimeout(NetcodePlayer handle, DateTime when, TimeSpan timeout)
    {
        if (!TryGetPlayer(handle, out var player)) return;
        player.DisconnectStart = when;
        player.DisconnectTimeout = timeout;
        player.State = PlayerConnectState.Disconnecting;
    }

    public void SetConnectState(NetcodePlayer handle, PlayerConnectState state)
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

    public void UpdateConnectProgress(NetcodePlayer handle, int progress)
    {
        if (!TryGetPlayer(handle, out var player)) return;
        player.ConnectProgress = progress;
    }
}
