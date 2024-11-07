using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace TTGHotS.Discord
{
    internal class DiscordBot
    {
        // private const string TOKEN_FILE = @"UserSpecificFiles\token_referencer.txt";
        private const string TOKEN_FILE = @"UserSpecificFiles\token_ttg.txt";

        private IBotCommunicator _discord;
        private TTGModule _ttgModule;

        public DiscordBot()
        {
            _discord = new DiscordWrapper();
            _ttgModule = new TTGModule(_discord);
        }

        public async Task InitializeAsync()
        {
            if (!File.Exists(TOKEN_FILE))
            {
                throw new FileNotFoundException(@$"Could not find file {TOKEN_FILE}. This file needs to exist, and contain the secret token to connect as your Discord bot");
            }


            _discord.InitializeClient();
            _discord.InitializeLog();

            //  You can assign your bot token to a string, and pass that in to connect.
            //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
            // var token = "token";

            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            var token = File.ReadAllText(TOKEN_FILE);
            // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;
            _discord.Login(token);
            _discord.Start(HandleCommandAsync);
            await _ttgModule.InitializeAsync();

            // Block this task until the program is closed.
            await Task.Delay(Timeout.Infinite);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            // Bail out if it's a System Message.
            if (!(arg is SocketUserMessage msg))
            {
                return;
            }

            // We don't want the bot to respond to itself
            if (msg.Author.Id == _discord.MyId)
            {
                return;
            }

            await _ttgModule.ExecuteTTGCommand(msg);

            // Create a number to track where the prefix ends and the command begins
            var pos = 0;
            // Replace the '!' with whatever character
            // you want to prefix your commands with.
            // Uncomment the second half if you also want
            // commands to be invoked by mentioning the bot instead.
            if (msg.HasCharPrefix('!', ref pos) /* || msg.HasMentionPrefix(_client.CurrentUser, ref pos) */)
            {
                if (msg.Content.StartsWith("!status "))
                {
                    var parts = msg.Content.Split(" ");
                    var activityType = (ActivityType)int.Parse(parts[1]);
                    var text = string.Join(" ", parts.Skip(2));

                    _discord.SetStatusMessage(text, activityType);
                }

                // Create a Command Context.
                //var context = new SocketCommandContext(_client, msg);

                // Execute the command. (result does not indicate a return value, 
                // rather an object stating if the command executed successfully).
                //var result = await _commands.ExecuteAsync(context, pos, _services);

                // Uncomment the following lines if you want the bot
                // to send a message if it failed.
                // This does not catch errors from commands with 'RunMode.Async',
                // subscribe a handler for '_commands.CommandExecuted' to see those.
                // if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                //    await msg.Channel.SendMessageAsync(result.ErrorReason);
            }
        }
    }
}
