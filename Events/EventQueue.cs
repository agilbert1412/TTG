using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TTGHotS.Discord;

namespace TTGHotS.Events
{
    internal class EventQueue : IEnumerable<QueuedEvent>
    {
        private readonly IBotCommunicator _communications;
        public List<QueuedEvent> _eventQueue;
        public static List<string> _soaQueue;
        private bool _globalPause;

        public EventQueue(IBotCommunicator discord)
        {
            _communications = discord;
            _eventQueue = new List<QueuedEvent>();
            _soaQueue = new List<string>();
            _globalPause = false;
        }

        public bool IsEmpty => !_eventQueue.Any();

        public QueuedEvent First => _eventQueue.First();

        public void ImportFrom(string eventsQueueFile, EventCollection allEvents)
        {
            var lines = File.ReadAllText(eventsQueueFile, Encoding.UTF8);
            dynamic jsonData = JsonConvert.DeserializeObject(lines);
            foreach (JObject ttgEventString in jsonData)
            {
                var ttgEvent = new QueuedEvent(ttgEventString, allEvents);
                _eventQueue.Add(ttgEvent);
            }
        }

        public void ExportTo(string eventsQueueFile)
        {
            var json = JsonConvert.SerializeObject(_eventQueue, Formatting.Indented);
            File.WriteAllText(eventsQueueFile, json);
        }

        public void PushAtBeginning(QueuedEvent eventQueued)
        {
            _eventQueue.Insert(0, eventQueued);
        }

        public void QueueEvent(QueuedEvent eventQueued)
        {
            _eventQueue.Add(eventQueued);
        }

        public void RemoveFirst()
        {
            _eventQueue.RemoveAt(0);
        }

        public void PrintToConsole()
        {
            Console.WriteLine("Queue is currently:\n[");
            Console.WriteLine(GetPrintableQueue());
            Console.WriteLine("]");
            Console.WriteLine("-------------------------------");
        }

        public void PrintToDiscord(SocketUserMessage messageToReplyTo = null)
        {
            var message = GetDiscordMessage();
            _communications.ReplyTo(messageToReplyTo, message);
        }

        public void PrintToDiscord(ulong channelId)
        {
            var message = GetDiscordMessage();
            _communications.SendMessage(channelId, message);
        }

        private string GetDiscordMessage()
        {
            var message = "Queue is currently:\n```";
            message += GetPrintableQueue();
            message += "\n```";
            return message;
        }

        private string GetPrintableQueue()
        {
            var printedQueue = "";
            foreach (var queuedEvent in _eventQueue)
            {
                printedQueue += (GetPrintableEvent(queuedEvent)) + Environment.NewLine;
            }

            return printedQueue;
        }

        private string GetPrintableEvent(QueuedEvent eventToPrint)
        {
            return
                $"Event: {eventToPrint.baseEventName} \t| Current Bank: {eventToPrint.BaseEvent.GetBank()} \t| Total Queue Count: {eventToPrint.queueCount}";
        }

        public IEnumerator<QueuedEvent> GetEnumerator()
        {
            return _eventQueue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool IsPaused()
        {
            return _globalPause;
        }

        public void Pause()
        {
            _globalPause = true;
        }

        public void Unpause()
        {
            _globalPause = false;
        }

        public List<string> GetSoaQueue()
        {
            return _soaQueue;
        }

        public void AddSpearOfAdun(string soaEvent)
        {
            _soaQueue.Add(soaEvent);
        }
    }
}
