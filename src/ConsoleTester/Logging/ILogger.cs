namespace ConsoleTester.Logging
{
    public interface ILogger
    {
        void LogError(string message);
        void LogInformation(string message);
        void LogVerbose(string message);
        void LogWarning(string message);
    }
}