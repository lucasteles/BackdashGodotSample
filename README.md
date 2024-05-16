# SpaceWar with Godot

This project is a usage sample of [rollback netcode](https://lucasteles.github.io/Backdash/docs/introduction.html#how-does-it-work) using [Backdash](https://github.com/lucasteles/Backdash) in [Godot](https://godotengine.org/).

It shows a basic example of an online lobby for NAT traversal using [UDP hole punching](https://en.wikipedia.org/wiki/UDP_hole_punching).

# How does it work?

This enables a P2P connection over the internet, this is possible using
a [middle server](https://github.com/lucasteles/Backdash/tree/master/samples/LobbyServer)
which all clients know.
The server catches the IP address and port of a client and sends it to the others.

The current server runs almost as a simple HTTP with JSON responses. It keeps the lobby info with sliding expiration
cache.

When a client enters the lobby the server responds with a token of type `Guid`/`UUID`. It is used a very
basic `Authentication` mechanism.

The client uses HTTP pooling to get updated information on each lobby member/peer.

When logged in, every client needs to send a `UDP` package with their token to the server. The server uses the package headers metadata  
to keep track of their `IP` and open `Port`.

> ⚠️ UDP Hole punching usually **does not** work witch clients behind the same NAT. To mitigate this the server
> also tracks the local IP and port on each client to check if the peer is on the same network.

## Controls

- **Arrows**: Move
- **Space**: Fire
- **Left Ctrl**: Missile

## Running

### Server

> [!NOTE]
> This project uses an already published [demo lobby server](https://lobby-server.fly.dev/swagger/index.html).

The server URL is configured in the `settings.ini` file and you can start your own [server from here](https://github.com/lucasteles/Backdash/tree/master/samples/LobbyServer):

After cloning the repository run this command on the server project directory:
```bash
dotnet run .
```

- Default **HTTP**: `9999`
- Default **UDP** : `8888`

> [!TIP]
> 💡 Check the swagger `API` docs at http://localhost:9999/swagger
