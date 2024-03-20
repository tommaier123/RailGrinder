using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text;
using System.Web;
using static System.Net.WebRequestMethods;

namespace RailGrinder
{
    internal class Program
    {
        static string userid_template = """https://synthriderz.com/api/rankings?s={"mode":§mode§,"difficulty":§difficulty§,"modifiers":§modifiers§,"profile.name":"§name§"}&page=1&limit=10&sort=rank,ASC""";

        static string personal_template = """https://synthriderz.com/api/scores?join[]=leaderboard&join[]=leaderboard.beatmap&join[]=profile&join[]=profile.user&sort=rank,ASC&page=§page§&limit=10&s={"$and":[{"beatmap.published":true},{"profile.id":§userid§},{"leaderboard.mode":§mode§},{"leaderboard.difficulty":§difficulty§},{"modifiers":§modifiers§},{"leaderboard.beatmap.ost":true},{"leaderboard.challenge":0}]}""";

        static string leaderboard_template = @"https://synthriderz.com/api/leaderboards/§id§/scores?limit=10&page=§page§&sort=rank,ASC";

        static string all_leaderboards_template = """https://synthriderz.com/api/leaderboards?join[]=beatmap&page=§page§&limit=10&s={"$and":[{"beatmap.published":true},{"mode":§mode§},{"difficulty":§difficulty§},{"beatmap.ost":true},{"challenge":0}]}""";

        static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Rail");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("Grinder ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("V1.1.4");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("by Nova_Max");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.White;

            List<dynamic> personal_leaderboard = new List<dynamic>();
            List<dynamic> all_leaderboards = new List<dynamic>();

            int userid = 224725;
            int difficulty = 4;
            int mode = 1;
            bool played = true;
            string modifiers = "0";

            double average_poor = 0;
            double average_good = 0;
            double average_perefect = 0;
            double average_accuracy = 0;


            //#if !DEBUG
            Console.WriteLine("Select Search Operation: ");
            Console.WriteLine("0: Unplayed Maps");
            Console.WriteLine("1: Played Maps");
            string play = Console.ReadLine();
            if (play == "0")
            {
                played = false;
            }
            else if (play == "1")
            {
                played = true;
            }
            else
            {
                Console.WriteLine("Invalid input");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("");
            Console.WriteLine("Select Difficulty: ");
            Console.WriteLine("0: Easy");
            Console.WriteLine("1: Normal");
            Console.WriteLine("2: Hard");
            Console.WriteLine("3: Expert");
            Console.WriteLine("4: Master");
            string num = Console.ReadLine();
            if (num == "0")
            {
                difficulty = 0;
            }
            else if (num == "1")
            {
                difficulty = 1;
            }
            else if (num == "2")
            {
                difficulty = 2;
            }
            else if (num == "3")
            {
                difficulty = 3;
            }
            else if (num == "4")
            {
                difficulty = 4;
            }
            else
            {
                Console.WriteLine("Invalid input");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("");
            Console.WriteLine("Select Mode: ");
            Console.WriteLine("0: Rhythm");
            Console.WriteLine("1: Force");
            string mod = Console.ReadLine();
            if (mod == "0")
            {
                mode = 0;
            }
            else if (mod == "1")
            {
                mode = 1;
            }
            else
            {
                Console.WriteLine("Invalid input");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("");
            Console.WriteLine("Select Modifiers: ");
            Console.WriteLine("0: No Modifiers");
            Console.WriteLine("1: Modifiers");
            string modifier = Console.ReadLine();
            if (modifier == "0")
            {
                modifiers = "0";
            }
            else if (modifier == "1")
            {
                modifiers = "{}";
            }
            else
            {
                Console.WriteLine("Invalid input");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                return;
            }

            //#endif
            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                //#if !DEBUG
                Console.WriteLine("");
                Console.WriteLine("Enter Username (Capitalization Matters): ");
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
                    var ids = data.Select(x => x.profile.id).Distinct().ToList();
                    if (ids.Count > 1)
                    {
                        Console.WriteLine("Multiple users found with that name: ");
                        foreach (var id in ids)
                        {
                            Console.WriteLine(id);
                        }
                        Console.WriteLine("Please enter the one you want to use: ");
                        userid = Convert.ToInt32(Console.ReadLine());
                    }
                    else if (ids.Count == 1)
                    {
                        userid = ids[0];
                    }
                    else
                    {
                        Console.WriteLine("User not found");
                        Console.WriteLine("Press Enter to exit...");
                        Console.ReadLine();
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error");
                    Console.WriteLine("Press Enter to exit...");
                    Console.ReadLine();
                    return;
                }
                //#endif
                Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine("User ID: " + userid);
                Console.WriteLine("");

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

                average_poor = personal_leaderboard.Average(x => double.Parse((string)x.poor_hit_percent));
                average_good = personal_leaderboard.Average(x => double.Parse((string)x.good_hit_percent));
                average_perefect = personal_leaderboard.Average(x => double.Parse((string)x.perfect_hit_percent));

                average_accuracy = average_poor * 0.25 + average_good * 0.5 + average_perefect;

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

                        int average = i.modified_score;
                        if (map_leaderboard.Count() > 0)
                        {
                            average = (int)map_leaderboard.Average(x => x.baseScore);
                        }

                        results.Add(new
                        {
                            rank = i.rank,
                            ratio = (float)i.modified_score / average,
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
                        Console.WriteLine("rank: " + res.rank.ToString().PadRight(4, ' ') + " " + res.title);
                    }

                }
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("");
            Console.WriteLine("Average Poor:     " + average_poor.ToString("n4"));
            Console.WriteLine("Average Good:     " + average_good.ToString("n4"));
            Console.WriteLine("Average Perfect:  " + average_perefect.ToString("n4"));
            Console.WriteLine("Average Accuracy: " + average_accuracy.ToString("n4"));

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
    }
}
