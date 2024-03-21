using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using static System.Net.WebRequestMethods;

namespace RailGrinder
{
    internal class Program
    {
        static string cmd_template = """https://synthriderz.com/api/rankings?s={"mode":§mode§,"difficulty":§difficulty§,"modifiers":§modifiers§,"profile.id":§id§}&page=1&limit=10&sort=rank,ASC""";

        static string userid_template = """https://synthriderz.com/api/rankings?s={"mode":§mode§,"difficulty":§difficulty§,"modifiers":§modifiers§,"profile.name":"§name§"}&page=1&limit=10&sort=rank,ASC""";

        static string personal_template = """https://synthriderz.com/api/scores?join[]=leaderboard&join[]=leaderboard.beatmap&join[]=profile&join[]=profile.user&sort=rank,ASC&page=§page§&limit=10&s={"$and":[{"beatmap.published":true},{"profile.id":§userid§},{"leaderboard.mode":§mode§},{"leaderboard.difficulty":§difficulty§},{"modifiers":§modifiers§},{"leaderboard.beatmap.ost":true},{"leaderboard.challenge":0}]}""";

        static string leaderboard_template = @"https://synthriderz.com/api/leaderboards/§id§/scores?limit=10&page=§page§&sort=rank,ASC";

        static string all_leaderboards_template = """https://synthriderz.com/api/leaderboards?join[]=beatmap&page=§page§&limit=10&s={"$and":[{"beatmap.published":true},{"mode":§mode§},{"difficulty":§difficulty§},{"beatmap.ost":true},{"challenge":0}]}""";

        static double average_poor = 0;
        static double average_good = 0;
        static double average_perfect = 0;
        static double average_accuracy = 0;
        static double average_rank = 0;

        static async Task Main(string[] args)
        {
            if (args.Length != 5)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Rail");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write("Grinder ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("V1.1.6");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("by Nova_Max");
                Console.WriteLine("");
            }
            Console.ForegroundColor = ConsoleColor.White;

            List<dynamic> personal_leaderboard = new List<dynamic>();
            List<dynamic> all_leaderboards = new List<dynamic>();

            int userid = 224725;
            int difficulty = 4;
            int mode = 1;
            string modifiers = "0";
            bool played = true;
            bool average = false;

            if (args.Length == 1 && (args[0] == "-?" || args[0] == "-help"))
            {
                Console.WriteLine("Usage: RailGrinder [userid] [difficulty] [mode] [modifiers] [output path]");
                Console.WriteLine("[userid]:     <Integer>");
                Console.WriteLine("[difficulty]: 0:Easy - 1:Normal - 2:Hard - 3:Expert - 4:Master");
                Console.WriteLine("[mode]:       0:Rhythm - 1:Force");
                Console.WriteLine("[modifiers]:  0:No Modifiers - 1:Combined");
                return;
            }

            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;

                if (args.Length == 5)
                {
                    userid = Convert.ToInt32(args[0]);
                    difficulty = Convert.ToInt32(args[1]);
                    mode = Convert.ToInt32(args[2]);
                    modifiers = Convert.ToInt32(args[3]) == 0 ? "0" : "{}";

                    string req = cmd_template;
                    req = req.Replace("§difficulty§", difficulty.ToString());
                    req = req.Replace("§mode§", mode.ToString());
                    req = req.Replace("§modifiers§", modifiers);
                    req = req.Replace("§id§", userid.ToString());

                    string resp = await client.DownloadStringTaskAsync(req);
                    dynamic res_data = JObject.Parse(resp);


                    average_rank = res_data.data[0].rank_avg;

                    average_poor = res_data.data[0].poor_hit_percent;
                    average_good = res_data.data[0].good_hit_percent;
                    average_perfect = res_data.data[0].perfect_hit_percent;

                    average_accuracy = average_poor * 0.25 + average_good * 0.5 + average_perfect;

                    WriteFile(args[4]);

                    return;
                }

