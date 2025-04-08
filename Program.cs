namespace Peanits;

using System;

public class Program
{
	public static bool ShutdownRequested = false;

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
