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
        //********************************
        //To-do: Figure out why the combined_rank is being erroneously used for nomods
        //To-do: Correct the average calculation (inaccurate in the rankings for nomods. Needs to be recalculated.)
        //to-do: Improve the ROI calculation
        //to-do: Adjust ROI for missed notes and no-fail triggers

        static string cmd_template = """https://synthriderz.com/api/rankings?s={"mode":§mode§,"difficulty":§difficulty§,"modifiers":§modifiers§,"profile.id":§id§}&page=1&limit=10&sort=rank,ASC""";

        static string userid_template = """https://synthriderz.com/api/rankings?s={"mode":§mode§,"difficulty":§difficulty§,"modifiers":§modifiers§,"profile.name":"§name§"}&page=1&limit=10&sort=rank,ASC""";

        static string personal_template = """https://synthriderz.com/api/scores?join[]=leaderboard&join[]=leaderboard.beatmap&join[]=profile&join[]=profile.user&sort=rank,ASC&page=§page§&limit=10&s={"$and":[{"beatmap.published":true},{"profile.id":§userid§},{"leaderboard.mode":§mode§},{"leaderboard.difficulty":§difficulty§},{"modifiers":§modifiers§},{"leaderboard.beatmap.ost":true},{"leaderboard.challenge":0}]}""";

        //static string leaderboard_template = @"https://synthriderz.com/api/leaderboards/§id§/scores?limit=10&page=§page§&sort=rank,ASC";
        static string leaderboard_template = @"https://synthriderz.com/api/leaderboards/§id§/scores?limit=10&page=§page§&modifiers=§modifiers§&sort=rank,ASC";

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
                Console.WriteLine("V1.2.2");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("by Nova_Max and Marinus");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("This tool is designed to help analyze a player's performance based on leaderboard scores and find the best chances for ranking improvement. This really hammers the Synthriderz api, so please be kind and don't over-use it.");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Gray;
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
            string dm;
            int rank;
            string modifier ="0";

            // Pair each modifier value with its textual name:
            (int Value, string Name)[] ModifierMap =
            {
                (1,       "Spin90"),
                (2,       "Spin180"),
                (4,       "Spin360"),
                (8,       "Spin360Plus"),
                (16,      "NoFail"),
                (32,      "NoObstacles"),
                (64,      "HaloNotes"),
                (128,     "NJS2x"),
                (256,     "NJS3x"),
                (512,     "SuddenDeath"),
                (1024,    "PrismaticNotes"),
                (2048,    "VanishNotes"),
                (4096,    "SmallNotes"),
                (8192,    "BigNotes"),
                (16384,   "SpinStyled"),
                (32768,   "SpinWild"),
                (131072,  "SpiralMild"),
                (262144,  "SpiralStyled"),
                (524288,  "SpiralWild")
            };
            if (args.Length == 1 && (args[0] == "-?" || args[0] == "-help"))
            {
                Console.WriteLine("Usage: RailGrinder [userid] [difficulty] [mode] [modifiers] [output path]");
                Console.WriteLine("[userid]:     <Integer>");
                Console.WriteLine("[difficulty]: 0:Easy - 1:Normal - 2:Hard - 3:Expert - 4:Master");
                Console.WriteLine("[mode]:       0:Rhythm - 1:Force");
                //Console.WriteLine("[modifiers]:  0:No Modifiers - 1:Combined - 2:Spin (all) - 3:Spiral (all)");
                Console.WriteLine("[modifiers]:  0:No Modifiers - 1:Combined");
                Console.WriteLine("[path] (optional):  Path to save summary of averages");
                return;
            }

            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;

                // **************************************************************************************
                // Execute this analysis based on command line arguments
                // **************************************************************************************
                if (args.Length > 3)
                {
                    userid = Convert.ToInt32(args[0]);
                    difficulty = Convert.ToInt32(args[1]);
                    mode = Convert.ToInt32(args[2]);
                    modifiers =
                        Convert.ToInt32(args[3]) == 0 ? "0" :
                        Convert.ToInt32(args[3]) == 1 ? "{}" :
                        "0";

                    // Calculate and save the basic statistics to file if a path is provided
                    if (args.Length == 5)
                    {

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
                }
                else

                // **************************************************************************************
                // Prompt the user to provide the desired options for this analysis in the command line
                // **************************************************************************************
                {
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
                    Console.WriteLine("2: Combined");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("3: Spin (all) - developmental");
                    Console.WriteLine("4: Spiral (all) - developmental");
                    Console.ForegroundColor = ConsoleColor.White;
                    do
                    {
                        invalid = false;
                        modifier = Console.ReadLine();
                        //No Modifiers:
                        //    rankings api call requires "modifiers":0 
                        //    personal leaderboard api call requires "modifiers":0 
                        //    leaderboards api call requires modifiers=0
                        //Combined:
                        //    rankings api call requires "modifiers":{} 
                        //    personal leaderboard api call requires "modifiers":{} 
                        //    leaderboards api call requires modifiers=-1 [This is the last api called by this program, so we'll change the value of this variable from "{}" to "-1" before we query the api]
                        //
                        //Spin/Spiral:
                        //    The $in code here was an attempt to add support for combined Spin(all) and combined Spiral(all) rankings and analysis. the $in method works for leaderboard api, but fails for the ranking api.
                        //    Unfortunately, Synthriderz only has two ranking lists (no mods and combined) and no way to filter out others, so there is no way for this method to work at present like it does for 0 & {}.
                        //    I commented out the code vs deleting as it may help further future development of spin & spiral tools, but it will require additional changes to the approach since we can't use Z ranking lists
                        //    For now, we will use Combined leaderboards calls to validate the player.id and add additional filters later for maps that have not yet been played in spin/spiral.
                        //
                        //Note: The $in list is not 100% comprehensive, and only represents the most common modes used by scorechasers (2x, 3x, big, small).
                        //Every additional will double the length of the list. Maybe that's OK? Prisma, halo, nowalls, and nofail are presently excluded.
                        //I suppose we can experiment and TRY all 5184 combos, etc, and see at what point this breaks. The leaderboard API call appears to be working with 4 modes for spin and 27 modes/combos for spiral when manually called using the string below I was testing with.
                        //Test API call (all spins, vanilla mild): https://synthriderz.com/api/scores?join[]=leaderboard&join[]=leaderboard.beatmap&join[]=profile&join[]=profile.user&sort=rank,ASC&page=1&limit=10&s={%22$and%22:[{%22beatmap.published%22:true},{%22profile.id%22:1698739},{%22leaderboard.mode%22:0},{%22leaderboard.difficulty%22:4},{%22modifiers%22:{%22$in%22:[1,2,4,8]}},{%22leaderboard.beatmap.ost%22:true},{%22leaderboard.challenge%22:0}]}
                        //Spin: 3 modes + top 2 modifiers: 108 combinations. All mods: 5184 combinations.
                        //    For multiple modifiers: {"$in":[1,2,3,8]} //+additional 
                        //Spiral: 3 modes + top 2 modifiers: 27 combinations.  All mods: 1296 combinations
                        //    For all spiral including mild styled wild (vanilla) 2x 3x bignotes smallnotes: {"$in":[131072,135168,139264,131200,135296,139392,131328,135424,139520,262144,266240,270336,262272,266368,270464,262400,266496,270592,524288,528384,532480,524416,528512,532608,524544,528640,532736]}
                        //
                        if (modifier == "1" || modifier == "")
                        {
                            modifiers = "0";
                        }
                        else if (modifier == "2" || modifier == "3" || modifier == "4")
                        {
                            modifiers = "{}";
                        }
                        //We can filter for spin & spiral later. All top plays oif all modes will returned in this combined leaderboard.
                        //else if (modifier == "3")
                        //{
                        //    modifiers = "{\"$in\":[1,2,4,8,need to add the rest]}";
                        //}
                        //else if (modifier == "4")
                        //{
                        //    modifiers = "{\"$in\":[131072,135168,139264,131200,135296,139392,131328,135424,139520,262144,266240,270336,262272,266368,270464,262400,266496,270592,524288,528384,532480,524416,528512,532608,524544,528640,532736]}";
                        //}
                        else
                        {
                            invalid = true;
                            Console.WriteLine("Invalid input, try again: ");
                        }
                    } while (invalid);

                    //Prompt for a username, and then check to see if the username is valid and unique.
                    //If no leaderboards are found for that username, ask again.
                    //If multiple leaderboards are discovered for the same username, ask which one to use.
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
                            //Console.WriteLine("userid_template: " + req);

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
                                //This is not giving accurate values for combined leaderboards. I think it's including mods other than the top combined score.
                                //We'll probably need to recalculate this 
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
                }

                // **************************************************************************************
                // Build a list of all maps the user has played in the selected  difficulty/mode/mods
                // **************************************************************************************

                Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine("User ID: " + userid);
                Console.WriteLine("");
                Console.WriteLine("Loading Personal Leaderboards (10 leaderboards per page)");

                if (!average)
                {
                    int page = 0;
                    int pages = 0;

                    do
                    {
                        page++;
                        //Console.WriteLine("Personal Leaderboard page: " + page + " of " + pages);
                        Console.Write("Personal Leaderboard page: " + page + " of ");
                        string req = personal_template;
                        req = req.Replace("§page§", page.ToString());
                        req = req.Replace("§userid§", userid.ToString());
                        req = req.Replace("§difficulty§", difficulty.ToString());
                        req = req.Replace("§mode§", mode.ToString());
                        req = req.Replace("§modifiers§", modifiers);
                        //Console.WriteLine("Personal_template:" + req);

                        string resp = await client.DownloadStringTaskAsync(req);
                        dynamic res_data = JObject.Parse(resp);

                        personal_leaderboard.AddRange(res_data.data);
                        page = res_data.page;
                        pages = res_data.pageCount;
                        Console.WriteLine(pages);
                    } while (page < pages);

                    // Z will the top scores of all moves with modifiers={}, but the first of any map listed is the one that counts.
                    // but we can sort the personal leaderboard by modified_score descending to get the top scores first.
                    // the req api call is hardcoded to sort by rank, so no sort or filter is necessary here for nomods.
                    if (modifier == "3")
                    {
                        // SPIN
                        // Filter for spin bits (1, 2, 4, 8), and sort descending by modified score, best scores first.
                        //personal_leaderboard = personal_leaderboard.GroupBy(x => x.leaderboard.beatmap.id).Select(x => x.OrderByDescending(y => y.modified_score).Where(x => ((int)x.modifiers & (1 | 2 | 4 | 8)) != 0).First()).ToList();
                        personal_leaderboard = personal_leaderboard.Where(x => ((int)x.modifiers & (1 | 2 | 4 | 8)) != 0).GroupBy(x => x.leaderboard.beatmap.id).Select(x => x.OrderByDescending(y => y.modified_score).FirstOrDefault()).ToList();
                    }
                    else if (modifier == "4")
                    {
                        // SPIRAL
                        // Filter for spiral bits (SpinMild = 131072, SpiralStyled = 262144, SpiralWild = 524288), and sort descending by modified score, best scores first.
                        //personal_leaderboard = personal_leaderboard.GroupBy(x => x.leaderboard.beatmap.id).Select(x => x.OrderByDescending(y => y.modified_score).Where(x => ((int)x.modifiers & (1 | 2 | 4 | 8)) != 0).First()).ToList();
                        personal_leaderboard = personal_leaderboard.Where(x => ((int)x.modifiers & (131072 | 262144 | 524288)) != 0).GroupBy(x => x.leaderboard.beatmap.id).Select(x => x.OrderByDescending(y => y.modified_score).FirstOrDefault()).ToList();
                    } 
                    else //if (modifier == "2") or anything else, just give the combined leaderboard
                    {
                        // COMBINED
                        // personal_leaderboard = personal_leaderboard.GroupBy(x => x.leaderboard.beatmap.id).Select(x => x.OrderBy(y => y.rank_combined).First()).ToList();
                        // personal_leaderboard = personal_leaderboard.GroupBy(x => x.leaderboard.beatmap.id).Select(x => x.OrderByDescending(y => y.modified_score).First()).ToList();
                        personal_leaderboard = personal_leaderboard.GroupBy(x => x.leaderboard.beatmap.id).Select(x => x.OrderByDescending(y => y.modified_score).Where(x => x.modified_score > 0).First()).ToList();
                    }

                    // **************************************************************************************
                    // Analyze the performance and ranking of played maps for opportunities to improve
                    // **************************************************************************************
                    if (played)
                    {
                        int count = 0;
                        List<dynamic> results = new List<dynamic>();
                        Console.WriteLine("");
                        Console.WriteLine("Analyzing leaderboards for each map. This list represents each Rank(Modifiers) plus analysis of each page of higher ranked results (10 secores per .).");
                        //Console.WriteLine("(None = 0, Spin90 = 1, Spin180 = 2, Spin360 = 4, Spin360Plus = 8, NoFail = 16, NoObstacles = 32, HaloNotes = 64, NJS2x = 128, NJS3x = 256, SuddenDeath = 512, PrismaticNotes = 1024, VanishNotes = 2048, SmallNotes = 4096, BigNotes = 8192, SpinStyled = 16384, SpinWild = 32768, SpiralMild = 131072, SpiralStyled = 262144, SpiralWild = 524288)");

                        foreach (var i in personal_leaderboard)
                        {
                            count++;
                            //Console.Write("Map " + count + " of " + personal_leaderboard.Count);


                            //Convert the modifier integer into a descriptive string
                            //bool success = int.TryParse((string)i.modifiers, out int intmodifers);
                            if (i.modifiers == 0)
                            {
                                dm = "";
                            }
                            else
                            {
                                dm = ": ";

                                //var result = new List<string>();
                                int tm = i.modifiers;

                                for (int j = ModifierMap.Length - 1; j >= 0; j--)
                                {
                                    var (value, name) = ModifierMap[j];
                                    if (tm >= value)
                                    {
                                        if (dm != ": ")
                                        {
                                            dm = dm + (", ");
                                        };
                                        dm = dm + name;
                                        tm = tm - value;
                                    }
                                }

                            }

                            int id = i.leaderboard.id;
                            //leaderboard data:
                            //  combined rank = combined leaderboard rank
                            //  rank = top rank with selected settings
                            //         Note: this is only useful when modifiers=0.
                            //         For modifiers={} it returns the best rank with any combination of settings
                            //Both of these are unreliable and often return different values then the song leaderboards.
                            //#int rank = Math.Min((int)i.rank, 2000);
                            //Average all the scores better than the player, top 200 at worst, to limit the amount of API hammering.
                            if (modifier == "1") //nomods
                            {
                                rank = Math.Min((int)i.rank, 200);
                            }
                            else if (modifier =="2") //combined
                            {
                                rank = Math.Min((int)i.rank_combined, 200);
                            } else // We don't know rank yet for spin or spiral and need to pull the whole leaderboard to figure that out.
                            {
                                rank = 0;
                            };
                            bool stop = false;
                            page = 0;
                            List<dynamic> map_leaderboard = new List<dynamic>();

                            //Highlight blue and gold notes
                            string bolts = "  ";
                            Console.ForegroundColor = ConsoleColor.White;
                            if (i.notes_hit == i.leaderboard.max_combo)
                            {
                                if (i.poor_hit_percent == 0)
                                {
                                    if (i.good_hit_percent == 0)
                                    {
                                        bolts = "!!";
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                    }
                                    else
                                    {
                                        bolts = "! ";
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                    }
                                }
                            }
                            else 
                            {
                                bolts = "x ";
                                Console.ForegroundColor = ConsoleColor.Red;
                            }

                            Console.WriteLine("");
                            if (modifier == "1" && rank>0)
                            {
                                Console.Write("Map " + count + " of " + personal_leaderboard.Count + ": " + i.rank + bolts + " " + i.leaderboard.beatmap.artist + " - " + i.leaderboard.beatmap.title);
                            }
                            else if (modifier == "2" && rank>0)
                            {
                                Console.Write("Map " + count + " of " + personal_leaderboard.Count + ": " + i.rank_combined + bolts + " (" + i.modifiers + dm + ") " + i.leaderboard.beatmap.artist + " - " + i.leaderboard.beatmap.title);
                            }
                            else //display a ? for spin, spiral, or other null responses
                            {
                                Console.Write("Map " + count + " of " + personal_leaderboard.Count + ": ?" + bolts + " (" + i.modifiers + dm + ") " + i.leaderboard.beatmap.artist + " - " + i.leaderboard.beatmap.title);
                            }


                            //If not the top score, how much room for improvement is there?
                            if (rank > 1)
                            {
                                do
                                {
                                    page++;
                                    Console.Write(".");


                                    string req = leaderboard_template;
                                    req = req.Replace("§page§", page.ToString());
                                    req = req.Replace("§id§", id.ToString());
                                    if (modifiers == "0") 
                                    { 
                                        req = req.Replace("§modifiers§", "0"); 
                                    }
                                    else
                                    {
                                        req = req.Replace("§modifiers§", "-1");
                                    }

                                    string resp = await client.DownloadStringTaskAsync(req);
                                    resp = "{\"data\":" + resp + "}";

                                    dynamic res_data = JObject.Parse(resp);
                                    foreach (var j in res_data.data)
                                    {

                                        // The leaderboard contains rank and rank_combined:
                                        //     rank = the rank for that specific combination of modifiers
                                        //     rank_combined = the overall combined rank (modifers=-1), but ONLY for the best score.  All other modifier combos with lesser scores have rank_combined=0.
                                        //Only a single j.rank is available in the song leaderboard.  However j.rank==j.rank_combined if modifiers=-1
                                        //We need to compare this to the appropriate rank or rank.combined, which are stored independently in the personal leaderboard



                                        // ******* ADD LOGIC HERE TO CALCULATE THE LEADERBOARD RANK FOR SPIN AND SPIRAL ****
                                        // and maybe fix if we're misusing rank or rank_combined?

                                        if (j.rank < rank)
                                        {
                                            map_leaderboard.Add(j);
                                        }
                                        else
                                        {
                                            stop = true;
                                        }
                                    }
                                    //Exit if page++ exceeds the number of pages, as could happen in some edge cases.
                                    if(page>20) 
                                    {
                                        Console.Write("x ");
                                        Console.Write(req); 
                                        stop = true; 
                                    }

                                } while (!stop);
                                
                            }

                            //**********
                            //Check: Should we be using modified or base? 
                            //Modified_score will be the same regardless for modifiers=="0" for the filtered leaderboard, so should this be using modified_score for the average, too?
                            //Also, this is where we might do statistical analysis to look at the upper tail mean & standard deviation to develop a Z metric and a better ROI calculation.
                            int average_score = i.modified_score;
                            if (map_leaderboard.Count() > 0)
                            {
                                average_score = (int)map_leaderboard.Average(x => x.baseScore);
                            }
                            
                            //Calculate the new ROI metrics and save the results
                            //#Use rank for unmodified, or rank_combined if modified
                            if (modifiers == "0")
                            {
                                results.Add(new
                                {
                                    rank = i.rank,
                                    //ratio = 1-(float)i.modified_score / average_score,
                                    ratio = (float)i.rank * (1-((float)i.modified_score / average_score)),
                                    title = i.leaderboard.beatmap.title + " - " + i.leaderboard.beatmap.artist,
                                    bolts = bolts
                                });
                            }
                            else
                            {
                                results.Add(new
                                {
                                    rank = i.rank_combined,
                                    //ratio = (float)i.modified_score / average_score,
                                    ratio = (float)i.rank_combined * (1 - ((float)i.modified_score / average_score)),
                                    title = i.leaderboard.beatmap.title + " - " + i.leaderboard.beatmap.artist,
                                    bolts = bolts
                                }) ;

                            }
                        }

                        // ***********************************************************
                        // Display an ordered list of maps, from best rank to worse
                        // ***********************************************************
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("Results (best at the top): ");
                        results = results.OrderBy(x => x.rank).ToList();
                        foreach (var res in results)
                        {
                            //Highlight blue and gold notes
                            string rank_bolts = res.rank.ToString() + res.bolts;
                            Console.ForegroundColor = ConsoleColor.White;
                            if (res.bolts == "!!")
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                            }
                            else if (res.bolts == "! ")
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                            }
                            else if (res.bolts == "x ")
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                            }

                            Console.WriteLine("rank: " + rank_bolts.PadRight(4, ' ') + " ratio: " + res.ratio.ToString("n3") + " " + res.title);
                        }

                        // ***********************************************************
                        // Display an ordered list of maps, from highest to lower potential gain
                        // ***********************************************************
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("Results (most opportunity for improvement at the top): ");
                        //results = results.OrderByDescending(x => x.rank).ToList();
                        results = results.OrderBy(x => x.ratio).ToList();
                        results = results.OrderByDescending(x => x.ratio).ToList();
                        foreach (var res in results)
                        {
                            //Highlight blue and gold notes
                            string rank_bolts = res.rank.ToString() + res.bolts;
                            Console.ForegroundColor = ConsoleColor.White;
                            if (res.bolts == "!!")
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                            } else if (res.bolts == "! ")
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                            } else if (res.bolts == "x ")
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                            }

                            Console.WriteLine("rank: " + rank_bolts.PadRight(4, ' ') + " ratio: " + res.ratio.ToString("n3") + " " + res.title);
                        }
                    }
                    else
                    // Analyze unplayed maps for the best opportunities
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

                        //Reformat modifiers to query vanilla or combined
                        if (!(modifiers == "0")) { modifiers = "-1"; }

                        int count = 0;
                        List<dynamic> results = new List<dynamic>();

                        all_leaderboards = all_leaderboards.GroupBy(x => x.beatmap.id).Select(x => x.OrderByDescending(y => y.rank_combined).First()).ToList();
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
                                req = req.Replace("§modifiers§", modifiers);
                                //#req = req.Replace("§difficulty§", difficulty.ToString());
                                //#req = req.Replace("§mode§", mode.ToString());

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