                //#if !DEBUG 
                Console.WriteLine("Select Search Operation: ");
                Console.WriteLine("1: Unplayed Maps");
                Console.WriteLine("2: Played Maps");
                Console.WriteLine("3: Average");
                bool invalid = false;
                do
                {
                    invalid = false;
                    string play = Console.ReadLine();
                    if (play == "1")
                    {
                        played = false;
                    }
                    else if (play == "2")
                    {
                        played = true;
                    }
                    else if (play == "3")
                    {
                        average = true;
                    }
                    else
                    {
                        invalid = true;
                        Console.WriteLine("Invalid input, try again: ");
                    }
                } while (invalid);

                Console.WriteLine("");
                Console.WriteLine("Select Difficulty: ");
                Console.WriteLine("1: Easy");
                Console.WriteLine("2: Normal");
                Console.WriteLine("3: Hard");
                Console.WriteLine("4: Expert");
                Console.WriteLine("5: Master (default)");
                do
                {
                    invalid = false;
                    string num = Console.ReadLine();
                    if (num == "1")
                    {
                        difficulty = 0;
                    }
                    else if (num == "2")
                    {
                        difficulty = 1;
                    }
                    else if (num == "3")
                    {
                        difficulty = 2;
                    }
                    else if (num == "4")
                    {
                        difficulty = 3;
                    }
                    else if (num == "5" || num == "")
                    {
                        difficulty = 4;
                    }
                    else
                    {
                        invalid = true;
                        Console.WriteLine("Invalid input, try again: ");
                    }
                } while (invalid);

                Console.WriteLine("");
                Console.WriteLine("Select Mode: ");
                Console.WriteLine("1: Rhythm");
                Console.WriteLine("2: Force");
                do
                {
                    invalid = false;
                    string mod = Console.ReadLine();
                    if (mod == "1")
                    {
                        mode = 0;
                    }
                    else if (mod == "2")
                    {
                        mode = 1;
                    }
                    else
                    {
                        invalid = true;
                        Console.WriteLine("Invalid input, try again: ");
                    }
                } while (invalid);

                Console.WriteLine("");
                Console.WriteLine("Select Modifiers: ");
                Console.WriteLine("1: No Modifiers  (default)");
                Console.WriteLine("2: Modifiers");
                do
                {
                    invalid = false;
                    string modifier = Console.ReadLine();
                    if (modifier == "1" || modifier == "")
                    {
                        modifiers = "0";
                    }
                    else if (modifier == "2")
                    {
                        modifiers = "{}";
                    }
                    else
                    {
                        invalid = true;
                        Console.WriteLine("Invalid input, try again: ");
                    }
                } while (invalid);

                Console.WriteLine("");
                Console.WriteLine("Enter Username (Capitalization Matters): ");
                do
                {
                    invalid = false;
                    string username = Console.ReadLine();

                    try
                    {
                        string req = userid_template;
                        req = req.Replace("§difficulty§", difficulty.ToString());
                        req = req.Replace("§mode§", mode.ToString());
                        req = req.Replace("§modifiers§", modifiers);
                        req = req.Replace("§name§", HttpUtility.UrlEncode(username));

                        string resp = await client.DownloadStringTaskAsync(req);
                        dynamic res_data = JObject.Parse(resp);
                        IEnumerable<dynamic> data = res_data.data;
                        var rankings = data.GroupBy(x => x.profile.id).Select(x => x.First()).ToList();
                        int index = 0;
                        if (rankings.Count > 1)
                        {
                            Console.WriteLine("Multiple users found with that name: ");
                            int count = 0;
                            foreach (var ranking in rankings)
                            {
                                count++;
                                Console.WriteLine(count + ": " + ranking.profile.id.ToString().PadRight(9, ' ') + " rank: " + ranking.rank);
                            }
                            Console.WriteLine("Please enter the one you want to use: ");
                            int selected = Convert.ToInt32(Console.ReadLine());
                            if (selected > rankings.Count || selected <= 0)
                            {
                                invalid = true;
                                Console.WriteLine("Invalid input, try again: ");
                            }
                            index = selected - 1;
                        }
                        if (rankings.Count > 0)
                        {
                            userid = rankings[index].profile.id;
                            average_rank = rankings[index].rank_avg;

                            average_poor = rankings[index].poor_hit_percent;
                            average_good = rankings[index].good_hit_percent;
                            average_perfect = rankings[index].perfect_hit_percent;

                            average_accuracy = average_poor * 0.25 + average_good * 0.5 + average_perfect;
                        }
                        else
                        {
                            invalid = true;
                            Console.WriteLine("User not found, try again: ");
                        }
                    }
                    catch (Exception e)
                    {
                        invalid = true;
                        Console.WriteLine("Error, try again: ");
                    }
                } while (invalid);


