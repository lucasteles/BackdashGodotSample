using Backdash;
using Backdash.Core;
using SpaceWar.Logic;
using SpaceWar.Models;
using SpaceWar.Services;

public partial class BattleScene : Node2D
{
    GlobalConfig config;
    INetcodeSession<GameInputs> rollbackSession;
    OnlineMatchSession gameSession;

    GameState gs;
    NonGameState ngs;

    Label lblPing;
    Label lblRollbackFrames;
    Label lblStatusMessage;
    PanelContainer messageBox;

    static readonly NetcodeOptions options = new()
    {
        FrameDelay = 2,
        Log = new()
        {
            EnabledLevel = LogLevel.Warning,
        },
        Protocol = new()
        {
            NumberOfSyncRoundtrips = 10,
            DisconnectTimeout = TimeSpan.FromSeconds(3),
            DisconnectNotifyStart = TimeSpan.FromSeconds(1),
            LogNetworkStats = false,
        },
    };

    public override void _Ready()
    {
        config = GlobalConfig.Instance;
        lblPing = GetNode<Label>("%lblPingValue");
        lblRollbackFrames = GetNode<Label>("%lblRollbackValue");
        lblStatusMessage = GetNode<Label>("%lblStatusMessage");
        messageBox = GetNode<PanelContainer>("%MessageBox");

        lblStatusMessage.Text = "";
        messageBox.Visible = false;

        CreateRollbackSession();
        InitializeSession();
    }

    void InitializeSession()
    {
        var numPlayers = rollbackSession.NumberOfPlayers;

        gs = new();
        ngs = new();


        gs.Init(numPlayers, GetViewportRect());
        ngs.Init(
            gs.Ships,
            rollbackSession.GetPlayers(),
            this
        );

        gameSession = new(gs, ngs, rollbackSession);
        rollbackSession.SetHandler(gameSession);
        rollbackSession.Start();
    }

    void CreateRollbackSession()
    {
        SessionServices<GameInputs> services = new()
        {
            LogWriter = new GDBackdashLogger(),
        };

        switch (config.Mode)
        {
            case PlayerMode.Player:
                var localPlayer = config.MatchPlayers.FirstOrDefault(x => x.IsLocal());
                if (localPlayer is null)
                    throw new InvalidOperationException("No local player defined");

                rollbackSession = RollbackNetcode.CreateSession(config.LocalPort, options, services);
                rollbackSession.AddPlayers(config.MatchPlayers);
                break;
            case PlayerMode.Spectator:
                rollbackSession = RollbackNetcode.CreateSpectatorSession(
                    config.LocalPort, config.SpectateHost,
                    config.LobbyInfo.Players.Length,
                    options, services
                );
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void _Process(double delta)
    {
        if (gameSession is null)
            return;

        gameSession.Update(delta);
        UpdateUi();
    }

    void UpdateUi()
    {
        var maxPing = ngs.Players
            .Where(static p => p.Handle.IsRemote())
            .Max(static player => player.PeerNetworkStats.Ping);

        lblRollbackFrames.Text = ngs.RollbackFrames.FrameCount.ToString();
        lblPing.Text = $"{maxPing.TotalMilliseconds:f2}";

        if (!messageBox.Visible && ngs.StatusText.Length > 0)
        {
            messageBox.Visible = true;
            lblStatusMessage.Text = ngs.StatusText.ToString();
        }
        else if (messageBox.Visible && ngs.StatusText.Length is 0)
        {
            messageBox.Visible = false;
            lblStatusMessage.Text = "";
        }
    }

    public override void _Input(InputEvent input)
    {
        if (input.IsActionPressed(ActionNames.Cancel))
            GetTree().Quit();
    }

    protected override void Dispose(bool disposing)
    {
        rollbackSession.Dispose();
        base.Dispose(disposing);
    }
}
