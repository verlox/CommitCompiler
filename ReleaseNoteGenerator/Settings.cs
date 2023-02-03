using System;
using Win = Microsoft.Win32;
using Veylib.Utilities;
using System.Drawing;
using System.Diagnostics;
using Veylib.ICLI;

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
                key.SetValue("removeDupes", removeDupes);
                CLI.WriteLine(new CLI.MessageProperties { Time = null }, Color.Green, "Exported settings to registry");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                CLI.WriteLine(new CLI.MessageProperties { Time = null }, "Failed to export settings to registry: ", Color.Red, ex.Message);
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
                removeDupes = bool.Parse(key.GetValue("removeDupes")?.ToString() ?? "true");
                CLI.WriteLine(new CLI.MessageProperties { Time = null }, Color.Green, "Imported settings from registry");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                CLI.WriteLine(new CLI.MessageProperties { Time = null }, "Failed to import settings from registry: ", Color.Red, ex.Message);
            }
        }

        internal static bool censorSwearing = true;
        internal static bool autoCapitalize = true;
        internal static bool addCommitHash = true;
        internal static bool filterCommits = true;
        internal static bool sortCommits = true;
        internal static bool removeDupes = true;
    }
}
