using System.Runtime.CompilerServices;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

DiscordSocketConfig cfg = new();
cfg.GatewayIntents |= Discord.GatewayIntents.MessageContent;
DiscordSocketClient Client = new(cfg);
await Client.LoginAsync(Discord.TokenType.Bot, "MTM1MzY5NDQwNDY2OTQ3Mjc2OQ.GQSqgt.ZYF58sU0L0K6G1OEaDhppzcgHPhXsNtKpijd78");
await Client.StartAsync();

Client.Ready += ()=>
{
	Console.WriteLine("Discord Ready");
	return Task.CompletedTask;
};

Client.MessageReceived += async (SocketMessage message) =>
{
	if (message.Content=="&Scheduler")
	{
		PollMediaProperties mon = new();
		mon.Text = "Monday";

		PollMediaProperties tue = new();
		tue.Text = "Tuesday";

		PollMediaProperties wed = new();
		wed.Text = "Wednesday";

		PollMediaProperties thu = new();
		thu.Text = "Thursday";

		PollMediaProperties fri = new();
		fri.Text = "Friday";

		PollMediaProperties sat = new();
		sat.Text = "Saturday";

		PollMediaProperties sun = new();
		sun.Text = "Sunday";

		PollProperties poll = new PollProperties();
		poll.Question = new();
		poll.Question.Text = "What days are you available?";
		poll.Duration = 7 * 24;
		poll.Answers = [mon, tue, wed, thu, fri, sat, sun];
		poll.AllowMultiselect = true;
		poll.LayoutType = PollLayout.Default;
	
		await message.Channel.SendMessageAsync(poll: poll);
	}
};

Client.PollVoteAdded += async (
	Cacheable<IUser, ulong> user,
	Cacheable<ISocketMessageChannel, IRestMessageChannel, IMessageChannel, ulong> channel,
	Cacheable<IUserMessage, ulong> messageCache,
	Cacheable<SocketGuild, RestGuild, IGuild, ulong>? guild,
	ulong id) =>
{
	IUserMessage message = await messageCache.GetOrDownloadAsync();
	if (message == null)
	{
		Console.WriteLine($"No Message? {messageCache.Id}");
		return;
	}

	if (message.Author.Id != Client.CurrentUser.Id)
		return;

	if (message.Poll == null
		|| message.Poll.Value.Results == null)
		return;

	foreach(PollAnswerCounts count in message.Poll.Value.Results.Value.AnswerCounts)
	{
		Console.WriteLine($"{count.AnswerId} - {count.Count}");
	}
};

Console.WriteLine("Press return to exit");
Console.ReadLine();