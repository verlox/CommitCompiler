using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Veylib;
using Veylib.Utilities.Net;
using Veylib.ICLI;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net.Configuration;
using System.Security.Policy;
using System.Drawing;

namespace ReleaseNoteGenerator
{
    internal class Program
    {
        static Core core = new Core();
        static Regex keywordsRegex = new Regex(@"add|update|change|fix|remove|pull request|rename|cleanup|format");
        static Regex swearingRegex = new Regex(@"([a-z0-9]{0,99})(fuck|shit|ass)([a-z0-9]{0,99})", RegexOptions.IgnoreCase);
        static Regex shaRegex = new Regex(@"^[a-z0-9]{40}$", RegexOptions.IgnoreCase);
        static Regex userRepoRegex = new Regex(@"^([a-z0-9\-]{1,39}/[a-z0-9\-\.]{1,100})$", RegexOptions.IgnoreCase);
        static List<string> swearingWhitelist = new List<string> { "class", "password" };
        static Dictionary<string, List<string>> messages = new Dictionary<string, List<string>>
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
                core.WriteLine(Color.Red, $"Ratelimited until {General.FromEpoch(long.Parse(response.Headers.GetValues("X-Ratelimit-Reset")[0]))}, press any key to exit");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }
        static void Main(string[] args)
        {
            Core.StartupProperties properties = new Core.StartupProperties
            {
                Author = new Core.StartupAuthorProperties
                {
                    Name = "verlox",
                    Url = "verlox.cc"
                },
                Version = "1.0.0.0",
                Title = new Core.StartupConsoleTitleProperties
                {
                    Animated = false,
                    Text = "CommitCompiler"
                },
                Logo = new Core.StartupLogoProperties
                {
                    AutoCenter = true,
                    Text = @"
_________                           ___________________                        ___________            
__  ____/____________ __________ ______(_)_  /__  ____/____________ ______________(_)__  /____________
_  /    _  __ \_  __ `__ \_  __ `__ \_  /_  __/  /    _  __ \_  __ `__ \__  __ \_  /__  /_  _ \_  ___/
/ /___  / /_/ /  / / / / /  / / / / /  / / /_ / /___  / /_/ /  / / / / /_  /_/ /  / _  / /  __/  /    
\____/  \____//_/ /_/ /_//_/ /_/ /_//_/  \__/ \____/  \____//_/ /_/ /_/_  .___//_/  /_/  \___//_/     
                                                                       /_/                            "
                },
                DefaultMessageLabel = null,
                DefaultMessageTime = null,
                SplashScreen = new Core.StartupSpashScreenProperties
                {
                    AutoGenerate = true,
                    DisplayProgressBar = true
                }
            };
            core.Start(properties);

            

        repoInput:
            string repo = "";

            string userrepo = core.ReadLine("Enter repository url $ ", Color.White);
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
                core.WriteLine(Color.Red, "Repo does not exist");
                goto repoInput;
            }
        shaInput:
            string sinceSha = core.ReadLine("Since commit (SHA hash) $ ", Color.White);

            if (shaRegex.Matches(sinceSha).Count != 1)
                goto shaInput;

            request = new NetRequest($"https://api.github.com/repos/{repo}/commits/{sinceSha}");
            request.SetHeader("User-Agent", "CommitCompiler");
            response = request.Send();

            if (response.Status != HttpStatusCode.OK)
            {
                ratelimitCheck(response);
                core.WriteLine(Color.Red, "Commit does not exist");
                goto shaInput;
            }

            retrieveCommits:

            int page = 1;
            int totalCommits = 0;
            try
            {
                bool foundSha = false;
                core.WriteLine("Started scanning for commits in repository ", Color.White, repo, "...");
                while (!foundSha)
                {
                    request = new NetRequest($"https://api.github.com/repos/{repo}/commits?page={page}&per_page=100");
                    request.SetHeader("User-Agent", "CommitCompiler");
                    response = request.Send();
                    dynamic json = response.ToJson();
                    if (response.Status != HttpStatusCode.OK)
                    {
                        ratelimitCheck(response);
                        core.WriteLine(Color.Red, $"Failed to send request: {response.Content}, retry?");
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

                    core.WriteLine("Reading page ", Color.White, page.ToString(), null, ", total commits on page: ", Color.White, json.Count.ToString());
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
                        list.Add($"* [{commit.sha}] {message[0].ToString().ToUpper()}{message.Substring(1, message.Length - 1).Split('\n')[0]}");
                        if (commit.sha == sinceSha)
                        {
                            core.WriteLine("Found commit hash that matches original (", Color.White, sinceSha, null, "), total commits logged and sorted: ", Color.White, totalCommits.ToString());
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

            core.WriteLine("Compiling into commits.txt");
            foreach (var msg in messages)
            {
                if (msg.Value.Count == 0)
                    continue;

                File.AppendAllText("commits.txt", $"## {msg.Key[0].ToString().ToUpper()}{msg.Key.Substring(1, msg.Key.Length - 1)}:\n\n");
                foreach (string val in msg.Value)
                {
                    if (val.Split(' ').Length < 4)
                        continue;
                    else if (val.Contains("Merge branch 'master") || val.Contains("dependabot"))
                        continue;

                    string censored = val;
                    foreach (Match match in swearingRegex.Matches(val))
                    {
                        string word = $"{match.Groups[1]}{match.Groups[2]}{match.Groups[3]}";
                        if (swearingWhitelist.Contains(word))
                            break;

                        censored = censored.Replace(match.Value, new string('#', match.Value.Length));
                    }
                    File.AppendAllText("commits.txt", $"{censored}\n");
                }
            }

            File.AppendAllText("commits.txt", $"\n# Statistics\n\n* **Commits done**: {totalCommits}\n\n(Generated with [*CommitCompiler*](https://github.com/verlox/CommitCompiler) made by **verlox**)");

            core.WriteLine(Color.LimeGreen, "Finished outputting to ", Color.Lime, "commits.txt", Color.LimeGreen, ", open the file?");
            if (new SelectionMenu("Yes", "No").Activate() == "Yes")
            {
                Process.Start("commits.txt");
                core.WriteLine("Closing in 5 seconds...");
                new Thread(() =>
                {
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }).Start();
            }
        }
    }
}
