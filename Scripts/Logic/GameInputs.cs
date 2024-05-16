namespace SpaceWar.Logic;

[Flags]
public enum GameInputs : ushort
{
    None = 0,
    Thrust = 1 << 0,
    Break = 1 << 1,
    RotateLeft = 1 << 2,
    RotateRight = 1 << 3,
    Fire = 1 << 4,
    Missile = 1 << 5,
}

public static class GameInput
{
    public static GameInputs ReadInputs()
    {
        var result = GameInputs.None;
        if (Input.IsActionPressed(ActionNames.Up, true))
            result |= GameInputs.Thrust;
        if (Input.IsActionPressed(ActionNames.Down, true))
            result |= GameInputs.Break;
        if (Input.IsActionPressed(ActionNames.Left, true))
            result |= GameInputs.RotateLeft;
        if (Input.IsActionPressed(ActionNames.Right, true))
            result |= GameInputs.RotateRight;
        if (Input.IsActionPressed(ActionNames.Fire, true))
            result |= GameInputs.Fire;
        if (Input.IsActionPressed(ActionNames.Bomb, true))
            result |= GameInputs.Missile;

        return result;
    }
}
