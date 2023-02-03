using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Veylib;
using Veylib.Utilities.Net;
using Veylib.ICLI;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Runtime.InteropServices;
using Veylib.Utilities;

/*
 * 
 * TODO
 * 
 * fix extra fucking lines caused by splash screen (?????)
 * 
 */

namespace ReleaseNoteGenerator
{
    internal class Program
    {
        internal static CLI core = new CLI();
        private static Regex keywordsRegex = new Regex(@"add|update|change|fix|remove|pull request|rename|cleanup|format");
        private static Regex swearingRegex = new Regex(@"([a-z0-9]{0,99})(fuck|shit|ass)([a-z0-9]{0,99})", RegexOptions.IgnoreCase);
        private static Regex shaRegex = new Regex(@"^[a-z0-9]{40}$", RegexOptions.IgnoreCase);
        private static Regex userRepoRegex = new Regex(@"^([a-z0-9\-]{1,39}/[a-z0-9\-\.]{1,100})$", RegexOptions.IgnoreCase);
        private static List<string> swearingWhitelist = new List<string> { "class", "password", "assembly" };
        private static List<string> rawMessages = new List<string>();
        private static Dictionary<string, List<string>> messages = new Dictionary<string, List<string>>
            {
                {
                    "add",
                    new List<string>()
                },
                {
                    "update",
                    new List<string>()
                },
                {
                    "remove",
                    new List<string>()
                },
                {
                    "change",
                    new List<string>()
                },
                {
                    "rename",
                    new List<string>()
                },
                {
                    "cleanup",
                    new List<string>()
                },
                {
                    "format",
                    new List<string>()
                },
                {
                    "fix",
                    new List<string>()
                },
                {
                    "pull request",
                    new List<string>()
                },
                {
                    "general",
                    new List<string>()
                }
            };

