using Backdash;
using Backdash.Data;
using Backdash.Serialization.Buffer;

namespace SpaceWar.Logic;

public sealed class OnlineMatchSession(
    GameState gameState,
    NonGameState nonGameState,
    INetcodeSession<GameInputs> session
) : INetcodeSessionHandler
{
    readonly SynchronizedInput<GameInputs>[] inputs =
        new SynchronizedInput<GameInputs>[nonGameState.NumberOfPlayers];

    public void Update(double delta)
    {
        if (nonGameState.Sleeping)
        {
            nonGameState.SleepTime -= delta;
            return;
        }

        UpdateStats();
        session.BeginFrame();
        if (nonGameState.LocalPlayerHandle is { } localPlayer)
        {
            HandleNoGameInput();
            var localInput = GameInput.ReadInputs();
            if (session.AddLocalInput(localPlayer, localInput) is not ResultCode.Ok)
                return;
        }

        var syncInputResult = session.SynchronizeInputs();
        if (syncInputResult is not ResultCode.Ok)
        {
            // Log.Debug($"{DateTime.Now:o} => ERROR SYNC INPUTS: {syncInputResult}");
            return;
        }

        session.GetInputs(inputs);
        gameState.Update(inputs);
        nonGameState.Update(gameState);
        session.AdvanceFrame();
    }

    void HandleNoGameInput()
    {
        if (Input.IsKeyPressed(Key.Key1)) DisconnectPlayer(0);
        if (Input.IsKeyPressed(Key.Key2)) DisconnectPlayer(1);
        if (Input.IsKeyPressed(Key.Key3)) DisconnectPlayer(2);
        if (Input.IsKeyPressed(Key.Key4)) DisconnectPlayer(3);
    }

    void DisconnectPlayer(int number)
    {
        if (nonGameState.NumberOfPlayers <= number) return;
        var handle = nonGameState.Players[number].Handle;
        session.DisconnectPlayer(in handle);
        nonGameState.StatusText.Clear();
        nonGameState.StatusText.Append("Disconnected player ");
        nonGameState.StatusText.Append(handle.Number);
    }


    public void OnSessionStart()
    {
        Log.Debug($"{DateTime.Now:o} => GAME STARTED");
        nonGameState.SetConnectState(PlayerConnectState.Running);
        nonGameState.StatusText.Clear();
    }

    public void OnSessionClose()
    {
        Log.Debug($"{DateTime.Now:o} => GAME CLOSED");
        nonGameState.SetConnectState(PlayerConnectState.Disconnected);
    }

    public void TimeSync(FrameSpan framesAhead)
    {
        Log.Debug($"{DateTime.Now:o} => Time sync requested {framesAhead.FrameCount}");
        nonGameState.SleepTime = framesAhead.Duration().TotalSeconds;
    }

    void UpdateStats()
    {
        nonGameState.RollbackFrames = session.RollbackFrames;
        for (var i = 0; i < nonGameState.Players.Length; i++)
        {
            ref var player = ref nonGameState.Players[i];
            if (!player.Handle.IsRemote())
                continue;
            session.GetNetworkStatus(player.Handle, ref player.PeerNetworkStats);
        }
    }

    public void OnPeerEvent(PlayerHandle player, PeerEventInfo evt)
    {
        Log.Debug($"{DateTime.Now:o} => PEER EVENT: {evt} from {player}");

        if (player.IsSpectator())
            return;

        switch (evt.Type)
        {
            case PeerEvent.Connected:
                nonGameState.SetConnectState(player, PlayerConnectState.Synchronizing);
                break;
            case PeerEvent.Synchronizing:
                var progress = 100 * evt.Synchronizing.CurrentStep / (float)evt.Synchronizing.TotalSteps;
                nonGameState.UpdateConnectProgress(player, (int)progress);
                break;
            case PeerEvent.SynchronizationFailure:
                nonGameState.SetConnectState(player, PlayerConnectState.Disconnected);
                break;
            case PeerEvent.Synchronized:
                nonGameState.UpdateConnectProgress(player, 100);
                break;
            case PeerEvent.ConnectionInterrupted:
                nonGameState.SetDisconnectTimeout(
                    player, DateTime.UtcNow,
                    evt.ConnectionInterrupted.DisconnectTimeout
                );
                break;
            case PeerEvent.ConnectionResumed:
                nonGameState.SetConnectState(player, PlayerConnectState.Running);
                break;
            case PeerEvent.Disconnected:
                nonGameState.SetConnectState(player, PlayerConnectState.Disconnected);
                break;
        }
    }

    public void SaveState(in Frame frame, ref readonly BinaryBufferWriter writer) =>
        gameState.SaveState(in writer);

    public void LoadState(in Frame frame, ref readonly BinaryBufferReader reader)
    {
        Console.WriteLine($"{DateTime.Now:o} => Loading state {frame}...");
        gameState.LoadState(in reader);
    }

    public void AdvanceFrame()
    {
        session.SynchronizeInputs();
        session.GetInputs(inputs);
        gameState.Update(inputs);
        session.AdvanceFrame();
    }
}
