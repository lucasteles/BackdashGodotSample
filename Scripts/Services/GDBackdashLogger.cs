namespace SpaceWar.Services;

using Backdash.Core;

sealed class GDBackdashLogger : ILogWriter
{
    public void Dispose() { }

    public void Write(LogLevel level, char[] chars, int size)
    {
        string message = new(chars, 0, size);

        switch (level)
        {
            case LogLevel.None:
                break;
            case LogLevel.Trace:
            case LogLevel.Debug:
                Log.Debug(message);
                break;
            case LogLevel.Information:
                Log.Info(message);
                break;
            case LogLevel.Warning:
                Log.Warning(message);
                break;
            case LogLevel.Error:
                Log.Error(message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }
}
