namespace Peanits;

using Discord;
using Discord.WebSocket;

public static class DiscordUtils
{
	public static async Task<GuildEmote> GetUserAvatarAsEmote(IUser user, ulong guildId)
	{
		SocketGuild guild = Bot.Client.GetGuild(guildId);
		foreach(GuildEmote? emote in guild.Emotes)
		{
			if (emote.Name == user.GlobalName)
				return emote;
		}

		string avatarUrl = user.GetAvatarUrl(ImageFormat.Png);
		using HttpClient client = new();
		using HttpResponseMessage response = await client.GetAsync(avatarUrl);
		Image img = new(response.Content.ReadAsStream());
		return await guild.CreateEmoteAsync(user.GlobalName, img);
	}
}
