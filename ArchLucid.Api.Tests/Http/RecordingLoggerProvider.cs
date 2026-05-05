using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Api.Tests.Http;

internal sealed class RecordingLoggerProvider : ILoggerProvider
{
    public IList<(LogLevel Level, EventId EventId, string Message)> Entries { get; } =
        new List<(LogLevel Level, EventId EventId, string Message)>();

    public ILogger CreateLogger(string categoryName) =>
        new RecordingLogger(Entries);

    public void Dispose()
    {
    }

    private sealed class RecordingLogger : ILogger
    {
        private readonly IList<(LogLevel Level, EventId EventId, string Message)> _entries;

        internal RecordingLogger(IList<(LogLevel Level, EventId EventId, string Message)> entries) =>
            _entries = entries;

        IDisposable ILogger.BeginScope<TState>(TState state) =>
            NullDisposable.Instance;

        bool ILogger.IsEnabled(LogLevel logLevel) =>
            true;

        void ILogger.Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (formatter is null)
                return;

            _entries.Add((logLevel, eventId, formatter(state!, exception)));
        }
    }

    private sealed class NullDisposable : IDisposable
    {
        internal static readonly NullDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
