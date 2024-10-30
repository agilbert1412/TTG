using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TTGHotS.Discord;
using System.Net.Http;
using System.Threading.Tasks;

namespace TTGHotS.Events
{
    internal class Goals
    {
        private readonly IBotCommunicator _communications;
        private readonly XmlHandler _xml;
        private readonly ulong _eventsChannelId;
        private List<Goal> _goalsList;

        public Goals(IBotCommunicator discord, XmlHandler xml, ulong eventsChannelId)
        {
            _communications = discord;
            _xml = xml;
            _goalsList = new List<Goal>();
            _eventsChannelId = eventsChannelId;
        }

        public int Count => _goalsList.Count;

        public List<Goal> ToList()
        {
            return _goalsList;
        }

        public void ImportFrom(string goalsFile)
        {
            var lines = File.ReadAllText(goalsFile, Encoding.UTF8);
            dynamic jsonData = JsonConvert.DeserializeObject(lines);
            foreach (JObject ttgGoalString in jsonData)
            {
                var ttgGoal = new Goal(ttgGoalString);
                _goalsList.Add(ttgGoal);
            }
        }

        public void ExportTo(string goalsFile)
        {
            var json = JsonConvert.SerializeObject(_goalsList, Formatting.Indented);
            File.WriteAllText(goalsFile, json);
        }

        public void ClearAllBanks()
        {
            foreach (var g in _goalsList)
            {
                g.currentBank = 0;
                g.displayBank = 0;
            }
        }

        public async Task AssignCreditsToGoal(int creditsAmount, EventQueue eventQueue)
        {
            var currentMission = _xml.GetCurrentMission(Format.AsIs);
            var currentPlanet = _xml.GetCurrentPlanet(Format.AsIs);
            var missionLower = currentMission.ToLower();
            var planetLower = currentPlanet.ToLower();
            var planetValue = 0;
            var missionValue = 0;
            var campaignValue = 0;
            var planetGoal = 0;
            var missionGoal = 0;
            var campaignGoal = 0;
            var missionRepeat = -1;
            var planetRepeat = -1;
            Console.WriteLine($"Attempting to assign {creditsAmount} credits to goals.  Found Mission: {currentMission} on planet {currentPlanet}.");
            foreach (var goal in _goalsList)
            {
                if (goal.timeframe == "mission")
                {
                    if (goal.mission.ToLower() == missionLower)
                    {
                        goal.AddCredits(creditsAmount);
                        Console.WriteLine($"Added {creditsAmount} ({goal.currentBank}/{goal.cost}) to the mission goal for {goal.mission}.");
                        missionValue = goal.displayBank;
                        missionGoal = goal.cost;
                        missionRepeat = goal.repeatCount;
                    }
                }
                if (goal.timeframe == "planet")
                {
                    if (goal.planet.ToLower() == planetLower)
                    {
                        goal.AddCredits(creditsAmount);
                        Console.WriteLine($"Added {creditsAmount} ({goal.currentBank}/{goal.cost}) to the planet goal for {goal.mission}.");
                        planetValue = goal.displayBank;
                        planetGoal = goal.cost;
                        planetRepeat = goal.repeatCount;
                    }
                }
                if (goal.timeframe == "campaign")
                {
                    goal.AddCredits(creditsAmount);
                    campaignValue = goal.displayBank;
                    campaignGoal = goal.cost;
                }

                Console.WriteLine($"Comparing {goal.currentBank} to {goal.cost} for {goal.mission} and {currentMission}.");
                if (goal.currentBank >= goal.cost && goal.mission.ToLower() == missionLower) //TESTABLE
                {
                    TriggerGoal(goal, eventQueue);
                }
            }

            Console.WriteLine("Writing values to server.");
            var progressValues = new Dictionary<string, string>
            {
                { "planet", planetValue.ToString() },
                { "planetName", currentPlanet },
                { "mission", missionValue.ToString() },
                { "missionName", currentMission },
                { "campaign", campaignValue.ToString() },
                { "missionrepeat", missionRepeat.ToString() },
                { "planetrepeat", planetRepeat.ToString() }
            };
            var response = await SendToServer("values", progressValues);
            Console.WriteLine($"Server response from Values/Set: {response}");

            var goalValues = new Dictionary<string, string>
            {
                { "planet", planetGoal.ToString() },
                { "mission", missionGoal.ToString() },
                { "campaign", campaignGoal.ToString() }
            };
            var response2 = await SendToServer("goals", goalValues);
            Console.WriteLine($"Server response from Goals/Set: {response2}");

            /*Console.WriteLine($"Mission Text: [{missionGoalProgressText}]");
            Console.WriteLine($"Planet Text: [{planetGoalProgressText}]");
            Console.WriteLine($"Campaign Text: [{campaignGoalProgresstext}]");
            File.WriteAllText("TTGProgress.txt", $"{MakeTitleCase(currentMission)}\n{missionGoalProgressText}\n{MakeTitleCase(currentPlanet)}\n{planetGoalProgressText}\n{campaignGoalProgresstext}");*/
        }

        public static async Task<string> SendToServer(string endpoint, Dictionary<string, string> data)
        {
            try
            {
                var client = new HttpClient();
                var content = new FormUrlEncodedContent(data);
                var res2 = await client.PostAsync($"http://localhost:3000/{endpoint}/set", content);
                var responseString = await res2.Content.ReadAsStringAsync();
                return responseString;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        private async void TriggerGoal(Goal metGoal, EventQueue eventQueue)
        {
            Console.WriteLine($"+++ WE HAVE MET THE {metGoal.timeframe} GOAL FOR {metGoal.mission}");
            var goalEvent = new Event();
            if (metGoal.timeframe == "planet")
            {
                goalEvent.name = "planetgoal" + metGoal.mission;
            }
            else if (metGoal.timeframe == "mission")
            {
                goalEvent.name = "missiongoal" + metGoal.mission;
            }
            else if (metGoal.timeframe == "campaign")
            {
                goalEvent.name = "starcraftendgame";
            }

            var goalEventQueued = new QueuedEvent(goalEvent);
            while (metGoal.currentBank >= metGoal.cost)
            {
                if (metGoal.repeatCount >= 0)
                {
                    metGoal.repeatCount++;
                }

                if (metGoal.type != "campaign")
                {
                    metGoal.currentBank -= metGoal.cost;
                }

                goalEventQueued.queueCount += 1;
            }

            goalEventQueued.username = "Chat";

            eventQueue.PushAtBeginning(goalEventQueued);

            _communications.SendMessage(_eventsChannelId, $"We have met the {metGoal.timeframe} goal for {metGoal.mission}!");
        }

        public Goal GetGoal(string currentMission)
        {
            return _goalsList.FirstOrDefault(x => x.mission.Equals(currentMission, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
