
namespace Peanits;

using System.Diagnostics;
using Discord;
using Discord.Interactions;

public class ManagementCommandModule()
	: InteractionModuleBase<SocketInteractionContext>
{
	public enum Operations
	{
		Update,
		Shutdown,
	}

	[DefaultMemberPermissions(GuildPermission.Administrator)]
	[SlashCommand("manage", "Manage the bot process")]
	public async Task OnlineUpdate(Operations operation)
	{
		await this.RespondAsync($"{operation} in progress.", ephemeral: true);

		try
		{
			Task t = this.Run(operation);
		}
		catch (Exception ex)
		{
			await this.ModifyOriginalResponseAsync(op =>
			{
				op.Content = $"Error: {ex.Message}";
			});
		}
	}

	private Task Run(Operations operation)
	{
		switch (operation)
		{
			case Operations.Update: return this.Update();
			case Operations.Shutdown: return this.Shutdown();
		}

		throw new NotImplementedException();
	}

	private Task Update()
	{
		ProcessStartInfo startInfo = new();
		startInfo.FileName = "bash";
		startInfo.Arguments = $"Scripts/Update.sh";
		startInfo.UseShellExecute = true;

		Process process = new();
		process.StartInfo = startInfo;
		process.Start();

		Program.ShutdownRequested = true;

		return Task.CompletedTask;
	}

	private Task Shutdown()
	{
		Program.ShutdownRequested = true;
		return Task.CompletedTask;
	}
}