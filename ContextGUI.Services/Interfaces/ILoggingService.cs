namespace ContextGUI.Services.Interfaces;

/// <summary>
/// Provides logging capabilities for services.
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    void LogInformation(string message, params object[] args);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    void LogWarning(string message, params object[] args);

    /// <summary>
    /// Logs an error message with an exception.
    /// </summary>
    void LogError(System.Exception exception, string message, params object[] args);
}