        static void ratelimitCheck(NetResponse response)
        {
            if (response.Headers.GetValues("X-Ratelimit-Remaining")[0] == "0")
            {
                CLI.WriteLine(Color.Red, $"Ratelimited until {General.FromEpoch(long.Parse(response.Headers.GetValues("X-Ratelimit-Reset")[0]))}, press any key to exit");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }
        static void Main(string[] args)
        {
            VeylibHandler.ProjectIdentifier = "CommitCompiler";
            VeylibHandler.SetExceptionHandlingMode(VeylibHandler.ExceptionHandlingMode.Handle);
            VeylibHandler.Exception += (e) =>
            {
                Debug.WriteLine(e);
                CLI.WriteLine(Color.Red, "Error in VeylibHandler: ", Color.OrangeRed, e.Message);
            };

            CLI.StartupProperties properties = new CLI.StartupProperties
            {
                Author = new CLI.StartupAuthorProperties
                {
                    Name = "verlox",
                    Url = "verlox.cc"
                },
                Version = "1.0.0.0",
                Title = new CLI.StartupConsoleTitleProperties
                {
                    Animated = false,
                    Text = "CommitCompiler"
                },
                Logo = new CLI.StartupLogoProperties
                {
                    AutoCenter = true,
                    Text = @"_________                           ___________________                        ___________            
__  ____/____________ __________ ______(_)_  /__  ____/____________ ______________(_)__  /____________
_  /    _  __ \_  __ `__ \_  __ `__ \_  /_  __/  /    _  __ \_  __ `__ \__  __ \_  /__  /_  _ \_  ___/
/ /___  / /_/ /  / / / / /  / / / / /  / / /_ / /___  / /_/ /  / / / / /_  /_/ /  / _  / /  __/  /    
\____/  \____//_/ /_/ /_//_/ /_/ /_//_/  \__/ \____/  \____//_/ /_/ /_/_  .___//_/  /_/  \___//_/     
                                                                       /_/                            "
                },
                DefaultMessageLabel = null,
                SplashScreen = new CLI.StartupSpashScreenProperties
                {
                    AutoGenerate = true,
                    DisplayProgressBar = true,
                }
            };

#if DEBUG
            properties.SplashScreen = null;
#endif

            // Start the console core
            CLI.Start(properties);

            // import settings
            Settings.import();

        pickOption:
            CLI.WriteLine(new CLI.MessageProperties { Time = null }, "Pick an option:");
            switch (new SelectionMenu("Start", "Settings", "Exit").Activate())
            {
                case "Start":
                    goto repoInput;
                case "Settings":
                    goto settingsMenu;
                default:
                    Environment.Exit(0);
                    return;
            }

        settingsMenu:
            CLI.Clear();
            CLI.WriteLine(new CLI.MessageProperties { Time = null }, "Options");
            string opt(bool input)
            {
                return $"{CLI.Formatting.Reset} [{(input ? $"{CLI.Formatting.CreateColorString(Color.Lime)}enabled " : $"{CLI.Formatting.CreateColorString(Color.Red)}disabled")}{CLI.Formatting.Reset}]";
            }
            SelectionMenu.Settings settings = new SelectionMenu.Settings
            {
                Style = new SelectionMenu.Style
                {
                    SelectionFormatTags = CLI.Formatting.Underline + CLI.Formatting.Italic,
                    PreOptionText = " > "
                }
            };

            SelectionMenu menu = new SelectionMenu(settings);
            menu.Options.AddRange(new List<string> { $"Censor swearing{opt(Settings.censorSwearing)}", $"Auto capitalize{opt(Settings.autoCapitalize)}", $"Add commit hash{opt(Settings.addCommitHash)}", $"Remove one word commits{opt(Settings.filterCommits)}", $"Sort commits into categories{opt(Settings.sortCommits)}", $"Skip duplicate commit messaegs{opt(Settings.removeDupes)}", $"{CLI.Formatting.CreateColorString(Color.Red)}Back" });
            
            switch (menu.Activate().Split(' ').First())
            {
                case "Censor":
                    Settings.censorSwearing = !Settings.censorSwearing;
                    goto settingsMenu;
                case "Auto":
                    Settings.autoCapitalize = !Settings.autoCapitalize;
                    goto settingsMenu;
                case "Add":
                    Settings.addCommitHash = !Settings.addCommitHash;
                    goto settingsMenu;
                case "Remove":
                    Settings.filterCommits = !Settings.filterCommits;
                    goto settingsMenu;
                case "Sort":
                    Settings.sortCommits = !Settings.sortCommits;
                    goto settingsMenu;
                case "Skip":
                    Settings.removeDupes = !Settings.removeDupes;
                    goto settingsMenu;
            }

            CLI.Clear();
            Settings.export();

            goto pickOption;

        repoInput:
            string repo = "";

            string userrepo = CLI.ReadLine("Enter repository url $ ", Color.White);
            userrepo = userrepo.Replace("https://github.com/", "");
            Match userRepoMatch = userRepoRegex.Match(userrepo);
            if (!userRepoMatch.Success)
                goto repoInput;

            repo = userRepoMatch.Groups[1].Value;

            NetRequest request = new NetRequest($"https://api.github.com/repos/{repo}");
            request.SetHeader("User-Agent", "CommitCompiler");
            NetResponse response = request.Send();

            if (response.Status != HttpStatusCode.OK)
            {
                ratelimitCheck(response);
                CLI.WriteLine(Color.Red, "Repo does not exist");
                goto repoInput;
            }

        shaInput:
            string sinceSha = CLI.ReadLine("Since commit (SHA hash) $ ", Color.White);

            if (shaRegex.Matches(sinceSha).Count != 1)
                goto shaInput;

            request = new NetRequest($"https://api.github.com/repos/{repo}/commits/{sinceSha}");
            request.SetHeader("User-Agent", "CommitCompiler");
            response = request.Send();

            if (response.Status != HttpStatusCode.OK)
            {
                ratelimitCheck(response);
                CLI.WriteLine(Color.Red, "Commit does not exist");
                goto shaInput;
            }

        retrieveCommits:

            int page = 1;
            int totalCommits = 0;
            try
            {
                bool foundSha = false;
                CLI.WriteLine("Started scanning for commits in repository ", Color.White, repo, "...");
                while (!foundSha)
                {
                    request = new NetRequest($"https://api.github.com/repos/{repo}/commits?page={page}&per_page=100");
                    request.SetHeader("User-Agent", "CommitCompiler");
                    response = request.Send();
                    dynamic json = response.ToJson();
                    if (response.Status != HttpStatusCode.OK)
                    {
                        ratelimitCheck(response);
                        CLI.WriteLine(Color.Red, $"Failed to send request: {response.Content}, retry?");
                        switch (new SelectionMenu("Retry", "Exit").Activate())
                        {
                            case "Retry":
                                goto retrieveCommits;
                            case "Exit":
                                Environment.Exit(0);
                                return;
                        }
                    }
                    else if (json?.Count == 0 || json == null)
                        break;

                    CLI.WriteLine("Reading page ", Color.White, page.ToString(), null, ", total commits on page: ", Color.White, json.Count.ToString());
                    foreach (dynamic commit in json)
                    {
                        totalCommits++;
                        string message = commit.commit.message;
                        string type = "general";

                        Match keyword = keywordsRegex.Match(message.ToLower());
                        if (keyword?.Value.Length > 0)
                            type = keyword.Value.ToLower();

                        if (type == "update" || type == "change")
                            type = "change";

                        messages.TryGetValue(type, out List<string> list);
                        if (!rawMessages.Contains(message.ToLower()) && Settings.removeDupes)
                        {
                            CLI.WriteLine("Ignoring commit with hash ", Color.White, commit.sha.ToString().Substring(0, 7), null, ", same message as previous commit");
                            list.Add($"* {(Settings.addCommitHash ? $"[{commit.sha.ToString().Substring(0, 7)}] " : "")}{(Settings.autoCapitalize ? $"{message[0].ToString().ToUpper()}{message.Substring(1, message.Length - 1).Split('\n')[0]}" : message)}");
                            rawMessages.Add(message.ToLower());
                        }
                        if (commit.sha == sinceSha)
                        {
                            CLI.WriteLine("Found commit hash that matches original (", Color.White, sinceSha.Substring(0, 7), null, "), total commits logged and sorted: ", Color.White, totalCommits.ToString());
                            foundSha = true;
                            break;
                        }
                    }

                    page++;
                }
            } catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            if (File.Exists("commits.txt"))
                File.Delete("commits.txt");

            CLI.WriteLine("Compiling into commits.txt");
            
            foreach (var msg in messages)
            {
                if (msg.Value.Count == 0)
                    continue;

                List<string> commits = new List<string>();
                foreach (string val in msg.Value)
                {
                    if (Settings.filterCommits)
                    {
                        if (val.Split(' ').Length < 4)
                            continue;
                        else if (val.Contains("Merge branch 'master") || val.Contains("dependabot"))
                            continue;
                    }

                    string censored = val;

                    if (Settings.censorSwearing)
                    {
                        foreach (Match match in swearingRegex.Matches(val))
                        {
                            string word = $"{match.Groups[1]}{match.Groups[2]}{match.Groups[3]}";
                            bool replace = true;
                            foreach (string swear in swearingWhitelist)
                            {
                                if (word.ToLower().Contains(swear))
                                {
                                    replace = false;
                                    break;
                                }
                            }

                            if (replace)
                                censored = censored.Replace(match.Value, new string('#', match.Value.Length));
                        }
                    }
                    commits.Add(censored);
                }

                if (commits.Count > 0)
                { 
                    if (Settings.sortCommits)
                        File.AppendAllText("commits.txt", $"## {msg.Key[0].ToString().ToUpper()}{msg.Key.Substring(1, msg.Key.Length - 1)}:\n\n");
    
                    File.AppendAllLines("commits.txt", commits);
                }
            }

            File.AppendAllText("commits.txt", $"\n# Statistics\n\n* **Commits done**: {totalCommits}\n\n(Generated with [*CommitCompiler*](https://github.com/verlox/CommitCompiler) made by **verlox**)");

            CLI.WriteLine(Color.LimeGreen, "Finished outputting to ", Color.Lime, "commits.txt", Color.LimeGreen, ", open the file?");
            if (new SelectionMenu("Yes", "No").Activate() == "Yes")
            {
                Process.Start("commits.txt");
                CLI.Write("Closing in 5 seconds");
                new Thread(() =>
                {
                    for (int i = 0;i < 5; i++)
                    {   
                        CLI.Write(".");
                        Thread.Sleep(1000);
                    }

                    Environment.Exit(0);
                }).Start();
            }
        }
    }
}
