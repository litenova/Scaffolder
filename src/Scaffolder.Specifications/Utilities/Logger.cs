namespace Scaffolder.Specifications.Utilities;

/// <summary>
/// Static logger for specifications library.
/// Gets configured by the host application (CLI in this case).
/// </summary>
public static class Logger
{
    private static Action<string> _info = _ => { };
    private static Action<string> _warning = _ => { };
    private static Action<string> _error = _ => { };
    private static Action<string> _debug = _ => { };

    /// <summary>
    /// Configures the logger with the provided actions.
    /// Should be called once at application startup.
    /// </summary>
    public static void Configure(
        Action<string> info,
        Action<string> warning,
        Action<string> error,
        Action<string> debug)
    {
        _info = info ?? throw new ArgumentNullException(nameof(info));
        _warning = warning ?? throw new ArgumentNullException(nameof(warning));
        _error = error ?? throw new ArgumentNullException(nameof(error));
        _debug = debug ?? throw new ArgumentNullException(nameof(debug));
    }

    public static void Info(string message) => _info(message);
    public static void Warning(string message) => _warning(message);
    public static void Error(string message) => _error(message);
    public static void Debug(string message) => _debug(message);
}

