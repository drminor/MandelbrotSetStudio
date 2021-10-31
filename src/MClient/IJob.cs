namespace MClient
{
	public interface IJob
	{
		int JobId { get; set; }
		SMapWorkRequest SMapWorkRequest { get; }
		string ConnectionId { get; }

		bool CancelRequested { get; set; }
		bool IsCompleted { get; } // Processing for this job has been completed.
		bool IsLastSubJob { get; } // When the result for this subjob is sent, the client should be notified that there will be no more results.

		bool RequiresQuadPrecision();

		void DecrementSubJobsRemainingToBeSent();
		void ResetSubJobsRemainingToBeSent();
	}
}