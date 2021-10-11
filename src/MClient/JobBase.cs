using FSTypes;
using System;
using System.Threading;

namespace MClient
{
	public class JobBase : IJob
	{
		private int _jobId;
		private int _numberOfSectionRemainingToSend;

		#region Constructor

		public JobBase(SMapWorkRequest sMapWorkRequest)
		{
			SMapWorkRequest = sMapWorkRequest ?? throw new ArgumentNullException(nameof(sMapWorkRequest));
			_jobId = -1;
			CancelRequested = false;
			ResetSubJobsRemainingToBeSent();
		}

		#endregion

		#region Public Properties

		public SMapWorkRequest SMapWorkRequest { get; private set; }

		public string RepoFilename => SMapWorkRequest.Name;

		public string ConnectionId => SMapWorkRequest.ConnectionId;
		public int MaxIterations => SMapWorkRequest.MaxIterations;
		public bool RequiresQuadPrecision() => SMapWorkRequest.RequiresQuadPrecision();

		public bool CancelRequested { get; set; }
		public bool IsCompleted { get; protected set; }
		public bool IsLastSubJob { get; protected set; }

		public int JobId
		{
			get { return _jobId; }
			set
			{
				if (value == -1) throw new ArgumentException("-1 cannot be used as a JobId.");
				if (_jobId != -1) throw new InvalidOperationException("The JobId cannot be set once it has already been set.");

				_jobId = value;
			}
		}

		#endregion

		/// <summary>
		/// Sets IsLastSubJob = true, if the number of sections remining to send reaches 0.
		/// </summary>
		public void DecrementSubJobsRemainingToBeSent()
		{
			int newVal = Interlocked.Decrement(ref _numberOfSectionRemainingToSend);
			if (newVal == 0)
			{
				IsLastSubJob = true;
			}
		}

		public void ResetSubJobsRemainingToBeSent()
		{
			_numberOfSectionRemainingToSend = SMapWorkRequest.Area.CanvasSize.Width * SMapWorkRequest.Area.CanvasSize.Height;
			IsLastSubJob = false;
		}
	}
}
