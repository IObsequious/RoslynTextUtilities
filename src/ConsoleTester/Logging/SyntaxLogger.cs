using System;
using System.IO;

namespace ConsoleTester.Logging
{
    public class SyntaxLogger
    {
        private TextWriter _writer;

        public SyntaxLogger(TextWriter writer)
        {
            _writer = writer;
        }

        public void LogVerbose(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            WriteLine(message);
            Console.ResetColor();
        }

        public void LogInformation(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            WriteLine(message);
            Console.ResetColor();
        }

        public void LogWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red | ConsoleColor.Yellow;
            WriteLine(message);
            Console.ResetColor();
        }

        public void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLine(message);
            Console.ResetColor();
        }

        private void Write(string value) => _writer.Write(value);

        private void WriteLine(string value) => _writer.WriteLine(value);
    }
}

