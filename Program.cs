namespace Peanits;

using System;
using System.Text.Json;

public class Program
{
	public static bool ShutdownRequested = false;

	public static T GetData<T>(string name)
		where T : new()
	{
		if (!File.Exists(name))
			return new T();

		string json = File.ReadAllText(name);
		T? val = JsonSerializer.Deserialize<T>(json);

		if (val == null)
			return new T();

		return val;
	}

	public static void SetData(object data, string name)
	{
		if (File.Exists(name))
			File.Delete(name);

		string json = JsonSerializer.Serialize(data);
		File.WriteAllText(name, json);
	}

	private static async Task Main(string[] args)
	{
		Bot bot = new();
		await bot.Start();

		while (!ShutdownRequested)
		{
			await Task.Delay(1000);
		}

		await bot.Stop();
	}
}
