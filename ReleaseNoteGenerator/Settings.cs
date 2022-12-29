using System;
using Win = Microsoft.Win32;
using Veylib.Utilities;
using System.Drawing;
using System.Diagnostics;

namespace ReleaseNoteGenerator
{
    internal class Settings
    {
        internal static void export()
        {
            try
            {
                Win.RegistryKey key = Registry.OpenOrCreate(Registry.BaseKey);
                key.SetValue("censorSwearing", censorSwearing);
                key.SetValue("autoCapitalize", autoCapitalize);
                key.SetValue("addCommitHash", addCommitHash);
                key.SetValue("filterCommits", filterCommits);
                key.SetValue("sortCommits", sortCommits);
                Program.core.WriteLine(Color.Green, "Exported settings to registry");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Program.core.WriteLine("Failed to export settings to registry: ", Color.Red, ex.Message);
            }
        }

        internal static void import()
        {
            try
            {
                Win.RegistryKey key = Registry.OpenOrCreate(Registry.BaseKey);
                censorSwearing = bool.Parse(key.GetValue("censorSwearing")?.ToString() ?? "true");
                autoCapitalize = bool.Parse(key.GetValue("autoCapitalize")?.ToString() ?? "true");
                addCommitHash = bool.Parse(key.GetValue("addCommitHash")?.ToString() ?? "true");
                filterCommits = bool.Parse(key.GetValue("filterCommits")?.ToString() ?? "true");
                sortCommits = bool.Parse(key.GetValue("sortCommits")?.ToString() ?? "true");
                Program.core.WriteLine(Color.Green, "Imported settings from registry");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Program.core.WriteLine("Failed to import settings from registry: ", Color.Red, ex.Message);
            }
        }

        internal static bool censorSwearing = true;
        internal static bool autoCapitalize = true;
        internal static bool addCommitHash = true;
        internal static bool filterCommits = true;
        internal static bool sortCommits = true;
    }
}
