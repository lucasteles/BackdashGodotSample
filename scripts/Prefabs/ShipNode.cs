using System.Diagnostics.CodeAnalysis;
using Godot.Collections;
using SpaceWar.Logic;
using SpaceWar.Models;
using SpaceWar.Services;
using static SpaceWar.Logic.GameConstants;

public partial class ShipNode : Node2D
{
    [Export] public int Index;
    [Export] Array<CompressedTexture2D> shipTextures = [];
    [Export] Array<CompressedTexture2D> missileTextures = [];
    [Export] CompressedTexture2D bulletTexture;
    [Export] Node2D bulletsHolder;
    [Export] Sprite2D thrustFire;
    [Export] Sprite2D shipSprite;
    [Export] AnimatedSprite2D explosionAnimation;
    [Export] AnimatedSprite2D shipExplosionAnimation;
    [Export] ProgressBar lifeBar;
    [Export] ProgressBar loadingBar;
    [Export] Label lblStatusText;

    Sprite2D[] bullets;
    readonly Sprite2D missileSprite = new();

    PlayerInfo playerConnection;

    bool alive;

    public override void _Ready()
    {
        alive = true;
        missileSprite.Name = "Missile";
        AddChild(missileSprite);
        missileSprite.Visible = false;
        explosionAnimation.Visible = false;
        lifeBar.MaxValue = StartingHealth;
        FixScale();
    }

    public override void _Process(double delta)
    {
        lblStatusText.Text = "";
        loadingBar.Visible = false;
        lifeBar.Visible = alive;

        if (!alive)
            return;

        var textColor = Colors.White;
        var barColor = Colors.White;
        var (step, total) = (0f, 0f);

        switch (playerConnection.State)
        {
            case PlayerConnectState.Connecting:
                textColor = Colors.LimeGreen;
                lblStatusText.Text = playerConnection.Handle.IsLocal() ? "Local Player" : "Connecting ...";
                break;
            case PlayerConnectState.Synchronizing:
                textColor = Colors.LightBlue;
                if (playerConnection.Handle.IsLocal())
                    lblStatusText.Text = "Local Player";
                else
                {
                    barColor = Colors.Cyan;
                    lblStatusText.Text = "Synchronizing";
                    total = 100;
                    step = playerConnection.ConnectProgress;
                }

                break;
            case PlayerConnectState.Disconnected:
                textColor = Colors.Crimson;
                lblStatusText.Text = "Disconnected";
                break;
            case PlayerConnectState.Disconnecting:
                textColor = Colors.Coral;
                barColor = Colors.Yellow;
                lblStatusText.Text = "Waiting for player";
                total = (float)playerConnection.DisconnectTimeout.TotalMilliseconds;
                step = (float)(DateTime.UtcNow - playerConnection.DisconnectStart).TotalMilliseconds;
                step = Mathf.Clamp(step, 0, total);
                break;
        }

        if (string.IsNullOrWhiteSpace(lblStatusText.Text))
            return;

        if (total > 0)
        {
            loadingBar.Visible = true;
            loadingBar.Modulate = barColor;
            loadingBar.MaxValue = total;
            loadingBar.Value = step;
        }

        lifeBar.Visible = false;
        lblStatusText.Modulate = textColor;
    }

    static readonly StringName sceneName = "ship";

    public static ShipNode Instantiate(Ship ship, PlayerInfo player)
    {
        var node = Prefab.Instantiate<ShipNode>(sceneName);

        if (node.bullets is null)
            node.InitializeBullets(ship);

        node.SetPlayer(player);
        return node;
    }

    void SetPlayer(PlayerInfo player)
    {
        ArgumentNullException.ThrowIfNull(player);
        playerConnection = player;
        SetIndex(player.Handle.Index);
    }

    public void SetIndex(int index)
    {
        Index = index;
        Name = $"Ship{index}";
        shipSprite.Texture = shipTextures[Index];
        missileSprite.Texture = missileTextures[Index];
    }

    public void FixScale()
    {
        var shipSize = shipSprite.Texture.GetSize();
        var newShipSize = new Vector2(ShipRadius, ShipRadius) * 2;
        Scale = newShipSize / shipSize;

        if (missileSprite.Texture is not null)
            missileSprite.GlobalScale =
                Vector2.One * (MissileProjectileRadius * 2f / missileSprite.Texture.GetHeight());

        var animTexture = explosionAnimation.SpriteFrames.GetFrameTexture(explosionAnimation.Animation, 0);
        explosionAnimation.GlobalScale = Vector2.One * (MissileExplosionRadius * 2f / animTexture.GetHeight());
    }

    public void UpdateShip(Ship ship)
    {
        alive = ship.Active;

        if (shipSprite.Visible && !alive && ship.Health <= 0 && !shipExplosionAnimation.IsPlaying())
        {
            shipExplosionAnimation.Visible = true;
            shipExplosionAnimation.Frame = 0;
            shipExplosionAnimation.Play();
        }

        if (!alive)
            Disable();

        Position = ship.Position;
        shipSprite.RotationDegrees = ship.Heading;
        thrustFire.Visible = ship.Thrust > 0;

        lifeBar.Rotation = 0;
        lifeBar.Value = ship.Health;

        var isExploding = ship.Missile.IsExploding();

        missileSprite.Visible = ship.Missile.Active && !isExploding;
        missileSprite.GlobalPosition = ship.Missile.Position;
        missileSprite.GlobalRotationDegrees = ship.Missile.Heading;

        explosionAnimation.Visible = isExploding;
        if (explosionAnimation.Visible)
        {
            explosionAnimation.GlobalPosition = ship.Missile.Position;
            var animationStep = (int)Mathf.Lerp(
                0, explosionAnimation.SpriteFrames.GetFrameCount(explosionAnimation.Animation) - 1,
                ship.Missile.HitBoxTime / (float)MissileHitBoxTimeout
            );

            explosionAnimation.Frame = animationStep;
        }

        for (var i = 0; i < bullets.Length; i++)
        {
            ref var nodeBullet = ref bullets[i];
            ref var bullet = ref ship.Bullets[i];

            nodeBullet.Visible = bullet.Active;
            nodeBullet.GlobalPosition = bullet.Position;
        }
    }

    void Disable()
    {
        shipSprite.Visible = false;
        lifeBar.Visible = false;
        loadingBar.Visible = false;
        lblStatusText.Visible = false;
    }

    [MemberNotNull(nameof(bullets))]
    void InitializeBullets(Ship source)
    {
        bullets = new Sprite2D[source.Bullets.Length];
        for (var i = 0; i < bullets.Length; i++)
        {
            Sprite2D bullet = new()
            {
                Visible = false,
                Texture = bulletTexture,
                GlobalScale = 3f * Vector2.One,
            };

            bulletsHolder.AddChild(bullet);
            bullets[i] = bullet;
        }
    }
}
