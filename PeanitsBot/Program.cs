namespace Peanits;

using System;

public class Program
{
	private static async Task Main(string[] args)
	{
		Bot bot = new();
		await bot.Start();

		Console.WriteLine("Press return to exit");
		Console.ReadLine();
	}
}
