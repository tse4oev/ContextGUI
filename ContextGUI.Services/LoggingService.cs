using System;
using Serilog;
using ContextGUI.Services.Interfaces;

namespace ContextGUI.Services;

/// <summary>
/// Serilog-based logging implementation.
/// </summary>
public sealed class LoggingService : ILoggingService
{
    /// <inheritdoc />
    public void LogInformation(string message, params object[] args)
    {
        Log.Information(message, args);
    }

    /// <inheritdoc />
    public void LogWarning(string message, params object[] args)
    {
        Log.Warning(message, args);
    }

    /// <inheritdoc />
    public void LogError(Exception exception, string message, params object[] args)
    {
        Log.Error(exception, message, args);
    }
}
