using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FGenConsole
{
	class Program
	{
		private static readonly CancellationTokenSource cSource = new CancellationTokenSource();
		private static readonly TimeSpan ReadWaitInterval = TimeSpan.FromSeconds(2);
		private static readonly JobHandler jobHandler = new JobHandler(ReadWaitInterval);

		static async Task Main(string[] args)
		{
			bool testing = args.Length > 0;

			string testingClause = testing ? " under test" : "";

			Console.WriteLine($"Application has started{testingClause}.");

			//float[] ft = new float[] { 1, 2, 3, 4 };

			//RectangleInt area = new RectangleInt(new PointInt(0, 0), new SizeInt(2, 2));

			//FJobResult fJobResult = new FJobResult(area, ft);

			//string strFt = fJobResult.Counts;

			//FJobResult fJobResult2 = new FJobResult(area, strFt);
			//float[] ft2 = fJobResult2.GetValues();


			//Console.CancelKeyPress += (sender, eventArgs) =>
			//{
			//	eventArgs.Cancel = true;
			//	cSource.Cancel();
			//};

			var keyBoardTask = Task.Run(() =>
			{
				Console.WriteLine("Press enter to cancel");
				Console.ReadKey();

				// Cancel the task
				cSource.Cancel();
			});

			if (testing)
			{
				// Wait for 2 seconds and then send 10 request, one every 1/5 of a second.
				jobHandler.SendTestRequestsAsync();
			}

			try
			{
				await jobHandler.HandleJobs(cSource.Token);
			}
			catch (TaskCanceledException)
			{
				Debug.WriteLine("Got the task cancelled exception.");
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Error: {e.Message}.");
			}

			await keyBoardTask;

			Console.WriteLine("Now shutting down");
		}

		//private static async Task<ConsoleKey> GetConsoleKeyAsync()
		//{
		//	try
		//	{
		//		ConsoleKey key = default;
		//		await Task.Run(() => key = Console.ReadKey(true).Key);
		//		return key;
		//	}
		//	catch (Exception ex)
		//	{
		//		throw ex;
		//	}
		//}

	}
}
