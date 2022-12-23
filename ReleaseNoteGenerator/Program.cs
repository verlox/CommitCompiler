using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veylib.Utilities.Net;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net.Configuration;
using System.Security.Policy;

namespace ReleaseNoteGenerator
{
    internal class Program
    {
        static Regex keywordsRegex = new Regex(@"add|update|change|fix|remove|pull request|rename|cleanup|format");
        static Regex swearingRegex = new Regex(@"([a-z0-9]{0,99})(fuck|shit|ass)([a-z0-9]{0,99})", RegexOptions.IgnoreCase);
        static List<string> swearingWhitelist = new List<string> { "class", "password" };
        static void Main(string[] args)
        {
            string sinceSha = "a0cd691637d94e50421f78990484f05283053c6d";
            Dictionary<string, List<string>> messages = new Dictionary<string, List<string>>
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

            int page = 1;
            int totalCommits = 0;
            try
            {
                bool foundSha = false;
                while (!foundSha)
                {
                    NetRequest request = new NetRequest($"https://api.github.com/repos/verlox/Veylib/commits?page={page}&per_page=100");
                    request.SetHeader("User-Agent", "CommitCompiler");
                    NetResponse response = request.Send();
                    dynamic json = response.ToJson();
                    if (response.Status != HttpStatusCode.OK)
                        throw new Exception(response.Content);
                    else if (json?.Count == 0 || json == null)
                        break;

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

            File.AppendAllText("commits.txt", $"\n# Statistics\n\n* **Commits done**: {totalCommits}\n\n(Generated with *CommitCompiler* made by **verlox**)");
        }
    }
}
