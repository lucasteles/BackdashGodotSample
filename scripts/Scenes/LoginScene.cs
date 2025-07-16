using SpaceWar.Models;

public partial class LoginScene : Node
{
    GlobalConfig config;
    LineEdit txtLobby;
    LineEdit txtUsername;
    LineEdit txtLocalPort;

    public override void _Ready()
    {
        config = GlobalConfig.Instance;
        txtLobby = GetNode<LineEdit>("%txtLobby");
        txtUsername = GetNode<LineEdit>("%txtUsername");
        txtLocalPort = GetNode<LineEdit>("%txtPort");

        txtLobby.Text = config.LobbyName;
        txtUsername.Text = config.Username;
        txtLocalPort.Text = config.LocalPort.ToString();
    }

    void _StartAsPlayer() => StartLobby(PlayerMode.Player);

    void _StartAsSpectator() => StartLobby(PlayerMode.Spectator);

    public override void _Input(InputEvent input)
    {
        if (input.IsActionPressed(ActionNames.Cancel))
            GetTree().Quit();
    }

    void StartLobby(PlayerMode mode)
    {
        config.Username = txtUsername.Text.ToAlphaNumeric();
        config.LobbyName = txtLobby.Text.ToAlphaNumeric();

        if (int.TryParse(txtLocalPort.Text.Trim(), out var newPort))
            config.LocalPort = newPort;

        config.Mode = mode;

        SceneManager.Instance.ChangeTo(SceneName.Lobby);
    }
}
