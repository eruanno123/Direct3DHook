
namespace HellbladeSaver
{
    using HellbladeSaver.Helpers;
    using NDesk.Options;
    using System;
    using System.Drawing;
    using System.Text.RegularExpressions;

    internal class ProgramParameters
    {
        public bool ShowHelp { get; private set; }
        public HellbladeTrackingConfig TrackingConfig { get; private set; }

        public int Verbosity { get; private set; }

        public OptionSet ProgramOptionsSet { get; private set; }

        public override string ToString ()
        {
            return $"Config: {TrackingConfig}";
        }

        public static ProgramParameters Parse(params string[] args)
        {
            /* Default path where Hellblade data is stored */
            string defaultPathBase = 
                PathHelper.AddBackslash(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) + @"HellbladeGame\Saved\SaveGames";

            var prm = new ProgramParameters();
            var tcfg = new HellbladeTrackingConfig() {
                SaveGamePath = defaultPathBase,
                SaveGameFilter = "*.sav",
                SaveBackupPath = defaultPathBase + "\\Backup",
                DefaultNameFormat = "hellblade_{0:yyMMdd_HHmmss}_{1:000}_{2}",
                HellbladeExecutablePath  = "HellbladeGame-Win64-Shipping",
                TargetImageSize = null
            };
            
            prm.Verbosity = 1;
            var optionSet = new OptionSet() {
                { "h|help", p => prm.ShowHelp = true },
                { "f|format=", p => tcfg.DefaultNameFormat = p },
                { "p|process-name=", p => tcfg.HellbladeExecutablePath = p },
                { "o|output-path=", p => tcfg.SaveBackupPath = p },
                { "i|input-path=", p => tcfg.SaveGamePath = p },
                { "w|wildcard=", p =>  tcfg.SaveGameFilter = p },
                { "s|image-size=", p => tcfg.TargetImageSize = ParseSize(p.ToString()) },
                { "v|verbosity=", p => prm.Verbosity = int.Parse(p) }
            };

            var positional = optionSet.Parse(args);
            prm.ProgramOptionsSet = optionSet;
            prm.TrackingConfig = tcfg;

            return prm;
        }

        private static Size? ParseSize (string p)
        {
            var match = Regex.Match(p, "([0-9]+)x([0-9]+)");
            if (match.Success)
            {
                return new Size(
                    int.Parse(match.Groups[1].Value), 
                    int.Parse(match.Groups[2].Value)
                    );
            }
            else
            {
                return null;
            }
        }
    }
}
