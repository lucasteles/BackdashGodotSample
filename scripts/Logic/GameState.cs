using Backdash.Serialization;
using SpaceWar.Models;
using Backdash;

namespace SpaceWar.Logic;

public sealed record GameState
{
    public Ship[] Ships = [];
    public Rect2I Bounds;
    public int FrameNumber;

    int NumberOfShips => Ships.Length;

    public void Init(int numberOfPlayers, Rect2I viewPort)
    {
        Ships = new Ship[numberOfPlayers];
        for (var i = 0; i < numberOfPlayers; i++)
            Ships[i] = new();

        FrameNumber = 0;
        Bounds = viewPort;
        Bounds = Bounds.Grow(-GameConstants.WindowPadding);

        var width = Bounds.Size.X;
        var height = Bounds.Size.Y;
        var r = height / 3;
        for (var i = 0; i < numberOfPlayers; i++)
        {
            var heading = (i + 1) * 360f / numberOfPlayers;
            var theta = Mathf.DegToRad(heading);
            var (cosT, sinT) = (Math.Cos(theta), Math.Sin(theta));
            var x = (float)Math.Round((width / 2.0) + (r * cosT), 2);
            var y = (float)Math.Round((height / 2.0) + (r * sinT), 2);
            Ships[i].Id = (byte)(i + 1);
            Ships[i].Position = new(x, y);
            Ships[i].Active = true;
            Ships[i].Heading = (int)((heading + 180) % 360);
            Ships[i].Health = GameConstants.StartingHealth;
            Ships[i].Radius = GameConstants.ShipRadius;
        }
    }

    public void SaveState(ref readonly BinaryBufferWriter writer)
    {
        writer.Write(in Bounds);
        writer.Write(in FrameNumber);
        writer.Write(in Ships); // Ship implements IBinarySerializable
    }

    public void LoadState(ref readonly BinaryBufferReader reader)
    {
        reader.Read(ref Bounds);
        reader.Read(ref FrameNumber);
        reader.Read(Ships);
    }

    static ShipInput GetShipAI(in Ship ship) => new(
        Heading: (ship.Heading + 5f) % 360f,
        Thrust: 0,
        Fire: false,
        Missile: false
    );

    static ShipInput ParseShipInputs(GameInputs inputs, in Ship ship)
    {
        if (!ship.Active)
            return new();

        float heading;
        if (inputs.HasFlag(GameInputs.RotateRight))
            heading = (ship.Heading + GameConstants.RotateIncrement) % 360f;
        else if (inputs.HasFlag(GameInputs.RotateLeft))
            heading = (ship.Heading - GameConstants.RotateIncrement + 360) % 360;
        else
            heading = ship.Heading;
        float thrust;
        if (inputs.HasFlag(GameInputs.Thrust))
            thrust = GameConstants.ShipThrust;
        else if (inputs.HasFlag(GameInputs.Break))
            thrust = -GameConstants.ShipThrust;
        else
            thrust = 0;
        return new(heading, thrust,
            inputs.HasFlag(GameInputs.Fire),
            inputs.HasFlag(GameInputs.Missile)
        );
    }

    public void UpdateShip(in Ship ship, in ShipInput inputs)
    {
        ship.Heading = (int)inputs.Heading;
        Vector2 dir = new(
            MathF.Cos(Mathf.DegToRad(ship.Heading)),
            MathF.Sin(Mathf.DegToRad(ship.Heading))
        );
        if (inputs.Fire && ship.FireCooldown is 0)
            for (var i = 0; i < ship.Bullets.Length; i++)
            {
                ref var bullet = ref ship.Bullets[i];
                if (bullet.Active)
                    continue;
                bullet.Active = true;
                bullet.Position = (ship.Position + (dir * ship.Radius)).RoundTo();
                bullet.Velocity = (ship.Velocity + (dir * GameConstants.BulletSpeed)).RoundTo();
                ship.FireCooldown = GameConstants.BulletCooldown;
                break;
            }

        if (inputs.Missile && ship.MissileCooldown is 0 && !ship.Missile.Active)
        {
            ship.MissileCooldown = GameConstants.MissileCooldown;
            ship.Missile.Active = true;
            ship.Missile.Heading = ship.Heading;
            ship.Missile.ProjectileRadius = GameConstants.MissileProjectileRadius;
            ship.Missile.ExplosionRadius = GameConstants.MissileExplosionRadius;
            ship.Missile.ExplodeTimeout = GameConstants.MissileExplosionTimeout;
            ship.Missile.HitBoxTime = GameConstants.MissileHitBoxTimeout;
            ship.Missile.Velocity = dir * GameConstants.MissileSpeed;
            ship.Missile.Position = (
                ship.Position + ship.Velocity +
                (dir * (ship.Radius + ship.Missile.ProjectileRadius))
            ).RoundTo();

            ship.Velocity += (ship.Missile.Velocity * -2).RoundTo();
        }

        ship.Thrust = Math.Sign(inputs.Thrust);
        if (ship.Thrust != 0)
        {
            ship.Velocity += (dir * inputs.Thrust).RoundTo();
            var magnitude = ship.Velocity.Length();
            if (magnitude > GameConstants.ShipMaxThrust)
                ship.Velocity = (ship.Velocity * GameConstants.ShipMaxThrust / magnitude).RoundTo();
        }

        ship.Position += ship.Velocity;
        if (ship.Position.X - ship.Radius < Bounds.Left() ||
            ship.Position.X + ship.Radius > Bounds.Right())
        {
            ship.Velocity.X *= -1;
            ship.Position.X += ship.Velocity.X * 2;
        }

        if (ship.Position.Y - ship.Radius < Bounds.Top() ||
            ship.Position.Y + ship.Radius > Bounds.Bottom())
        {
            ship.Velocity.Y *= -1;
            ship.Position.Y += ship.Velocity.Y * 2;
        }

        UpdateBullets(ship);
        UpdateMissile(ship);

        if (ship.FireCooldown > 0) ship.FireCooldown--;
        if (ship.MissileCooldown > 0) ship.MissileCooldown--;
        if (ship.Invincible > 0) ship.Invincible--;
        if (ship.Health <= 0) ship.Active = false;
    }

