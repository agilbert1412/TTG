using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TTGHotS.Twitch
{
    public class TwitchBot
    {
        const string ip = "irc.chat.twitch.tv";
        const int port = 6667;

        private string nick;
        private string password;
        private StreamReader streamReader;
        private StreamWriter streamWriter;
        private TaskCompletionSource<int> connected = new TaskCompletionSource<int>();

        public event TwitchChatEventHandler OnMessage = delegate { };
        public delegate void TwitchChatEventHandler(object sender, tcm e);

        public class tcm : EventArgs
        {
            public string Sender { get; set; }
            public string Message { get; set; }
            public string Channel { get; set; }
        }

        public TwitchBot(string nick, string password)
        {
            this.nick = nick;
            this.password = password;
        }

        public async Task Start()
        {
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ip, port);
            streamReader = new StreamReader(tcpClient.GetStream());
            streamWriter = new StreamWriter(tcpClient.GetStream()) { NewLine = "\r\n", AutoFlush = true };

            await streamWriter.WriteLineAsync($"CAP REQ :twitch.tv/commands twitch.tv/tags");
            await streamWriter.WriteLineAsync($"PASS {password}");
            await streamWriter.WriteLineAsync($"NICK {nick}");
            connected.SetResult(0);

            while (true)
            {
                try
                {
                    var line = await streamReader.ReadLineAsync();
                    var usedTwitchPrime = false;
                    var tpUsername = "";

                    if (line == null)
                    {
                        continue;
                    }
                    Console.WriteLine("TM: " + line);
                    if (line.Contains("msg-param-sub-plan=Prime"))
                    {
                        tpUsername = line.Split("login=")[1].Split(";")[0];
                        usedTwitchPrime = true;
                        line = line.Replace("USERNOTICE", "PRIVMSG");
                        Console.WriteLine("I found a prime!");
                        Console.WriteLine(line);
                    }
                    if (line.StartsWith("@badge-info="))
                    {
                        //line = "@badge-info=;badges=premium/1;color=#0000FF;display-name=highwolf_x;emotes=;flags=;id=c447617a-6c5d-4f93-92a3-7100eb6ec340;login=highwolf_x;mod=0;msg-id=resub;msg-param-cumulative-months=3;msg-param-months=0;msg-param-multimonth-duration=0;msg-param-multimonth-tenure=0;msg-param-should-share-streak=1;msg-param-streak-months=2;msg-param-sub-plan-name=Channel Subscription (gigguk);msg-param-sub-plan=Prime;msg-param-was-gifted=false;room-id=24411833;subscriber=1;system-msg=highwolf_x subscribed with Prime. They've subscribed for 3 months, currently on a 2 month streak!;tmi-sent-ts=1662784272393;user-id=39016146;user-type=absolute_chad:tmi.twitch.tv USERNOTICE #gigguk :Oh boy, my fav Genshin streamer doing an anime tier list?";
                        var templine = line.Split(";user-type=")[1];
                        var firstcolonindex = templine.IndexOf(":");
                        line = templine.Substring(firstcolonindex, templine.Length - firstcolonindex);
                        Console.WriteLine("Fixed line to be: " + line);
                    }
                    if (line == ":tmi.twitch.tv PRIVMSG #7thace" || line == ":tmi.twitch.tv PRIVMSG #giantgrantgames") //HOLY FUCK FIX ME
                    {
                        Console.WriteLine("I found a prime but it didn't have a message");
                        continue;
                    }
                    if (usedTwitchPrime)
                    {
                        line = line.Replace(":tmi.twitch.tv", $":{tpUsername}!{tpUsername}@{tpUsername}.tmi.twitch.tv");
                    }
                    var split = line.Split(" ");
                    //PING :tmi.twitch.tv
                    //Respond with PONG :tmi.twitch.tv
                    if (line.StartsWith("PING"))
                    {
                        Console.WriteLine("PONG");
                        await streamWriter.WriteLineAsync($"PONG {split[1]}");
                    }

                    if (split.Length > 2 && split[1] == "PRIVMSG")
                    {
                        //:7thace!7thace@7thace.tmi.twitch.tv PRIVMSG #7thace :chair500 crystals
                        //@badge-info=subscriber/62;badges=broadcaster/1,subscriber/3000;client-nonce=61585088de628399bf6088f8353fd255;color=#12AF12;display-name=7thAce;emotes=;first-msg=0;flags=;id=b1f1dd3f-ff9e-48c4-bd7a-9ccfc46ac4f8;mod=0;returning-chatter=0;room-id=13485725;subscriber=1;tmi-sent-ts=1662783495276;turbo=0;user-id=13485725;user-type= :7thace!7thace@7thace.tmi.twitch.tv PRIVMSG #7thace :chair500 crystals
                        //:mytwitchchannel!mytwitchchannel@mytwitchchannel.tmi.twitch.tv 
                        // ^^^^^^^^
                        //Grab this name here
                        var exclamationPointPosition = split[0].IndexOf("!");
                        var username = split[0].Substring(1, exclamationPointPosition - 1);
                        //Skip the first character, the first colon, then find the next colon
                        var secondColonPosition = line.IndexOf(':', 1);//the 1 here is what skips the first character
                        var message = line.Substring(secondColonPosition + 1);//Everything past the second colon
                        Console.WriteLine(message);
                        if (usedTwitchPrime)
                        {
                            message = "cheer250 " + message;
                            Console.WriteLine("I HAVE OVERRIDDEN THIS MESSAGE TO CHEER250 FOR PRIME");
                        }
                        var channel = split[2].TrimStart('#');

                        OnMessage(this, new tcm
                        {
                            Message = message,
                            Sender = username,
                            Channel = channel
                        });
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("EXCEPTION RECORDED: " + e.ToString());
                    continue;
                }
            }
        }

        public async Task SendMessage(string channel, string message)
        {
            await connected.Task;
            await streamWriter.WriteLineAsync($"PRIVMSG #{channel} :{message}");
        }

        public async Task JoinChannel(string channel)
        {
            await connected.Task;
            await streamWriter.WriteLineAsync($"JOIN #{channel}");
        }
    }
}