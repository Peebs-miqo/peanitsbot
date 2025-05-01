namespace Peanits;

using System.Text;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;

public class RaidPlannerModule
	: InteractionModuleBase<SocketInteractionContext>
{
	private Schedule schedule;

	public RaidPlannerModule()
	{
		this.schedule = Program.GetData<Schedule>("schedule.json");
	}

	public void Save()
	{
		Program.SetData(this.schedule, "schedule.json");
	}

	[SlashCommand("poll", "Start a weekly raid time poll")]
	public async Task Poll()
	{
		await this.RespondAsync("Done", ephemeral: true);
		RestUserMessage message = await this.Context.Channel.SendMessageAsync(null, components:this.schedule.Build());
		this.schedule.MessageId = message.Id;
		this.schedule.ChannelId = message.Channel.Id;
		this.Save();
	}

	[SlashCommand("reset", "reset the schedule")]
	public async Task Reset()
	{
		this.schedule.Reset();
		this.Save();
		await this.RespondAsync("Done", ephemeral: true);
	}

	[ComponentInteraction("poll-header-*")]
	public async Task OnPollCallback()
	{
		SocketMessageComponent? sm = this.Context.Interaction as SocketMessageComponent;
		if (sm == null)
			return;

		await this.schedule.Vote(sm.Data.CustomId, this.Context.Interaction.User.Id);
		this.Save();

	}
}

public class Schedule
{
	public ulong ChannelId { get; set; }
	public ulong MessageId { get; set; }

	public Day Monday { get; set; } = new();
	public Day Tuesday { get; set; } = new();
	public Day Wednesday { get; set; } = new();
	public Day Thursday { get; set; } = new();
	public Day Friday { get; set; } = new();
	public Day Saturday { get; set; } = new();
	public Day Sunday { get; set; } = new();

	public class Day
	{
		public HashSet<ulong> Votes400 { get; set; } = new();
		public HashSet<ulong> Votes830 { get; set; } = new();
		public void Reset()
		{
			this.Votes400.Clear();
			this.Votes830.Clear();
		}
	}

	public void Reset()
	{
		this.Monday.Reset();
		this.Tuesday.Reset();
		this.Wednesday.Reset();
		this.Thursday.Reset();
		this.Friday.Reset();
		this.Saturday.Reset();
		this.Sunday.Reset();
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

		this.GetVoteCount(ref voteCounts, this.Monday);
		this.GetVoteCount(ref voteCounts, this.Tuesday);
		this.GetVoteCount(ref voteCounts, this.Wednesday);
		this.GetVoteCount(ref voteCounts, this.Thursday);
		this.GetVoteCount(ref voteCounts, this.Friday);
		this.GetVoteCount(ref voteCounts, this.Saturday);
		this.GetVoteCount(ref voteCounts, this.Sunday);

		return voteCounts;
	}

	private void GetVoteCount(ref Dictionary<ulong, int> voteCounts, Day day)
	{
		this.GetVoteCount(ref voteCounts, day.Votes400);
		this.GetVoteCount(ref voteCounts, day.Votes830);
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

		ComponentBuilder b = new();
		b.WithButton("Monday 8:30PM", "poll-header-mon", ButtonStyle.Secondary, disabled:false, row:0);
		b.WithButton("Tuesday (Reclears)", "poll-header-tues", ButtonStyle.Secondary, disabled:true, row:0);
		b.WithButton("Wednesday 8:30PM", "poll-header-wed", ButtonStyle.Secondary, disabled:false, row:0);
		b.WithButton("Thursday 8:30PM", "poll-header-thu", ButtonStyle.Secondary, disabled:false, row:0);
		b.WithButton("Friday 8:30PM", "poll-header-fri", ButtonStyle.Secondary, disabled:false, row:0);
		b.WithButton("Saturday 4PM", "poll-header-sat4", ButtonStyle.Secondary, disabled:false, row:1);
		b.WithButton("Saturday 8PM", "poll-header-sat8",ButtonStyle.Secondary, disabled:false, row:1);
		b.WithButton("Sunday 4PM", "poll-header-sun4", ButtonStyle.Secondary, disabled:false, row:2);
		b.WithButton("Sunday 8PM", "poll-header-sun8", ButtonStyle.Secondary, disabled:false, row:2);

		/*b.WithButton("7:00", "poll-row-700", ButtonStyle.Secondary, disabled:true, row:1);
		b.WithButton("8:30", "poll-row-830", ButtonStyle.Secondary, disabled:true, row:2);
		b.WithButton("10:00", "poll-row-100", ButtonStyle.Secondary, disabled:true, row:3);

		b.WithButton($"{this.Monday.Votes700.Count}", "group-mon-700", this.Monday.Votes700.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:1, disabled:true);
		b.WithButton($"{this.Wednesday.Votes700.Count}", "group-wed-700", this.Wednesday.Votes700.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:1, disabled:true);
		b.WithButton($"{this.Thu.Votes700.Count}", "group-thu-700", this.Thu.Votes700.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:1, disabled:true);
		b.WithButton($"{this.Friday.Votes700.Count}", "group-fri-700", this.Friday.Votes700.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:1, disabled:true);

		b.WithButton($"{this.Monday.Votes830.Count}", "group-mon-830", this.Monday.Votes830.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);
		b.WithButton($"{this.Wednesday.Votes830.Count}", "group-wed-830", this.Wednesday.Votes830.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);
		b.WithButton($"{this.Thu.Votes830.Count}", "group-thu-830", this.Thu.Votes830.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);
		b.WithButton($"{this.Friday.Votes830.Count}", "group-fri-830", this.Friday.Votes830.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);

		b.WithButton($"{this.Monday.Votes100.Count}", "group-mon-100", this.Monday.Votes100.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);
		b.WithButton($"{this.Wednesday.Votes100.Count}", "group-wed-100", this.Wednesday.Votes100.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);
		b.WithButton($"{this.Thu.Votes100.Count}", "group-thu-100", this.Thu.Votes100.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);
		b.WithButton($"{this.Friday.Votes100.Count}", "group-fri-100", this.Friday.Votes100.Count >= 6 ? ButtonStyle.Success : ButtonStyle.Secondary, row:2, disabled:true);

		b.WithButton($"Show my votes", "poll-show-me", ButtonStyle.Primary, row:3);*/

		return b.Build();
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
		/*switch(id)
		{
			case "vote-mon-700": return this.Monday.Votes700;
			case "vote-wed-700": return this.Wednesday.Votes700;
			case "vote-thu-700": return this.Thu.Votes700;
			case "vote-fri-700": return this.Friday.Votes700;

			case "vote-mon-830": return this.Monday.Votes830;
			case "vote-wed-830": return this.Wednesday.Votes830;
			case "vote-thu-830": return this.Thu.Votes830;
			case "vote-fri-830": return this.Friday.Votes830;

			case "vote-mon-100": return this.Monday.Votes100;
			case "vote-wed-100": return this.Wednesday.Votes100;
			case "vote-thu-100": return this.Thu.Votes100;
			case "vote-fri-100": return this.Friday.Votes100;
		} */

		throw new NotSupportedException();
	}
}