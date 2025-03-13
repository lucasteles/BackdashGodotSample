using Backdash;
using Backdash.Core;
using SpaceWar.Logic;
using SpaceWar.Models;
using SpaceWar.Services;

public partial class BattleScene : Node2D
{
    GlobalConfig config;
    INetcodeSession<GameInputs> netcodeSession;
    OnlineMatchSession gameSession;

    GameState gs;
    NonGameState ngs;

    Label lblPing;
    Label lblRollbackFrames;
    Label lblStatusMessage;
    PanelContainer messageBox;

    public override void _Ready()
    {
        config = GlobalConfig.Instance;
        lblPing = GetNode<Label>("%lblPingValue");
        lblRollbackFrames = GetNode<Label>("%lblRollbackValue");
        lblStatusMessage = GetNode<Label>("%lblStatusMessage");
        messageBox = GetNode<PanelContainer>("%MessageBox");

        lblStatusMessage.Text = "";
        messageBox.Visible = false;

        netcodeSession = CreateRollbackSession();

        InitializeSession();
    }

    void InitializeSession()
    {
        gs = new();
        ngs = new();

        gs.Init(
            netcodeSession.NumberOfPlayers,
            (Rect2I)GetViewportRect()
        );

        ngs.Init(
            gs.Ships,
            netcodeSession.GetPlayers(),
            this
        );

        gameSession = new(gs, ngs, netcodeSession);
        netcodeSession.SetHandler(gameSession);
        netcodeSession.Start();
    }

    INetcodeSession<GameInputs> CreateRollbackSession()
    {
        var builder =
            RollbackNetcode
                .WithInputType<GameInputs>()
                .WithPort(config.LocalPort)
                .WithInputDelayFrames(2)
                .WithLogLevel(LogLevel.Warning)
                .WithLogWriter<GDBackdashLogger>();

        switch (config.Mode)
        {
            case PlayerMode.Player:
                if (!config.MatchPlayers.Any(x => x.IsLocal()))
                    throw new InvalidOperationException("No local player defined");

                return builder
                    .WithPlayers(config.MatchPlayers)
                    .ForRemote()
                    .Build();

            case PlayerMode.Spectator:
                return builder
                    .WithPlayerCount(config.LobbyInfo.Players.Length)
                    .ForSpectator(options =>
                    {
                        options.HostEndPoint = config.SpectateHost;
                    })
                    .Build();

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
        netcodeSession.Dispose();
        base.Dispose(disposing);
    }
}
