
namespace TestScreenshot.Logger
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class SimpleLogger
    {
        public static SimpleLogger Default { get; private set; }

        public SimpleLogger (LogLevel level = LogLevel.Info)
            : this (Console.Out, level)
        {
        }

        public SimpleLogger (TextWriter outputWriter, LogLevel level = LogLevel.Info)
        {
            _outputWriter = outputWriter;
            _currentLogLevel = level;
        }

        public static void SetDefaultLogger(SimpleLogger logger)
        {
            Default = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Log (LogLevel level, string message)
        {
            if (level >= _currentLogLevel)
            {
                WriteTime();
                WriteLogLevel(level);
                _outputWriter.WriteLine(message);
            }
        }

        public void Log (LogLevel level, string messageFormat, params object[] objs)
        {
            if (level >= _currentLogLevel)
            {
                WriteTime();
                WriteLogLevel(level);
                _outputWriter.Write(messageFormat, objs);
                _outputWriter.WriteLine();
                _outputWriter.Flush();
            }
        }

        private void WriteTime ()
        {
            _outputWriter.Write(DateTime.Now.ToString());
            _outputWriter.Write(" ");
        }

        private void WriteLogLevel(LogLevel level)
        {
            _outputWriter.Write(_mappings[level].PadRight(5));
            _outputWriter.Write(": ");
        }

        public void Trace (string message) => Log(LogLevel.Trace, message);
        public void Trace (string messageFormat, params object[] objs) => Log(LogLevel.Trace, messageFormat, objs);

        public void Info (string message) => Log(LogLevel.Info, message);
        public void Info (string messageFormat, params object[] objs) => Log(LogLevel.Info, messageFormat, objs);

        public void Warning (string message) => Log(LogLevel.Warning, message);
        public void Warning (string messageFormat, params object[] objs) => Log(LogLevel.Warning, messageFormat, objs);

        public void Error (string message) => Log(LogLevel.Error, message);
        public void Error (string messageFormat, params object[] objs) => Log(LogLevel.Error, messageFormat, objs);

        public void Fatal (string message) => Log(LogLevel.Fatal, message);
        public void Fatal (string messageFormat, params object[] objs) => Log(LogLevel.Fatal, messageFormat, objs);

        private LogLevel _currentLogLevel;
        private TextWriter _outputWriter;

        private readonly Dictionary<LogLevel, string> _mappings = new Dictionary<LogLevel, string> {
            { LogLevel.Trace,   "TRACE" },
            { LogLevel.Info,    "INFO"},
            { LogLevel.Warning, "WARN"},
            { LogLevel.Error,   "ERROR" },
            { LogLevel.Fatal,   "FATAL" }
        };
    }
}
