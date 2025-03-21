using System.Net;
using Backdash;
using SpaceWar.Models;

public class GlobalConfig
{
    static readonly Lazy<GlobalConfig> instance = new(() => new());
    public static GlobalConfig Instance => instance.Value;

    public string Username { get; set; }
    public string LobbyName { get; set; }
    public Uri ServerUrl { get; set; }
    public int LocalPort { get; set; }
    public int ServerUdpPort { get; set; }
    public PlayerMode Mode { get; set; }
    public Lobby LobbyInfo { get; set; }
    public IPEndPoint SpectateHost { get; set; }
    public IReadOnlyList<NetcodePlayer> MatchPlayers { get; set; }

    public GlobalConfig()
    {
        ConfigFile config = new();
        config.Load("res://settings.ini");

        var section = config.GetSections().Single();

        LobbyName = (string)config.GetValue(section, nameof(LobbyName));
        LobbyName = LobbyName.NormalizeText();

        LoadLocalPort(config, section);
        LoadUsername(config, section);
        LoadServerAddress(config, section);
    }

    void LoadLocalPort(ConfigFile config, string section)
    {
        LocalPort = (int)config.GetValue(section, nameof(LocalPort));

        if (LocalPort is 0)
            LocalPort = Random.Shared.Next(9_000, 10_000);
    }

    void LoadServerAddress(ConfigFile config, string section)
    {
        ServerUdpPort = (int)config.GetValue(section, nameof(ServerUdpPort));
        if (Uri.TryCreate(
                (string)config.GetValue(section, nameof(ServerUrl)),
                UriKind.Absolute, out var url))
            ServerUrl = url;
        else
            throw new InvalidOperationException("Invalid Server URL");
    }

    void LoadUsername(ConfigFile config, string section)
    {
        Username = (string)config.GetValue(section, nameof(Username));

        if (string.IsNullOrWhiteSpace(Username))
            Username = System.Environment.UserName;

        Username = Username.NormalizeText();
    }
}
