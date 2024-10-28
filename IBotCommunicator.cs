using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace TTGHotS
{
    internal interface IBotCommunicator
    {
        ulong MyId { get; }
        void InitializeClient();
        void InitializeLog();
        void Login(string token);
        void Start(Func<SocketMessage, Task> messageReceivedFunction);
        Task SendMessage(ulong channelId, string text);
        Task SendMessage(ISocketMessageChannel channel, string text);
        void ReplyTo(SocketUserMessage message, string text);
        string GetQualifiedName(ulong userId);
        ulong GetUserId(string username, string discriminator);
        void SetStatusMessage(string statusText, ActivityType activity = ActivityType.Playing);
        void DeleteMessage(IMessage message);
        void DeleteAllMessagesInChannel(ulong channelId);
        void DeleteAllMessagesInChannel(ISocketMessageChannel channel);
    }
}