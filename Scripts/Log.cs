namespace SpaceWar;

public static class Log
{
    public static void Info(string message) => GD.Print(message);

    public static void Error(Exception ex, string message)
    {
        if (ex is not null)
            message = $"{message}\tException: {ex}";

        GD.PushError(message);
    }

    public static void Warning(string message) => GD.PushWarning(message);

    public static void Debug(
        string message
    )
    {
#if DEBUG
        Info(message);
#else
        // skip
#endif
    }

    public static void Error(string message) => Error(null, message);
}
