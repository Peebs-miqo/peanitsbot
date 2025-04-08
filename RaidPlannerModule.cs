namespace Peanits;

using System.Text;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;

public class RaidPLannerModule
	: InteractionModuleBase<SocketInteractionContext>
{
	public static Schedule Schedule = new();

	[SlashCommand("poll", "Start a weekly planning")]
	public async Task Poll()
	{
		await this.RespondAsync("Done", ephemeral: true);
		RestUserMessage message = await this.Context.Channel.SendMessageAsync(null, components:Schedule.Build());
		Schedule.MessageId = message.Id;
		Schedule.ChannelId = message.Channel.Id;
	}

	[ComponentInteraction("poll-show-me")]
	public async Task OnShowMeCallback()
	{
		await this.Context.Interaction.RespondAsync(null, components:Schedule.BuildReply(this.Context.Interaction.User.Id), ephemeral:true);
	}

	[ComponentInteraction("vote-*")]
	public async Task OnPollCallback()
	{
		SocketMessageComponent? sm = this.Context.Interaction as SocketMessageComponent;
		if (sm == null)
			return;

		await Schedule.Vote(sm.Data.CustomId, this.Context.Interaction.User.Id);

		await sm.UpdateAsync(update =>
		{
			update.Components = Schedule.BuildReply(this.Context.Interaction.User.Id);
		});
	}
}

public class Schedule
{
	public ulong ChannelId;
	public ulong MessageId;

	public Day Mon = new();
	public Day Wed = new();
	public Day Thu = new();
	public Day Fri = new();

	public class Day
	{
		public HashSet<ulong> Votes700 = new();
		public HashSet<ulong> Votes830 = new();
		public HashSet<ulong> Votes100 = new();
	}

	public async Task UpdateSchedule()
	{
		IChannel? channel = await Bot.Client.GetChannelAsync(this.ChannelId);
		if (channel is SocketTextChannel textChannel)
		{

			StringBuilder builder = new();
			builder.AppendLine("In the stripped club, straight up Monking it.");
			builder.AppendLine("And by 'IT', haha well. lets justr say. My Peanits.");
			builder.AppendLine();

			Dictionary<ulong, int> voteCounts = this.GetVoteCounts();
			foreach((ulong userId, int count) in voteCounts)
			{
				IUser user = await channel.GetUserAsync(userId);

				builder.AppendLine($"{user.Username} is available for {count} timeslots");
			}

			IMessage? message = await textChannel.GetMessageAsync(this.MessageId);
			if (message is RestUserMessage userMessage)
			{
				await userMessage.ModifyAsync(update =>
				{
					update.Content = builder.ToString();
					update.Components = this.Build();
				});
			}
		}
	}

	private Dictionary<ulong, int> GetVoteCounts()
	{
		Dictionary<ulong, int> voteCounts = new();

		this.GetVoteCount(ref voteCounts, this.Mon);
		this.GetVoteCount(ref voteCounts, this.Wed);
		this.GetVoteCount(ref voteCounts, this.Thu);
		this.GetVoteCount(ref voteCounts, this.Fri);

		return voteCounts;
	}

	private void GetVoteCount(ref Dictionary<ulong, int> voteCounts, Day day)
	{
		this.GetVoteCount(ref voteCounts, day.Votes700);
		this.GetVoteCount(ref voteCounts, day.Votes830);
		this.GetVoteCount(ref voteCounts, day.Votes100);
	}

	private void GetVoteCount(ref Dictionary<ulong, int> voteCounts, HashSet<ulong> slots)
	{
		foreach(ulong user in slots)
		{
			if (!voteCounts.ContainsKey(user))
				voteCounts[user] = 0;

			voteCounts[user]++;
		}
	}

