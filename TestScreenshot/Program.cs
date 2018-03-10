
namespace HellbladeSaver
{
    using HellbladeSaver.Logger;
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;

    static class Program
    {
        private static void Main (string[] args)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");

                RunApplication(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FATAL: " + ex.Message);
            }
        }

        private static void RunApplication (string[] args)
        {
            var programParams = ProgramParameters.Parse(args);
            SimpleLogger.SetDefaultLogger(new SimpleLogger(Console.Error, (LogLevel)programParams.Verbosity));

            PrintHello(programParams);
            var hellbladeTracker = new HellbladeGameSaveTracker(programParams.TrackingConfig);

            SimpleLogger.Default.Info("STARTING TRACKING APPLICATION ...");
            SimpleLogger.Default.Info("YOU CAN PRESS 'Q' ANY TIME TO EXIT");
        
            hellbladeTracker.Run();

            WaitForExitKey();

            SimpleLogger.Default.Info("EXITING ...");
            hellbladeTracker.Cancel();

        }

        private static void WaitForExitKey ()
        {
            while (true)
            {
                var k = Console.ReadKey();
                if (k.Key == ConsoleKey.Q)
                {
                    break;
                }
            }
        }

        private static void PrintHello (ProgramParameters p)
        {
            Console.WriteLine(":::: HELLBLADE SAVER ::::");
            Console.WriteLine("-------------------------------------");
            if (p.ShowHelp)
            {
                var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                Console.WriteLine("  Application version: {0}", fvi.FileVersion);
                Console.WriteLine();
                Console.WriteLine("  Command line parameters:");
                p.ProgramOptionsSet.WriteOptionDescriptions(Console.Error);
                Console.WriteLine("-------------------------------------");
                Environment.Exit(1);
            }
            else
            {
                Console.WriteLine("Origin:          {0}", p.TrackingConfig.SaveGamePath);
                Console.WriteLine("Search pattern:  {0}", p.TrackingConfig.SaveGameFilter);
                Console.WriteLine("Backup path:     {0}", p.TrackingConfig.SaveBackupPath);
                Console.WriteLine("Backup format:   {0}", p.TrackingConfig.DefaultNameFormat);
                Console.WriteLine("Process name:    {0}", p.TrackingConfig.HellbladeExecutablePath);
                Console.WriteLine("-------------------------------------");
            }

        }
    }
}
