using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TestScreenshot.Logger;

namespace TestScreenshot
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main ()
        {
            SimpleLogger.SetDefaultLogger(new SimpleLogger(Console.Error, LogLevel.Trace));

            var tracker = new HellbladeGameSaveTracker(
                new HellbladeTrackingConfig()
                {
                    SaveGamePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\HellbladeGame\\Saved\\SaveGames",
                    SaveGameFilter = "*.sav",
                    SaveBackupPath = "Backup",
                    DefaultNameFormat = "LOCATION{0}",
                    HellbladeExecutablePath = "HellbladeGame-Win64-Shipping"
                });
            
            SimpleLogger.Default.Info("PRESS 'Q' TO EXIT");

            tracker.Run();

            while (true)
            {
                var k = Console.ReadKey();
                if (k.Key == ConsoleKey.Q)
                {
                    break;
                }
            }

            tracker.Cancel();

            SimpleLogger.Default.Info("EXITING ...");
        }
    }
}