    void UpdateBullets(Ship ship)
    {
        for (var i = 0; i < ship.Bullets.Length; i++)
        {
            ref var bullet = ref ship.Bullets[i];
            if (!bullet.Active)
                continue;
            bullet.Position += bullet.Velocity;
            if (!Bounds.HasPoint((Vector2I)bullet.Position))
            {
                bullet.Active = false;
                continue;
            }

            for (var j = 0; j < NumberOfShips; j++)
            {
                ref var other = ref Ships[j];
                if (!other.Active)
                    continue;

                if (other.Missile.Active
                    && other.Id != ship.Id
                    && !other.Missile.IsExploding()
                    && bullet.Position.DistanceTo(other.Missile.Position) <=
                    other.Missile.ProjectileRadius)
                {
                    other.Missile.ExplodeTimeout = 0;
                    bullet.Active = false;
                    continue;
                }

                if (other.Id == ship.Id || other.Invincible > 0)
                    continue;

                if (bullet.Position.DistanceTo(other.Position) > other.Radius)
                    continue;

                ship.Score++;
                other.Health -= GameConstants.BulletDamage;
                bullet.Active = false;
                break;
            }
        }
    }

    void UpdateMissile(Ship ship)
    {
        if (!ship.Missile.Active)
            return;

        var missile = ship.Missile;
        missile.Position += missile.Velocity;
        if (missile.Velocity.Length() < GameConstants.MissileMaxSpeed)
            missile.Velocity += (
                missile.Velocity.Normalized() * GameConstants.MissileAcceleration
            ).RoundTo();

        if (missile.HitBoxTime <= 0)
            missile.Active = false;
        else
            for (var j = 0; j < NumberOfShips; j++)
            {
                ref var other = ref Ships[j];
                var distance = missile.Position.DistanceTo(other.Position);
                if (!missile.IsExploding() &&
                    distance - missile.ProjectileRadius <= other.Radius &&
                    (other.Id != ship.Id ||
                     // wait some frames to not friend fire
                     GameConstants.MissileExplosionTimeout - missile.ExplodeTimeout >= 30))
                {
                    missile.ExplodeTimeout = 0;
                }
                else if (other.Missile.Active && other.Id != ship.Id)
                {
                    var missileDistance = missile.Position.DistanceTo(other.Missile.Position);

                    // missile hits explosion
                    if (other.Missile.IsExploding())
                    {
                        if (missileDistance - missile.ProjectileRadius <=
                            other.Missile.ExplosionRadius)
                        {
                            other.Missile.ExplodeTimeout = 0;
                            missile.ExplodeTimeout = 0;
                        }
                    }
                    // missile hits other missile
                    else if (missileDistance - missile.ProjectileRadius <=
                             other.Missile.ProjectileRadius)
                    {
                        missile.ExplodeTimeout = 0;
                        other.Missile.ExplodeTimeout = 0;
                        missile.ExplosionRadius += missile.ExplosionRadius / 2;
                        other.Missile.ExplosionRadius += other.Missile.ExplosionRadius / 2;
                    }
                }

                if (missile.ExplodeTimeout > 0) continue;
                if (other.Invincible > 0) continue;
                if (distance - missile.ExplosionRadius > other.Radius) continue;
                if (other.Id != ship.Id)
                    ship.Score++;

                other.Health -= GameConstants.MissileDamage;
                other.Invincible = GameConstants.MissileInvincibleTime;

                var pushDirection = (other.Position - missile.Position).Normalized();
                other.Velocity = (pushDirection * GameConstants.ShipMaxThrust).RoundTo();
                other.Position += other.Velocity * 2;
            }

        if (!Bounds.HasPoint((Vector2I)missile.Position))
        {
            var normal = Vector2.Zero;
            if (missile.Position.X < Bounds.Left()) normal = Vector2.Right;
            else if (missile.Position.X > Bounds.Right()) normal = Vector2.Left;
            else if (missile.Position.Y < Bounds.Top()) normal = Vector2.Down;
            else if (missile.Position.Y > Bounds.Bottom()) normal = Vector2.Up;
            else missile.ExplodeTimeout = 0;
            var newVelocity = -missile.Velocity.Reflect(normal);

            missile.Heading = (int)Mathf.RadToDeg(MathF.Atan2(newVelocity.Y, newVelocity.X));
            missile.Velocity = newVelocity;
        }

        if (missile.ExplodeTimeout > 0)
            missile.ExplodeTimeout--;
        if (missile.ExplodeTimeout is 0)
            missile.Velocity = Vector2.Zero;
        if (missile.IsExploding())
            missile.HitBoxTime--;
    }

    public void Update(ReadOnlySpan<SynchronizedInput<GameInputs>> inputs)
    {
        FrameNumber++;
        for (var i = 0; i < NumberOfShips; i++)
        {
            ref var ship = ref Ships[i];
            var gameInput = inputs[i].Disconnected
                ? GetShipAI(in ship)
                : ParseShipInputs(inputs[i], in ship);
            UpdateShip(in ship, in gameInput);
        }
    }
}
