namespace Peanits;

using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

public class RaidPlannerModule
	: InteractionModuleBase<SocketInteractionContext>
{
	private Schedule schedule;

	public RaidPlannerModule()
	{
		Schedule? schedule = Program.GetData<Schedule>("schedule.json");

		if (schedule == null)
		{
			schedule = new();
			schedule.AddSlot(DayOfWeek.Monday, new(20, 30), "Evening");
			schedule.AddSlot(DayOfWeek.Wednesday, new(20, 30), "Evening");
			schedule.AddSlot(DayOfWeek.Thursday, new(20, 30), "Evening");
			schedule.AddSlot(DayOfWeek.Friday, new(20, 30), "Evening");
			schedule.AddSlot(DayOfWeek.Saturday, new(16, 00), "Afternoon");
			schedule.AddSlot(DayOfWeek.Saturday, new(18, 00), "Evening");
			schedule.AddSlot(DayOfWeek.Sunday, new(16, 00), "Afternoon");
			schedule.AddSlot(DayOfWeek.Sunday, new(18, 00), "Evening");
		}

		this.schedule = schedule;

	}

	public void Save()
	{
		Program.SetData(this.schedule, "schedule.json");
	}

	[SlashCommand("create", "Create a weekly schedule here")]
	public async Task Poll()
	{
		await this.schedule.Post(this.Context.Channel);
		this.Save();
		await this.RespondAsync("Done", ephemeral: true);
	}

	[SlashCommand("update", "Updates posted days")]
	public async Task Update()
	{
		await this.DeferAsync(true);
		await this.ModifyOriginalResponseAsync((m) => m.Content = "Updating...");

		await this.schedule.Update(this.Context.Channel);
		this.Save();

		await this.ModifyOriginalResponseAsync((m) => m.Content = "Done");
	}

	[SlashCommand("reset", "Resets the selected day")]
	public async Task Reset(DayOfWeek day)
	{
		await this.schedule.Reset(day, this.Context.Channel);
		this.Save();
		await this.RespondAsync("Done", ephemeral: true);
	}

	[SlashCommand("set-icon-store", "Sets the schedules icon store to the server")]
	public async Task SetIconStore()
	{
		this.schedule.SetIconStore(this.Context.Guild);
		this.Save();
		await this.RespondAsync("Done", ephemeral: true);
	}

	[ComponentInteraction("join-*")]
	public async Task OnJoinCallback()
	{
		SocketMessageComponent? sm = this.Context.Interaction as SocketMessageComponent;
		if (sm == null || sm.GuildId == null)
			return;

		Schedule.Day? day = this.schedule.Join(sm.Data.CustomId, this.Context.Interaction.User);

		if (day == null)
		{
			await this.RespondAsync("Failed");
		}
		else
		{
			(string message, MessageComponent component) = await day.Generate();
			await sm.UpdateAsync(m =>
			{
				m.Content = message;
				m.Components = component;
			});

			this.Save();
		}
	}
}

public class Schedule
{
	public List<Day> Days { get; set; } = new();

	public class Day
	{
		public ulong MessageId { get; set; }
		public ulong? IconStore { get; set; }
		public DayOfWeek RaidDay { get; set; } = DayOfWeek.Sunday;
		public List<Slot> Slots { get; set; } = new();

		public void AddSlot(string name, TimeOnly time)
		{
			Slot slot = new();
			slot.Name = name;
			slot.Time = time;
			this.Slots.Add(slot);
		}