	public MessageComponent Build()
	{
		int totalVotes = 0;
		totalVotes += this.Mon.Votes700.Count;
		totalVotes += this.Mon.Votes830.Count;
		totalVotes += this.Mon.Votes100.Count;

		totalVotes += this.Wed.Votes700.Count;
		totalVotes += this.Wed.Votes830.Count;
		totalVotes += this.Wed.Votes100.Count;

		totalVotes += this.Thu.Votes700.Count;
		totalVotes += this.Thu.Votes830.Count;
		totalVotes += this.Thu.Votes100.Count;

		totalVotes += this.Fri.Votes700.Count;
		totalVotes += this.Fri.Votes830.Count;
		totalVotes += this.Fri.Votes100.Count;

		ComponentBuilder b = new();
		b.WithButton($"{totalVotes}", "poll-row-header", ButtonStyle.Secondary, disabled:true, row:0);
		b.WithButton("Mon", "poll-header-mon", ButtonStyle.Secondary, disabled:true, row:0);
		b.WithButton("Wed", "poll-header-wed", ButtonStyle.Secondary, disabled:true, row:0);
		b.WithButton("Thu", "poll-header-thu", ButtonStyle.Secondary, disabled:true, row:0);
		b.WithButton("Fri", "poll-header-fri", ButtonStyle.Secondary, disabled:true, row:0);

		b.WithButton("7:00", "poll-row-700", ButtonStyle.Secondary, disabled:true, row:1);
		b.WithButton("8:30", "poll-row-830", ButtonStyle.Secondary, disabled:true, row:2);
		b.WithButton("10:00", "poll-row-100", ButtonStyle.Secondary, disabled:true, row:3);

		b.WithButton($"{this.Mon.Votes700.Count}", "group-mon-700", this.Mon.Votes700.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:1, disabled:true);
		b.WithButton($"{this.Wed.Votes700.Count}", "group-wed-700", this.Wed.Votes700.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:1, disabled:true);
		b.WithButton($"{this.Thu.Votes700.Count}", "group-thu-700", this.Thu.Votes700.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:1, disabled:true);
		b.WithButton($"{this.Fri.Votes700.Count}", "group-fri-700", this.Fri.Votes700.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:1, disabled:true);

		b.WithButton($"{this.Mon.Votes830.Count}", "group-mon-830", this.Mon.Votes830.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);
		b.WithButton($"{this.Wed.Votes830.Count}", "group-wed-830", this.Wed.Votes830.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);
		b.WithButton($"{this.Thu.Votes830.Count}", "group-thu-830", this.Thu.Votes830.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);
		b.WithButton($"{this.Fri.Votes830.Count}", "group-fri-830", this.Fri.Votes830.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);

		b.WithButton($"{this.Mon.Votes100.Count}", "group-mon-100", this.Mon.Votes100.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);
		b.WithButton($"{this.Wed.Votes100.Count}", "group-wed-100", this.Wed.Votes100.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);
		b.WithButton($"{this.Thu.Votes100.Count}", "group-thu-100", this.Thu.Votes100.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);
		b.WithButton($"{this.Fri.Votes100.Count}", "group-fri-100", this.Fri.Votes100.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);

		b.WithButton($"Show my votes", "poll-show-me", ButtonStyle.Primary, row:3);

		return b.Build();
	}

	public MessageComponent BuildReply(ulong user)
	{
		ComponentBuilder b = new();

		b.WithButton(null, "poll-header-row", ButtonStyle.Secondary, disabled:true, row:0, emote: Emoji.Parse(":kiss:"));
		b.WithButton("Mon", "poll-header-mon", ButtonStyle.Secondary, disabled:true, row:0);
		b.WithButton("Wed", "poll-header-wed", ButtonStyle.Secondary, disabled:true, row:0);
		b.WithButton("Thu", "poll-header-thu", ButtonStyle.Secondary, disabled:true, row:0);
		b.WithButton("Fri", "poll-header-fri", ButtonStyle.Secondary, disabled:true, row:0);

		b.WithButton("7:00", "poll-row-700", ButtonStyle.Secondary, disabled:true, row:1);
		b.WithButton("8:30", "poll-row-830", ButtonStyle.Secondary, disabled:true, row:2);
		b.WithButton("10:00", "poll-row-100", ButtonStyle.Secondary, disabled:true, row:3);

		this.AddSlotButton(b, "vote-mon-700", 1, user);
		this.AddSlotButton(b, "vote-wed-700", 1, user);
		this.AddSlotButton(b, "vote-thu-700", 1, user);
		this.AddSlotButton(b, "vote-fri-700", 1, user);

		this.AddSlotButton(b, "vote-mon-830", 2, user);
		this.AddSlotButton(b, "vote-wed-830", 2, user);
		this.AddSlotButton(b, "vote-thu-830", 2, user);
		this.AddSlotButton(b, "vote-fri-830", 2, user);

		this.AddSlotButton(b, "vote-mon-100", 2, user);
		this.AddSlotButton(b, "vote-wed-100", 2, user);
		this.AddSlotButton(b, "vote-thu-100", 2, user);
		this.AddSlotButton(b, "vote-fri-100", 2, user);

		return b.Build();
	}

	public void AddSlotButton(ComponentBuilder b, string id, int row, ulong user)
	{
		HashSet<ulong> set = this.GetSet(id);
		bool userSelected = set.Contains(user);
		b.WithButton(
			userSelected ? "✔" : "✖",
			id,
			userSelected ? ButtonStyle.Primary : ButtonStyle.Secondary,
			row:row);
	}

	public async Task Vote(string id, ulong user)
	{
		HashSet<ulong> set = this.GetSet(id);

		if (set.Contains(user))
		{
			set.Remove(user);
		}
		else
		{
			set.Add(user);
		}

		await this.UpdateSchedule();
	}

	public HashSet<ulong> GetSet(string id)
	{
		switch(id)
		{
			case "vote-mon-700": return this.Mon.Votes700;
			case "vote-wed-700": return this.Wed.Votes700;
			case "vote-thu-700": return this.Thu.Votes700;
			case "vote-fri-700": return this.Fri.Votes700;

			case "vote-mon-830": return this.Mon.Votes830;
			case "vote-wed-830": return this.Wed.Votes830;
			case "vote-thu-830": return this.Thu.Votes830;
			case "vote-fri-830": return this.Fri.Votes830;

			case "vote-mon-100": return this.Mon.Votes100;
			case "vote-wed-100": return this.Wed.Votes100;
			case "vote-thu-100": return this.Thu.Votes100;
			case "vote-fri-100": return this.Fri.Votes100;
		}

		throw new NotSupportedException();
	}
}