                //#endif
                Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine("User ID: " + userid);
                Console.WriteLine("");

                if (!average)
                {
                    int page = 0;
                    int pages = 0;

                    do
                    {
                        page++;
                        Console.WriteLine("Personal Leaderboard page: " + page + " of " + pages);
                        string req = personal_template;
                        req = req.Replace("§page§", page.ToString());
                        req = req.Replace("§userid§", userid.ToString());
                        req = req.Replace("§difficulty§", difficulty.ToString());
                        req = req.Replace("§mode§", mode.ToString());
                        req = req.Replace("§modifiers§", modifiers);

                        string resp = await client.DownloadStringTaskAsync(req);
                        dynamic res_data = JObject.Parse(resp);

                        personal_leaderboard.AddRange(res_data.data);
                        page = res_data.page;
                        pages = res_data.pageCount;
                    } while (page < pages);

                    personal_leaderboard = personal_leaderboard.GroupBy(x => x.leaderboard.beatmap.id).Select(x => x.OrderBy(y => y.modified_score).First()).ToList();


                    if (played)
                    {
                        int count = 0;
                        List<dynamic> results = new List<dynamic>();

                        foreach (var i in personal_leaderboard)
                        {
                            count++;
                            Console.Write("Map " + count + " of " + personal_leaderboard.Count);

                            int id = i.leaderboard.id;
                            int rank = Math.Min((int)i.rank, 200);
                            bool stop = false;
                            page = 0;
                            List<dynamic> map_leaderboard = new List<dynamic>();
                            if (rank > 1)
                            {
                                do
                                {
                                    page++;
                                    Console.Write(".");

                                    string req = leaderboard_template;
                                    req = req.Replace("§page§", page.ToString());
                                    req = req.Replace("§id§", id.ToString());

                                    string resp = await client.DownloadStringTaskAsync(req);
                                    resp = "{\"data\":" + resp + "}";

                                    dynamic res_data = JObject.Parse(resp);
                                    foreach (var j in res_data.data)
                                    {
                                        if (j.rank < rank)
                                        {
                                            map_leaderboard.Add(j);
                                        }
                                        else
                                        {
                                            stop = true;
                                        }
                                    }
                                } while (!stop);
                                Console.WriteLine("");
                            }

                            int average_score = i.modified_score;
                            if (map_leaderboard.Count() > 0)
                            {
                                average_score = (int)map_leaderboard.Average(x => x.baseScore);
                            }

                            results.Add(new
                            {
                                rank = i.rank,
                                ratio = (float)i.modified_score / average_score,
                                title = i.leaderboard.beatmap.title + " - " + i.leaderboard.beatmap.artist
                            });
                        }

                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("");
                        Console.WriteLine("Results (best at the top): ");
                        results = results.OrderBy(x => x.ratio).ToList();
                        foreach (var res in results)
                        {
                            Console.WriteLine("rank: " + res.rank.ToString().PadRight(4, ' ') + " ratio: " + res.ratio.ToString("n3") + " " + res.title);
                        }
                    }
                    else
                    {
                        page = 0;
                        pages = 0;

                        do
                        {
                            page++;
                            Console.WriteLine("All Leaderboard page: " + page + " of " + pages);
                            string req = all_leaderboards_template;
                            req = req.Replace("§page§", page.ToString());
                            req = req.Replace("§difficulty§", difficulty.ToString());
                            req = req.Replace("§mode§", mode.ToString());

                            string resp = await client.DownloadStringTaskAsync(req);
                            dynamic res_data = JObject.Parse(resp);

                            all_leaderboards.AddRange(res_data.data);
                            page = res_data.page;
                            pages = res_data.pageCount;
                        } while (page < pages);

                        int count = 0;
                        List<dynamic> results = new List<dynamic>();

                        all_leaderboards = all_leaderboards.GroupBy(x => x.beatmap.id).Select(x => x.OrderByDescending(y => y.scores).First()).ToList();
                        var unplayed_leaderboards = all_leaderboards.Where(x => !personal_leaderboard.Any(y => y.leaderboard.beatmap.id == x.beatmap.id)).ToList();

                        foreach (var leaderboard in unplayed_leaderboards)
                        {
                            count++;
                            Console.Write("Map " + count + " of " + unplayed_leaderboards.Count);

                            bool stop = false;
                            page = 0;

                            do
                            {
                                page++;

                                Console.Write(".");

                                string req = leaderboard_template;
                                req = req.Replace("§page§", page.ToString());
                                req = req.Replace("§id§", leaderboard.id.ToString());

                                string resp = await client.DownloadStringTaskAsync(req);
                                resp = "{\"data\":" + resp + "}";

                                dynamic res_data = JObject.Parse(resp);
                                foreach (var j in res_data.data)
                                {
                                    double accuracy = double.Parse((string)j.poorHitPercent) * 0.25 + double.Parse((string)j.goodHitPercent) * 0.5 + double.Parse((string)j.perfectHitPercent);

                                    if (accuracy <= average_accuracy)
                                    {
                                        results.Add(new
                                        {
                                            rank = j.rank,
                                            title = leaderboard.beatmap.title + " - " + leaderboard.beatmap.artist
                                        });
                                        stop = true;
                                        break;
                                    }
                                }
                                if (page > 20)
                                {
                                    stop = true;
                                }
                            } while (!stop);
                            Console.WriteLine("");
                        }

                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("");
                        Console.WriteLine("Results (estimated rank): ");
                        results = results.OrderBy(x => x.rank).ToList();
                        foreach (var res in results)
                        {
                            if (res.rank > average_rank)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.White;
                            }

                            Console.WriteLine("rank: " + res.rank.ToString().PadRight(4, ' ') + " " + res.title);
                        }

                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("");
            Console.WriteLine("Average Poor:     " + average_poor.ToString("n4"));
            Console.WriteLine("Average Good:     " + average_good.ToString("n4"));
            Console.WriteLine("Average Perfect:  " + average_perfect.ToString("n4"));
            Console.WriteLine("Average Accuracy: " + average_accuracy.ToString("n4"));
            Console.WriteLine("Average Rank:     " + average_rank.ToString("n2"));
            WriteFile("scores/" + userid + "" + difficulty + "" + mode + "" + modifiers + ".csv");

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        static void WriteFile(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (dir != "" && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            if (!System.IO.File.Exists(path))
            {
                using (StreamWriter sw = System.IO.File.CreateText(path))
                {
                    sw.WriteLine("Date, Accuracy, Perfect, Good, Poor, Rank");
                }
            }
            using (StreamWriter sw = System.IO.File.AppendText(path))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ", " + average_accuracy + ", " + average_perfect + ", " + average_good + ", " + average_poor + ", " + average_rank);
            }
        }
    }
}
