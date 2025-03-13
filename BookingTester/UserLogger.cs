using System.Collections.Concurrent;

namespace BookingTester
{
    public class UserLogger
    {
        private static ConcurrentDictionary<string, ConsoleColor> ColorMap = new();
        private static readonly BlockingCollection<(DateTime Time, string Name, string Message)> LogQueue = new();
        private static readonly Thread LoggerThread;
        static UserLogger()
        {
            ColorMap["System"] = ConsoleColor.White;
            ColorMap["Michal Steyn"] = ConsoleColor.Blue;
            ColorMap["David Steyn"] = ConsoleColor.Green;
            ColorMap["Zoe Steyn"] = ConsoleColor.Cyan;
            ColorMap["Isaac Steyn"] = ConsoleColor.Yellow;
            ColorMap["Tiffany Steyn"] = ConsoleColor.Magenta;

            LoggerThread = new Thread(ProcessQueue)
            {
                IsBackground = true // Ensures the thread stops when the application exits
            };
            LoggerThread.Start();
        }
        public static void Info(string name, string message)
        {
            // Add log entry to the queue
            LogQueue.Add((DateTime.Now, name, message));
        }

        public static void Info(string message)
        {
            // Add log entry to the queue
            LogQueue.Add((DateTime.Now, "System", message));
        }

        private static void ProcessQueue()
        {
            foreach (var log in LogQueue.GetConsumingEnumerable()) // Automatically handles new messages
            {
                Console.ForegroundColor = ColorMap.GetValueOrDefault(log.Name, ConsoleColor.White); // Default color

                // Properly align columns
                Console.WriteLine($"{log.Time,-20} | {log.Name,-15} | {log.Message}");
                Console.ResetColor();
            }
        }

        public static void StopLogging()
        {
            LogQueue.CompleteAdding(); // Stops processing new messages
            LoggerThread.Join();       // Waits for the thread to finish
        }
    }
}
