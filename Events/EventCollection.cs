using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TTGHotS.Discord;

namespace TTGHotS.Events
{
    internal class EventCollection
    {
        private readonly IBotCommunicator _communications;
        public Dictionary<string, Event> _events;

        public double CurrentMultiplier { get; set; }

        public EventCollection(IBotCommunicator discord)
        {
            _communications = discord;
            _events = new Dictionary<string, Event>();
            CurrentMultiplier = 1;
        }

        public int Count => _events.Count;

        public void ImportFrom(string eventsFile)
        {
            var lines = File.ReadAllText(eventsFile, Encoding.UTF8);
            dynamic jsonData = JsonConvert.DeserializeObject(lines);
            foreach (JObject ttgEventString in jsonData)
            {
                var ttgEvent = new Event(ttgEventString);
                this.Add(ttgEvent.name, ttgEvent);
            }
        }

        public void ExportTo(string eventsFile)
        {
            var json = JsonConvert.SerializeObject(this.ToList());
            File.WriteAllText(eventsFile, json);
        }

        public List<Event> ToList()
        {
            return _events.Values.ToList();
        }

        public Event GetEvent(string eventName)
        {
            foreach (var eventKey in _events.Keys)
            {
                if (eventKey.Equals(eventName, StringComparison.OrdinalIgnoreCase))
                {
                    return _events[eventKey];
                }
            }

            Console.WriteLine($"Found no event by the name {eventName}");
            return null;
        }

        private void Add(string eventKey, Event eventToAdd)
        {
            _events.Add(eventKey, eventToAdd);
        }

        public void ClearAllBanks()
        {
            foreach (var e in ToList())
            {
                e.SetBank(0);
                e.tier = 0;
            }
        }
    }
}
