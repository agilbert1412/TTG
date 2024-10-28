/*using System;
using System.IO;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Linq;
using TTGHotS.Events;
using TTGHotS.Twitch;

namespace TTGHotS
{
    class ProgramOriginal
    {
        public static Dictionary<string, Event> eventDict = new Dictionary<string, Event>();
        public static List<Goal> goalsList = new List<Goal>(); //This might have to be something else later.
        public static List<QueuedEvent> eventQueue = new List<QueuedEvent>();
        public static Random random = new Random();
        public static int potvactivations = 0;

        public const string BattleNetBank = @"InsertBankPath\EventsBank.SC2Bank"; // This is the one to use
        public const string LocalBank = @"InsertBankPath\EventsBank.SC2Bank";
        public const string XML_EVENT = "/Bank/Section[@name='Event']/Key[@name='Name']/Value";
        public const string XML_LOCK = "/Bank/Section[@name='Event']/Key[@name='Lock']/Value";
        public const string XML_VALUE = "/Bank/Section[@name='Event']/Key[@name='Value']/Value";
        public const string XML_ARGS = "/Bank/Section[@name='Event']/Key[@name='Args']/Value";
        public const string XML_USERNAME = "/Bank/Section[@name='Event']/Key[@name='Username']/Value";
        public const string XML_PLANET = "/Bank/Section[@name='CurrentMission']/Key[@name='Planet']/Value";
        public const string XML_MISSION = "/Bank/Section[@name='CurrentMission']/Key[@name='Name']/Value";
        public static string hybridstr = "0000000000000000000000000000000000000000111111111111111111111111111111111111111122222222444444443333";
        //public static Goal campaignGoal = new Goal(500000, "campaign", "onetime");
        private static async Task MainOriginal(string[] args)
        {
            //doTrivia();

            //string password = "oauth:tj398z27qxurp4pmofxgl5srx31m8u";
            //string botUsername = "twitchofliberty";
            var password = ""; //oauth2
            var botUsername = "";
            var channel = "giantgrantgames";

            SetupData();

            Console.WriteLine(goalsList.Count + " is the total goal count.");
            Console.WriteLine(eventDict.Values.Count + " is the total events count.");
            //ExportEvents();
            //ExportGoals();
            //return;

            TwitchBot bot = new TwitchBot(botUsername, password);
            bot.Start().SafeFireAndForget();
            await bot.JoinChannel(channel.ToLower());
            await bot.SendMessage(channel, "TTG awaiting lunch orders.");

            bot.OnMessage += async (sender, tcm) =>
            {
                //Console.WriteLine($"{tcm.Sender} said '{tcm.Message}'");

                if (tcm.Message.ToLower().StartsWith("!eadd "))
                {
                    if (tcm.Sender.ToLower() == "giantgrantgames" || tcm.Sender.ToLower() == "7thace")
                    {
                        tcm.Message = tcm.Message.Replace("!eadd ", "cheer");
                    }
                }

                if (tcm.Message.ToLower().StartsWith("!bits "))
                {
                    var invokedEvent = ProcessCommand(tcm);
                    if (invokedEvent != null)
                    {
                        await bot.SendMessage(tcm.Channel, $"{invokedEvent.name} is at {invokedEvent.bank}/{invokedEvent.cost} bits.");
                    }
                }

                if (tcm.Sender.ToLower() == "giantgrantgames" || tcm.Sender.ToLower() == "7thace")
                {
                    if (tcm.Message.ToLower().StartsWith("!hey"))
                    {
                        await bot.SendMessage(tcm.Channel, "Acknowledged.");
                    }

                    if (tcm.Message.ToLower().StartsWith("!fe"))
                    {
                        string[] messageParts = tcm.Message.ToLower().Split(" ", 2);
                        if (tcm.Message.ToLower().Split(" ").Length > 1)
                        {
                            var forcedEvent = new Event();
                            forcedEvent.name = messageParts[1];
                            forcedEvent.bank = 1;
                            forcedEvent.cost = 1;
                            forcedEvent.donationType = "onetime";
                            forcedEvent.mission = "all";
                            forcedEvent.tier = 0;
                            var qfe = new QueuedEvent(forcedEvent);
                            qfe.queueCount = 1;
                            qfe.username = "Chat";
                            eventQueue.Insert(0, qfe);
                            await bot.SendMessage(tcm.Channel, $"@7thAce Queued up {forcedEvent.name}.");
                        }
                    }

                    if (tcm.Message.ToLower().StartsWith("!sb "))
                    {
                        foreach (var e in eventDict.Values.ToList<Event>())
                        {
                            int bitsvalue;
                            string[] lineparts = tcm.Message.ToLower().Split(" ", 3);
                            var success = int.TryParse(lineparts[1], out bitsvalue);
                            var eventname = lineparts[2];
                            if (e.name.ToLower() == eventname.ToLower().Replace(" ", ""))
                            {
                                e.bank = bitsvalue;
                                await bot.SendMessage(tcm.Channel, $"{e.name} set to  {e.bank} bits.");
                                return;
                            }
                        }

                        foreach (var g in goalsList)
                        {
                            int bitsvalue;
                            string[] lineparts = tcm.Message.ToLower().Split(" ", 3);
                            var success = int.TryParse(lineparts[1], out bitsvalue);
                            var goalname = lineparts[2];
                            if (g.mission.ToLower() == goalname.ToLower().Replace(" ", ""))
                            {
                                g.currentBank = bitsvalue;
                                await bot.SendMessage(tcm.Channel, $"{g.mission} set to {g.currentBank} bits.");
                            }
                        }
                    }

                    if (tcm.Message.ToLower().StartsWith("!setmission "))
                    {
                        string mission = tcm.Message.ToLower().Split(" ", 2)[1];
                        var planet = getPlanetFromMission(mission);
                        WriteXML(XML_PLANET, planet);
                        await bot.SendMessage(tcm.Channel, $"Force set: {mission} and {planet}!");
                    }
                }


                if (tcm.Message.ToLower().Contains("cheer")) /* tcm.Message.ToLower().Contains("chair") ||  *//*
                {
                    //tcm.Message = tcm.Message.Replace("chair", "cheer");
                    tcm.Message = tcm.Message.ToLower();
                    Console.WriteLine("TCM: " + tcm.Message);
                    var cheerValue = ProcessCheer(tcm);
                    LogPay(tcm, cheerValue);
                    var invokedEvent = new QueuedEvent(ProcessCommand(tcm));
                    invokedEvent.username = tcm.Sender;
                    if (cheerValue > 0)
                    {
                        AssignBitsToGoals(cheerValue);
                        if (!(invokedEvent.baseEvent is null))
                        {
                            invokedEvent.baseEvent.bank += cheerValue;
                            if (invokedEvent.baseEvent.cost <= invokedEvent.baseEvent.bank)
                            {
                                await bot.SendMessage(tcm.Channel, $"Detected {cheerValue} bits from {tcm.Sender} to activate {invokedEvent.baseEvent.name}!");
                            } else
                            {
                                await bot.SendMessage(tcm.Channel, $"Detected {cheerValue} bits from {tcm.Sender}.  {invokedEvent.baseEvent.name} is now at {invokedEvent.baseEvent.bank} / {invokedEvent.baseEvent.cost}.");
                            }

                            while (invokedEvent.baseEvent.bank >= invokedEvent.baseEvent.cost)
                            {
                                invokedEvent.baseEvent.CallEvent();
                                invokedEvent.baseEvent.CalculateNewCost();
                                LogEvent(tcm, invokedEvent.baseEvent);

                                var isInQueue = false;
                                foreach (var qe in eventQueue)
                                {
                                    if (qe.baseEvent.name == invokedEvent.baseEvent.name)
                                    {
                                        if (qe.baseEvent.donationType == "dynamic")
                                        {
                                            qe.queueCount += invokedEvent.baseEvent.tier;
                                        } else
                                        {
                                            qe.queueCount += 1;
                                        }
                                        isInQueue = true;
                                        Console.WriteLine($"Increased queue count of {invokedEvent.baseEvent.name} to {qe.queueCount}.");
                                    }
                                }
                                if (!isInQueue)
                                {
                                    eventQueue.Add(invokedEvent);
                                    if (invokedEvent.baseEvent.donationType == "dynamic")
                                    {
                                        invokedEvent.queueCount = invokedEvent.baseEvent.tier;
                                    } else
                                    {
                                        invokedEvent.queueCount = 1;
                                    }
                                    Console.WriteLine($"Added {invokedEvent.baseEvent.name} to queue.");
                                }
                            }
                            PrintEventQueue();
                        } 
                    }
                }
                ExportData();
                Dequeue();
            };

            await Task.Delay(-1);
        }


        private static void AssignBitsToGoals(int bitsCount)
        {
            var curMission = ReadXML(XML_MISSION).ToLower();
            var curPlanet = ReadXML(XML_PLANET).ToLower();
            var planetText = "0 / 0";
            var missionText = "0 / 0";
            var campaignText = "0 / 0";
            Console.WriteLine($"Attempting to assign {bitsCount} bits to goals.  Found Mission: {curMission} on planet {curPlanet}.");
            foreach (var goal in goalsList)
            {
                if (goal.timeframe == "mission")
                {
                    if (goal.mission.ToLower() == curMission.ToLower())
                    {
                        goal.currentBank += bitsCount;
                        Console.WriteLine($"Added {bitsCount} ({goal.currentBank}/{goal.cost}) to the mission goal for {goal.mission}.");
                        missionText =  $"{goal.currentBank} / {goal.cost}";
                    }
                }
                if (goal.timeframe == "planet")
                {
                    if (goal.planet.ToLower() == curPlanet.ToLower())
                    {
                        goal.currentBank += bitsCount;
                        Console.WriteLine($"Added {bitsCount} ({goal.currentBank}/{goal.cost}) to the planet goal for {goal.mission}.");
                        planetText =  $"{goal.currentBank} / {goal.cost}";
                    }
                }
                if(goal.timeframe == "campaign")
                {
                    goal.currentBank += bitsCount;
                    campaignText =  $"{goal.currentBank} / {goal.cost}";
                }

                Console.WriteLine($"Comparing {goal.currentBank} to {goal.cost} for {goal.mission} and {curMission}.");
                if (goal.currentBank >= goal.cost && goal.mission.ToLower() == curMission) //TESTABLE
                {
                    TriggerGoal(goal);
                }
            }

            Console.WriteLine($"Campaign Text: [{campaignText}]");
            Console.WriteLine($"Mission Text: [{missionText}]");
            Console.WriteLine($"Planet Text: [{planetText}]");
            File.WriteAllText("TTGProgress.txt", $"{campaignText}\n{missionText}\n{planetText}");
        }

        private static void TriggerGoal(Goal metGoal)
        {
            Console.WriteLine($"+++ WE HAVE MET THE {metGoal.timeframe} GOAL FOR {metGoal.mission}");
            var goalEvent = new Event();
            if (metGoal.timeframe == "planet")
            {
                goalEvent.name = "planetgoal" + metGoal.mission;
            } else if (metGoal.timeframe == "mission")
            {
                goalEvent.name = "missiongoal" + metGoal.mission;
            } else if (metGoal.timeframe == "campaign")
            {
                goalEvent.name = "solaritekerrigan";
            }
            var goalEventQueued = new QueuedEvent(goalEvent);
            while (metGoal.currentBank > metGoal.cost)
            {
                metGoal.currentBank -= metGoal.cost;
                goalEventQueued.queueCount += 1;
            }
            goalEventQueued.username = "Chat";

            eventQueue.Insert(0, goalEventQueued);
        }

        private static void ClearBankDEVONLY()
        {
            Console.WriteLine("DEV ONLY: Reset the bank for everything!");
            foreach (var g in goalsList)
            {
                g.currentBank = 0;
            }
            foreach (var e in eventDict.Values.ToList<Event>())
            {
                e.bank = 0;
                e.tier = 0;
            }
            ExportData();
            Environment.Exit(0);
        }

        private static void Dequeue()
        {
            if (eventQueue.Count > 0) {
                if (ReadXML(XML_LOCK) == "-1")
                {
                    var eventToSend = eventQueue.First<QueuedEvent>();
                    Console.WriteLine($"Attempting to dequeue {eventToSend.queueCount} instances of {eventToSend.baseEvent.name} triggered by {eventToSend.username}.");
                    eventQueue.RemoveAt(0);
                    PrintEventQueue();
                    if (eventToSend.baseEvent.name.Contains("missiongoal"))
                    {
                        var eventMission = eventToSend.baseEvent.name.Split("missiongoal")[1];
                        var currentMission = ReadXML(XML_MISSION);
                        if (eventMission.ToLower() != currentMission.ToLower())
                        {
                            Console.WriteLine("Blocked mission goal from " + eventMission + " since we are on " + currentMission + ".");
                            return;
                        } else
                        {
                            Console.WriteLine("Renaming it to missiongoal.");
                            eventToSend.baseEvent.name = "missiongoal";
                        }
                    }
                    if (eventToSend.baseEvent.name.ToLower() == "planetgoalphantoms of the void") {
                        WriteXML(XML_ARGS, GenHybrid(eventToSend.queueCount));
                    }
                    if (eventToSend.baseEvent.name.Contains("planetgoal"))
                    {
                        eventToSend.baseEvent.name = "planetgoal";
                    }
                    WriteXML(XML_EVENT, eventToSend.baseEvent.name);
                    WriteXML(XML_VALUE, eventToSend.queueCount.ToString());
                    WriteXML(XML_USERNAME, eventToSend.username);
                    if (eventToSend.baseEvent.name.ToLower() == "infinitecycle")
                    {
                        WriteXML(XML_ARGS, GenHybrid(eventToSend.queueCount));
                    }
                    WriteXML(XML_LOCK, "1");
                }
            }
        }

        public static string GenHybrid(int count)
        {
            var hybridoutstr = "";
            for (var j = 0; j < count; j++)
            {
                var index = random.Next(hybridstr.Length);
                hybridoutstr += hybridstr[index] + " ";
                hybridstr = hybridstr.Remove(index, 1);
                if (hybridstr.Length <= 5)
                {
                    hybridstr += "0000000000000000000000000000000000000000111111111111111111111111111111111111111122222222444444443333";
                }
            }
            Console.WriteLine("We're sending off " + hybridoutstr + " to ruin Grant's day!");
            return hybridoutstr;
        }

        public static Event ProcessCommand(TwitchBot.tcm tcm)
        {
            Event minEvent;
            var levenMin = 99;
            foreach (var command in eventDict.Keys)
            {
                if (tcm.Message.ToLower().Contains(command.ToLower()))
                {
                    return eventDict[command];
                }

                //Apply Levenstraum distance for each word.
                /*foreach (string word in tcm.Message.Split())
                {

                }*//*
            }

            /*foreach (string command in eventDict.Keys)
            {
                int ld = levenshtein(command, event
            }*//*


            Console.WriteLine("Command processor found no valid event!");
            return null;
        }

        public static int ProcessCheer(TwitchBot.tcm tcm)
        {
            var cheerSum = 0;
            Console.WriteLine("PC TCM: " + tcm.Message);
            foreach (var word in tcm.Message.ToLower().Split())
            {
                Console.WriteLine("word: " + word);
                if (word.ToLower().StartsWith("cheer") && word.Length > 5)
                {
                    var cheerValue = 0;
                    var success = int.TryParse(word.Substring(5, word.Length - 5), out cheerValue);
                    Console.WriteLine("CV: " + cheerValue);
                    if (success && cheerValue > 0)
                    {
                        cheerSum += cheerValue;
                        Console.WriteLine($"Added {cheerValue} to the cheer total of {cheerSum}.");
                    } else
                    {
                        Console.WriteLine($"Cheer detected without value (in a word) for {word}.");
                    }
                }
            }
            return cheerSum;
        }

        static void PrintEventQueue()
        {
            Console.WriteLine("Queue is currently:\n[");
            foreach (var qe in eventQueue)
            {
                Console.WriteLine($"Event: {qe.baseEvent.name} / Current Bank: {qe.baseEvent.bank} / Total Queue Count: {qe.queueCount}");
            }
            Console.WriteLine("]");
            Console.WriteLine("-------------------------------");
        }

        static void SetupData()
        {
            ImportEvents();
            ImportGoals();
            ImportTextData();
        }

        static void ExportData()
        {
            ExportEvents();
            ExportGoals();
        }

        private static string getPlanetFromMission(string mission)
        {
            switch (mission.ToLower().Replace(" ", ""))
            {
                case "labrat":
                    return "DominionLab";
                case "backinthesaddle":
                    return "DominionLab";
                case "rendezvous":
                    return "DominionLab";
                case "harvestofscreams":
                    return "Expedition";
                case "shootthemessenger":
                    return "Expedition";
                case "enemywithin":
                    return "Expedition";
                case "domination":
                    return "Char";
                case "fireinthesky":
                    return "Char";
                case "oldsoldiers":
                    return "Char";
                case "wakingtheancient":
                    return "Zerus";
                case "thecrucible":
                    return "Zerus";
                case "supreme":
                    return "Zerus";
                case "infested":
                    return "Hybrid";
                case "handofdarkness":
                    return "Hybrid";
                case "phantomsofthevoid":
                    return "Hybrid";
                case "withfriendslikethese...":
                    return "ZSpace1";
                case "conviction":
                    return "ZSpace1";
                case "planetfall":
                    return "Korhal";
                case "deathfromabove":
                    return "Korhal";
                case "thereckoning":
                    return "Korhal";
                default:
                    return "ERROR";
            }
        }

        static void WriteXML(string location, string value) //This does not write XML and we need to adjust the path.
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(BattleNetBank);
                XmlAttribute nodeAttribute;
                switch (location)
                {
                    case XML_EVENT:
                        nodeAttribute = doc.DocumentElement.SelectSingleNode(XML_EVENT).Attributes["string"];
                        nodeAttribute.InnerText = value;
                        break;
                    case XML_VALUE:
                        nodeAttribute = doc.DocumentElement.SelectSingleNode(XML_VALUE).Attributes["int"];
                        nodeAttribute.InnerText = value;
                        break;
                    case XML_LOCK:
                        nodeAttribute = doc.DocumentElement.SelectSingleNode(XML_LOCK).Attributes["int"];
                        nodeAttribute.InnerText = value;
                        break;
                    case XML_USERNAME:
                        nodeAttribute = doc.DocumentElement.SelectSingleNode(XML_USERNAME).Attributes["string"];
                        nodeAttribute.InnerText = value;
                        break;
                    case XML_PLANET:
                        nodeAttribute = doc.DocumentElement.SelectSingleNode(XML_PLANET).Attributes["string"];
                        nodeAttribute.InnerText = value;
                        break;
                    case XML_MISSION:
                        nodeAttribute = doc.DocumentElement.SelectSingleNode(XML_MISSION).Attributes["text"];
                        nodeAttribute.InnerText = value;
                        break;
                    case XML_ARGS:
                        nodeAttribute = doc.DocumentElement.SelectSingleNode(XML_ARGS).Attributes["string"];
                        nodeAttribute.InnerText = value;
                        break;
                    default:
                        Console.WriteLine("Unidentified XML Node, not writing anything!");
                        break;
                }
                doc.Save(BattleNetBank);
            } catch (Exception e)
            {
                Console.WriteLine("Could not write XML to file! (file in use) " + e.ToString());
                return;
            }
        }

        static string ReadXML(string location)
        {
            var doc = new XmlDocument();
            doc.Load(BattleNetBank);
            XmlAttribute nodeAttribute;
            switch (location)
            {
                case XML_LOCK:
                    nodeAttribute = doc.DocumentElement.SelectSingleNode(XML_LOCK).Attributes["int"];
                    return nodeAttribute.Value;
                case XML_MISSION:
                    nodeAttribute = doc.DocumentElement.SelectSingleNode(XML_MISSION).Attributes["text"];
                    return nodeAttribute.Value;
                case XML_PLANET:
                    nodeAttribute = doc.DocumentElement.SelectSingleNode(XML_PLANET).Attributes["string"];
                    return nodeAttribute.Value;
                default:
                    return null;
            }
        }

        static void ImportEvents()
        {
            var lines = File.ReadAllText(@"EventsList.json", Encoding.UTF8);
            dynamic jsonData = JsonConvert.DeserializeObject(lines);
            foreach (JObject ttgEventString in jsonData)
            {
                var ttgEvent = new Event(ttgEventString);
                eventDict.Add(ttgEvent.name, ttgEvent);
            }
        }

        static void ImportGoals()
        {
            var lines = File.ReadAllText(@"GoalsList.json", Encoding.UTF8);
            dynamic jsonData = JsonConvert.DeserializeObject(lines);
            foreach (JObject ttgGoalString in jsonData)
            {
                var ttgGoal = new Goal(ttgGoalString);
                goalsList.Add(ttgGoal);
            }
        }

        static void ExportEvents()
        {
            var json = JsonConvert.SerializeObject(eventDict.Values.ToList());
            System.IO.File.WriteAllText(@"EventsList.json", json);
        }

        static void ExportGoals()
        {
            var json = JsonConvert.SerializeObject(goalsList);
            System.IO.File.WriteAllText(@"GoalsList.json", json);
        }

        static void ImportTextData()
        {
            //Do we have any? lol
        }

        public static void LogEvent(TwitchBot.tcm tcm, Event calledEvent)
        {
            var localDate = DateTime.Now;
            using (var w = File.AppendText("EventLog.txt"))
            {
                w.WriteLine($"[{localDate}] {tcm.Sender} activated {calledEvent.name}.");
            }
        }

        public static void LogPay(TwitchBot.tcm tcm, int cheerSum)
        {
            var localDate = DateTime.Now;
            using (var w = File.AppendText("CheerLog.txt"))
            {
                w.WriteLine($"[{localDate}] {tcm.Sender} cheered {cheerSum} bits.");
            }
        }

        private static int levenshtein(string a, string b)
        {

            if (string.IsNullOrEmpty(a))
            {
                if (!string.IsNullOrEmpty(b))
                {
                    return b.Length;
                }
                return 0;
            }

            if (string.IsNullOrEmpty(b))
            {
                if (!string.IsNullOrEmpty(a))
                {
                    return a.Length;
                }
                return 0;
            }

            int cost;
            var d = new int[a.Length + 1, b.Length + 1];
            int min1;
            int min2;
            int min3;

            for (var i = 0; i <= d.GetUpperBound(0); i += 1)
            {
                d[i, 0] = i;
            }

            for (var i = 0; i <= d.GetUpperBound(1); i += 1)
            {
                d[0, i] = i;
            }

            for (var i = 1; i <= d.GetUpperBound(0); i += 1)
            {
                for (var j = 1; j <= d.GetUpperBound(1); j += 1)
                {
                    cost = (a[i - 1] != b[j - 1]) ? 1 : 0;

                    min1 = d[i - 1, j] + 1;
                    min2 = d[i, j - 1] + 1;
                    min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }

            return d[d.GetUpperBound(0), d.GetUpperBound(1)];

        }
    }
}*/
