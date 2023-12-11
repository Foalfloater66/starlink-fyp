/* C138 Final Year Project 2023-2024 */

namespace Utilities.Logging
{
    public interface ILogger
    {
        void LogEntry(int framecount, LoggingContext ctx);

        void Save();
    }
}