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
		string msg = await this.schedule.BuildMessage();
		RestUserMessage message = await this.Context.Channel.SendMessageAsync(msg, components:this.schedule.BuildComponents());
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

		this.schedule.Vote(sm.Data.CustomId, this.Context.Interaction.User.Id);
		this.Save();

		string msg = await this.schedule.BuildMessage();

		await sm.UpdateAsync((m) =>
		{
			m.Content = msg;
			m.Components = this.schedule.BuildComponents();
		});
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

	public async Task<string> BuildMessage()
	{
		DateTimeOffset week = DateTimeOffset.Now;
		int daysOff = (int)DayOfWeek.Monday - (int)week.DayOfWeek;
		week = week.AddDays(daysOff);

		if(week < DateTimeOffset.Now)
			week = week.AddDays(7);

		DateTimeOffset weekStartDate = week.Date;

		StringBuilder builder = new();
		builder.AppendLine("In the stripped club, straight up Monking it.");
		builder.AppendLine("And by 'IT', haha well. lets justr say. My Peanits.");
		builder.AppendLine();

		builder.Append($"Monday <t:{weekStartDate.AddDays(0).AddHours(20).AddMinutes(30).ToUnixTimeSeconds()}:t>, ");
		builder.Append($"<t:{weekStartDate.AddDays(0).AddHours(20).AddMinutes(30).ToUnixTimeSeconds()}:R> : ");
		builder.AppendLine(await this.GetUsersAsync(this.Monday.Votes830));

		//builder.Append($"<t:{weekStartDate.AddDays(1).AddHours(20).AddMinutes(30).ToUnixTimeSeconds()}:F> : ");
		//builder.AppendLine(await this.GetUsersAsync(this.Tuesday.Votes830));

		builder.Append($"Wednesday <t:{weekStartDate.AddDays(2).AddHours(20).AddMinutes(30).ToUnixTimeSeconds()}:t>, ");
		builder.Append($"<t:{weekStartDate.AddDays(2).AddHours(20).AddMinutes(30).ToUnixTimeSeconds()}:R> : ");
		builder.AppendLine(await this.GetUsersAsync(this.Wednesday.Votes830));

		builder.Append($"Thursday <t:{weekStartDate.AddDays(3).AddHours(20).AddMinutes(30).ToUnixTimeSeconds()}:t>, ");
		builder.Append($"<t:{weekStartDate.AddDays(3).AddHours(20).AddMinutes(30).ToUnixTimeSeconds()}:R> : ");
		builder.AppendLine(await this.GetUsersAsync(this.Thursday.Votes830));

		builder.Append($"Friday <t:{weekStartDate.AddDays(4).AddHours(20).AddMinutes(30).ToUnixTimeSeconds()}:t>, ");
		builder.Append($"<t:{weekStartDate.AddDays(4).AddHours(20).AddMinutes(30).ToUnixTimeSeconds()}:R> : ");
		builder.AppendLine(await GetUsersAsync(this.Friday.Votes830));

		builder.Append($"Saturday <t:{weekStartDate.AddDays(5).AddHours(16).AddMinutes(0).ToUnixTimeSeconds()}:t>, ");
		builder.Append($"<t:{weekStartDate.AddDays(5).AddHours(16).AddMinutes(0).ToUnixTimeSeconds()}:R> : ");
		builder.AppendLine(await this.GetUsersAsync(this.Saturday.Votes400));

		builder.Append($"Saturday <t:{weekStartDate.AddDays(5).AddHours(20).AddMinutes(0).ToUnixTimeSeconds()}:t>, ");
		builder.Append($"<t:{weekStartDate.AddDays(5).AddHours(20).AddMinutes(0).ToUnixTimeSeconds()}:R> : ");
		builder.AppendLine(await this.GetUsersAsync(this.Saturday.Votes830));

		builder.Append($"Sunday <t:{weekStartDate.AddDays(6).AddHours(16).AddMinutes(0).ToUnixTimeSeconds()}:t>, ");
		builder.Append($"<t:{weekStartDate.AddDays(6).AddHours(16).AddMinutes(0).ToUnixTimeSeconds()}:R> : ");
		builder.AppendLine(await GetUsersAsync(this.Sunday.Votes400));

		builder.Append($"Sunday <t:{weekStartDate.AddDays(6).AddHours(20).AddMinutes(0).ToUnixTimeSeconds()}:t>, ");
		builder.Append($"<t:{weekStartDate.AddDays(6).AddHours(20).AddMinutes(0).ToUnixTimeSeconds()}:R> : ");
		builder.AppendLine(await GetUsersAsync(this.Sunday.Votes830));

		return builder.ToString();
	}

	public async Task<string> GetUsersAsync(HashSet<ulong> votes)
	{
		StringBuilder builder = new();
		bool isFirst = true;
		foreach (ulong userId in votes)
		{
			if (!isFirst)
				builder.Append(", ");
			isFirst = false;

			IUser user = await Bot.Client.GetUserAsync(userId);
			builder.Append(user.GlobalName);
		}

		return builder.ToString();
	}
	public MessageComponent BuildComponents()
	{
		ComponentBuilder b = new();
		b.WithButton(
			$"Monday 8:30PM ({this.Monday.Votes830.Count})", 
			"poll-header-mon", 
			this.GetButtonStyle(this.Monday.Votes830.Count),
			row:0);

		b.WithButton(
			$"Tuesday (Reclears)", 
			"poll-header-tues", 
			ButtonStyle.Secondary, 
			disabled:true, 
			row:0);

		b.WithButton(
			$"Wednesday 8:30PM ({this.Wednesday.Votes830.Count})", 
			"poll-header-wed", 
			this.GetButtonStyle(this.Wednesday.Votes830.Count),
			row:0);

		b.WithButton(
			$"Thursday 8:30PM ({this.Thursday.Votes830.Count})", 
			"poll-header-thurs", 
			this.GetButtonStyle(this.Thursday.Votes830.Count),
			row:0);

		b.WithButton(
			$"Friday 8:30PM ({this.Friday.Votes830.Count})", 
			"poll-header-fri", 
			this.GetButtonStyle(this.Friday.Votes830.Count), 
			row:0);

		b.WithButton(
			$"Saturday 4PM ({this.Saturday.Votes400.Count})", 
			"poll-header-sat4", 
			this.GetButtonStyle(this.Saturday.Votes400.Count), 
			row:1);

		b.WithButton(
			$"Saturday 8PM ({this.Saturday.Votes830.Count})", 
			"poll-header-sat8",
			this.GetButtonStyle(this.Saturday.Votes830.Count), 
			row:1);

		b.WithButton(
			$"Sunday 4PM ({this.Sunday.Votes400.Count})", 
			"poll-header-sun4", 
			this.GetButtonStyle(this.Sunday.Votes400.Count), 
			row:2);

		b.WithButton(
			$"Sunday 8PM ({this.Sunday.Votes830.Count})", 
			"poll-header-sun8", 
			this.GetButtonStyle(this.Sunday.Votes830.Count), 
			row:2);

		return b.Build();
	}
	public ButtonStyle GetButtonStyle(int votes)
	{
		if(votes < 6)
			return ButtonStyle.Secondary;
		
		if(votes < 8)
			return ButtonStyle.Primary;

		return ButtonStyle.Success;
	}

	public void Vote(string id, ulong user)
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
	}

	public HashSet<ulong> GetSet(string id)
	{
		switch(id)
		{
			case "poll-header-mon": return this.Monday.Votes830;
			case "poll-header-tues": return this.Tuesday.Votes830;
			case "poll-header-wed": return this.Wednesday.Votes830;
			case "poll-header-thurs": return this.Thursday.Votes830;
			case "poll-header-fri": return this.Friday.Votes830;
			case "poll-header-sat4": return this.Saturday.Votes400;
			case "poll-header-sat8": return this.Saturday.Votes830;
			case "poll-header-sun4": return this.Sunday.Votes400;
			case "poll-header-sun8": return this.Sunday.Votes830;
		}

		throw new NotSupportedException();
	}
}