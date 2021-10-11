using FSTypes;

namespace MClient
{
	internal class JobForMq : JobBase
	{
		public JobForMq(SMapWorkRequest sMapWorkRequest) : base(sMapWorkRequest)
		{
		}

		public string MqRequestCorrelationId { get; set; }

		public MqImageResultListener MqImageResultListener { get; set; }

		public void MarkAsCompleted()
		{
			IsCompleted = true;
		}

		public void SetIsLastSubJob(bool val)
		{
			IsLastSubJob = val;
		}
	}
}
