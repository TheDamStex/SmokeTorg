namespace SmokeTorg.Common.Logging;

public class SimpleLogger : ILogger
{
    private readonly string _logFile = Path.Combine(AppContext.BaseDirectory, "app.log");

    public void Info(string message) => Write("INFO", message);
    public void Error(string message, Exception? ex = null) => Write("ERROR", $"{message} {ex}");

    private void Write(string level, string message)
    {
        var row = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
        File.AppendAllLines(_logFile, [row]);
    }
}
