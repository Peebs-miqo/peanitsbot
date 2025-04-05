namespace Peanits;

using System.Diagnostics;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

public class Bot
{
	public static DiscordSocketClient Client;
	protected InteractionService Interactions;

	public Bot()
	{
		DiscordSocketConfig cfg = new();
		cfg.GatewayIntents |= Discord.GatewayIntents.MessageContent;

		Client = new(cfg);
		this.Interactions = new(Client);

		Client.Log += this.OnClientLog;
		Client.InteractionCreated += this.OnClientInteractionCreated;
		Client.Ready += this.OnClientReady;
	}

	public async Task Start()
	{
		await Client.LoginAsync(Discord.TokenType.Bot, "MTM1MzY5NDQwNDY2OTQ3Mjc2OQ.GQSqgt.ZYF58sU0L0K6G1OEaDhppzcgHPhXsNtKpijd78");
		await Client.StartAsync();
	}

	private async Task OnClientReady()
	{
		await Interactions.AddModuleAsync<RaidPLannerModule>(null);

		await this.Interactions.RegisterCommandsGloballyAsync();
	}

	private Task OnClientInteractionCreated(SocketInteraction interaction)
	{
		SocketInteractionContext context = new(Client, interaction);
		return this.Interactions.ExecuteCommandAsync(context, null);
	}

	private Task OnClientLog(LogMessage arg)
	{
		Console.WriteLine(arg.Message);
		return Task.CompletedTask;
	}
}