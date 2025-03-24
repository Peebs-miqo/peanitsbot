using System.Runtime.CompilerServices;
using Discord.WebSocket;

DiscordSocketClient Client = new();
await Client.LoginAsync(Discord.TokenType.Bot, "MTM1MzY5NDQwNDY2OTQ3Mjc2OQ.GecRFn.LObPDc9sh7drq51TzPVPnPpBM_pjoTgrrUYzz0");
await Client.StartAsync();
Client.Ready += ()=>
{
	Console.WriteLine("Discord Ready");
	return Task.CompletedTask;
};
Client.MessageReceived+=(SocketMessage message)=>
{
	if (message.Content=="&Scheduler")
	{
		message.Channel.SendMessageAsync("Hello");
	}
	return Task.CompletedTask;
};

Console.ReadLine();