		public async Task<(string message, MessageComponent component)> Generate()
		{
			await Task.Yield();

			StringBuilder message = new();
			ComponentBuilder components = new();

			TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("E. Australia Standard Time");
			DateTimeOffset dayTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
			dayTime = dayTime.AddDays(-(int)dayTime.DayOfWeek);
			dayTime = dayTime.AddDays((int)this.RaidDay);

			if(dayTime < DateTimeOffset.Now)
				dayTime = dayTime.AddDays(7);

			message.AppendLine("<:empty:1367790271059984434>");
			message.AppendLine($"**{this.RaidDay.ToString()}**");

			foreach(Slot slot in this.Slots)
			{
				DateTimeOffset slotTime = dayTime.Date;
				slotTime = slotTime.AddTicks(slot.Time.Ticks);

				message.Append(slot.GetIcon());
				message.Append(slot.Name);
				message.Append($" - <t:{slotTime.ToUnixTimeSeconds()}:t>");
				message.Append($" (<t:{slotTime.ToUnixTimeSeconds()}:R>) ");
				message.Append($" [{slot.Users.Count}/8]: ");

				bool isFirstUser = true;
				foreach(ulong userId in slot.Users)
				{
					if (!isFirstUser)
						message.Append(",   ");

					isFirstUser = false;

					IUser user = await Bot.Client.GetUserAsync(userId);

					if (this.IconStore != null)
					{
						message.Append(await DiscordUtils.GetUserAvatarAsEmote(user, (ulong)this.IconStore));
						message.Append(" ");
					}

					message.Append(user.GlobalName);
				}

				message.AppendLine();
				components.WithButton(slot.Name, $"join-{this.RaidDay}-{slot.Name}", slot.GetButtonStyle());
			}

			return (message.ToString(), components.Build());
		}
	}

	public class Slot
	{
		public string Name { get; set; } = string.Empty;
		public TimeOnly Time { get; set; }
		public HashSet<ulong> Users { get; set; } = new();

		public string GetIcon()
		{
			if(this.Users.Count < 6)
				return ":black_small_square: ";

			if(this.Users.Count < 8)
				return ":small_orange_diamond: ";

			return ":small_blue_diamond: ";
		}

		public ButtonStyle GetButtonStyle()
		{
			if(this.Users.Count < 6)
				return ButtonStyle.Secondary;

			if(this.Users.Count < 8)
				return ButtonStyle.Primary;

			return ButtonStyle.Success;
		}
	}

	public void AddSlot(DayOfWeek dayOfWeek, TimeOnly time, string name)
	{
		Day day = this.GetDay(dayOfWeek);
		day.AddSlot(name, time);
	}

	public Day GetDay(DayOfWeek dayOfweek)
	{
		foreach(Day day in this.Days)
		{
			if(day.RaidDay == dayOfweek)
			{
				return day;
			}
		}

		Day newDay = new();
		newDay.RaidDay = dayOfweek;
		this.Days.Add(newDay);

		return newDay;
	}

	public async Task Post(IMessageChannel channel)
	{
		foreach(Day day in this.Days)
		{
			(string message, MessageComponent component) = await day.Generate();
			IUserMessage postedMessage = await channel.SendMessageAsync(message, components:component);
			day.MessageId = postedMessage.Id;
		}
	}

	public async Task Update(IMessageChannel channel)
	{
		foreach(Day day in this.Days)
		{
			(string message, MessageComponent component) = await day.Generate();

			IMessage postedMessage = await channel.GetMessageAsync(day.MessageId);
			if (postedMessage is IUserMessage userMessage)
			{
				await userMessage.ModifyAsync((m) =>
				{
					m.Content = message;
					m.Components = component;
				});
			}
		}
	}

	public Day? Join(string callbackId, IUser user)
	{
		string[] parts = callbackId.Split("-");

		if (parts.Length != 3)
			throw new Exception();

		DayOfWeek dayOfWeek = Enum.Parse<DayOfWeek>(parts[1]);
		string slotName = parts[2];

		Day day = this.GetDay(dayOfWeek);

		foreach(Slot slot in day.Slots)
		{
			if (slot.Name == slotName)
			{
				if (slot.Users.Contains(user.Id))
				{
					slot.Users.Remove(user.Id);
				}
				else
				{
					slot.Users.Add(user.Id);
				}

				return day;
			}
		}

		return null;
	}

	public async Task Reset(DayOfWeek dayOfWeek, IMessageChannel channel)
	{
		Day day = this.GetDay(dayOfWeek);
		foreach(Slot slot in day.Slots)
		{
			slot.Users.Clear();
		}

		IMessage message = await channel.GetMessageAsync(day.MessageId);
		if (message is IUserMessage userMessage)
		{
			(string content, MessageComponent component) = await day.Generate();
			await userMessage.ModifyAsync((m) =>
			{
				m.Content = content;
				m.Components = component;
			});
		}
	}

	public void SetIconStore(IGuild guild)
	{
		foreach(Day day in this.Days)
		{
			day.IconStore = guild.Id;
		}
	}
}