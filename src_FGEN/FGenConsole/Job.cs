//using CountsRepo;
using MqMessages;
using qdDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace FGenConsole
{
	internal class Job : IDisposable
	{
		private const string DiagTimeFormat = "HH:mm:ss ffff";

		private int _numberOfSectionRemainingToSend;

		//private ValueRecords<KPoint, SubJobResult> _countsRepo;
		private readonly KPoint _position;
		private readonly object _repoLock = new object();

		private readonly SubJobIterator _subJobIterator;

		#region Constructor

		public Job(FJobRequest fJobRequest, string requestMsgId)
		{
			FJobRequest = fJobRequest;
			RequestMsgId = requestMsgId;

			FGenJob fGenJob = BuildFGenJob(fJobRequest);
			FGenerator = new FGenerator(fGenJob);

			//_position = new KPoint(fJobRequest.Area.Point.X * FGenerator.BLOCK_WIDTH, fJobRequest.Area.Point.Y * FGenerator.BLOCK_HEIGHT);
			_position = new KPoint(fJobRequest.Area.Point.X, fJobRequest.Area.Point.Y);

			_subJobIterator = new SubJobIterator(this);
			ResetSubJobsRemainingToBeSent();

			_closed = false;

			Debug.WriteLine($"Creating new Repo. Name: {Name}, JobId: {JobId}.");
			//_countsRepo = new ValueRecords<KPoint, SubJobResult>(Name);
			ReportRepoContents();

			//Debug.WriteLine($"Starting to get histogram for {Name} at {DateTime.Now.ToString(DiagTimeFormat)}.");
			//Dictionary<int, int> h = GetHistogram();
			//Debug.WriteLine($"Histogram complete for {Name} at {DateTime.Now.ToString(DiagTimeFormat)}.");
		}

		private FGenJob BuildFGenJob(FJobRequest fJobRequest)
		{
			PointDd start = new PointDd(new Dd(fJobRequest.Coords.StartingX), new Dd(fJobRequest.Coords.StartingY));
			PointDd end = new PointDd(new Dd(fJobRequest.Coords.EndingX), new Dd(fJobRequest.Coords.EndingY));

			qdDotNet.SizeInt samplePoints = new qdDotNet.SizeInt(fJobRequest.SamplePoints.Width, fJobRequest.SamplePoints.Height);
			qdDotNet.RectangleInt area = new qdDotNet.RectangleInt(
				new qdDotNet.PointInt(fJobRequest.Area.Point.X, fJobRequest.Area.Point.Y),
				new qdDotNet.SizeInt(fJobRequest.Area.Size.Width, fJobRequest.Area.Size.Height));

			FGenJob fGenJob = new FGenJob(fJobRequest.JobId, start, end, samplePoints, fJobRequest.MaxIterations, area);

			return fGenJob;
		}

		private void ReportRepoContents()
		{
			Debug.WriteLine("The Repo contains:");
			//IEnumerable<KPoint> keys = _countsRepo.GetKeys();
			//foreach (KPoint key in keys)
			//{
			//	Debug.WriteLine(key);
			//}
		}

		#endregion

		public FJobRequest FJobRequest { get; private set; }
		public string RequestMsgId { get; private set; }
		public int JobId => FJobRequest.JobId;
		public string Name => FJobRequest.Name;

		public FGenerator FGenerator { get; }

		// Just using the Job's name for the Repo file name for now, this could change.
		public string RepoFilename => FJobRequest.Name;

		public bool IsCompleted => _subJobIterator.IsCompleted;
		public bool IsLastSubJob { get; private set; }

		private bool _closed;
		public bool Closed
		{
			get
			{
				lock (_repoLock)
				{
					return _closed;
				}
			}
			private set
			{
				_closed = value;
			}
		}

		public SubJob GetNextSubJob() => _subJobIterator.GetNextSubJob();

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

		// TODO: Consider using a lock statment here.
		public void ResetSubJobsRemainingToBeSent()
		{
			int newVal = FJobRequest.Area.Size.Height * FJobRequest.Area.Size.Width;
			Interlocked.Exchange(ref _numberOfSectionRemainingToSend, newVal);
			IsLastSubJob = false;
		}

		public bool Close(bool deleteRepo)
		{
			lock(_repoLock)
			{
				//CloseCountsRepo(deleteRepo);
				Closed = true;
			}

			return true;
		}

		#region Repository Methods

		public void WriteWorkResult(SubJob subJob, bool overwriteResults)
		{
			KPoint key = subJob.Position;
			SubJobResult val = subJob.SubJobResult;

			// When writing, include the Area's offset.
			KPoint transKey = key.ToGlobal(_position);

			//try
			//{
			//	lock (_repoLock)
			//	{
			//		if (Closed) return;

			//		if (overwriteResults)
			//		{
			//			_countsRepo.Change(transKey, val);
			//		}
			//		else
			//		{
			//			_countsRepo.Add(transKey, val, saveOnWrite: true);
			//		}
			//	}
			//}
			//catch
			//{
			//	Debug.WriteLine($"Could not write data for x: {key.X} and y: {key.Y}.");
			//}
		}

		public bool RetrieveWorkResultFromRepo(KPoint riKey, SubJobResult workResult)
		{
			return false;
			//// When writing include the Area's offset.
			//KPoint transKey = riKey.ToGlobal(_position);

			//lock (_repoLock)
			//{
			//	if (Closed) return false;
			//	bool result = _countsRepo.ReadParts(transKey, workResult);
			//	return result;
			//}
		}

		//public IEnumerable<Tuple<MapSectionResult, bool>> ReplayResults()
		//{
		//	SubJob subJob = GetNextSubJob();

		//	while (subJob != null)
		//	{
		//		MapSection ms = subJob.MapSectionWorkRequest.MapSection;
		//		RectangleInt riKey = ms.GetRectangleInt();
		//		MapSectionWorkResult workResult = GetEmptyResult(riKey);

		//		if (RetrieveWorkResultFromRepo(riKey, workResult))
		//		{
		//			MapSectionResult msr = new MapSectionResult(JobId, ms, workResult.Counts);

		//			Tuple<MapSectionResult, bool> item = new Tuple<MapSectionResult, bool>(msr, IsLastSubJob);
		//			DecrementSubJobsRemainingToBeSent();

		//			subJob = GetNextSubJob();
		//			yield return item;
		//		}
		//		else
		//		{
		//			yield return null;
		//		}
		//	}
		//}

		//private void CloseCountsRepo(bool deleteRepo)
		//{
		//	Debug.WriteLine($"Starting to close the repo: {RepoFilename} at {DateTime.Now.ToString(DiagTimeFormat)}.");
		//	if (_countsRepo != null)
		//	{
		//		ValueRecords<KPoint, SubJobResult> repo = _countsRepo;
		//		_countsRepo = null;
		//		repo.Dispose();
		//	}
		//	if (deleteRepo)
		//	{
		//		ValueRecords<KPoint, SubJobResult>.DeleteRepo(RepoFilename);
		//		Debug.WriteLine($"Completed deleting the repo: {RepoFilename} at {DateTime.Now.ToString(DiagTimeFormat)}.");
		//	}
		//	else
		//	{
		//		Debug.WriteLine($"Completed closing the repo: {RepoFilename} at {DateTime.Now.ToString(DiagTimeFormat)}.");
		//	}
		//}

		public List<KPoint> GetRepoKeysForJob()
		{
			IEnumerable<KPoint> keys = GetAllRepoKeys();
			if (keys == null) return null;

			int sx = FJobRequest.Area.Point.X;
			int sy = FJobRequest.Area.Point.Y;

			int ex = sx + FJobRequest.Area.Size.Width;
			int ey = sy + FJobRequest.Area.Size.Height;

			List<KPoint> result = new List<KPoint>();
			foreach(KPoint pos in keys)
			{
				if(pos.X >= sx && pos.X < ex && pos.Y >= sy && pos.Y < ey)
				{
					KPoint localKey = pos.FromGlobal(_position);
					result.Add(localKey);
				}
			}

			return result;
		}

		public IEnumerable<KPoint> GetAllRepoKeys()
		{
			//lock (_repoLock)
			//{
			//	if (Closed) return null;
			//	IEnumerable<KPoint> keys = _countsRepo.GetKeys();
			//	return keys;
			//}

			IEnumerable<KPoint> keys = new List<KPoint>();
			return keys;
		}

		public IDictionary<int, int> GetHistogram()
		{
			IEnumerable<KPoint> keys = GetAllRepoKeys();
			if (keys == null) return null;

			IDictionary<int, int> result = new Dictionary<int, int>();

			foreach (KPoint key in keys)
			{
				SubJobResult subJobResult = GetEmptySubJobResult(key);

				if(RetrieveWorkResultFromRepo(key, subJobResult))
				{
					foreach (int cntAndEsc in subJobResult.Counts)
					{
						int cnt = cntAndEsc / 10000;
						if (result.TryGetValue(cnt, out int occurances))
						{
							result[cnt] = occurances + 1;
						}
						else
						{
							result[cnt] = 1;
						}
					}

					subJobResult.IsFree = true;
				}
			}

			if (Closed) // Check to see if we have been closed since this method was called.
				return null;
			else
				return result;
		}

		private SubJobResult _emptySubJobResult = null;
		private SubJobResult GetEmptySubJobResult(KPoint dummy)
		{
			if (_emptySubJobResult == null)
			{
				int size = FGenerator.BLOCK_WIDTH * FGenerator.BLOCK_HEIGHT;
				string instanceName = "MainJob"; 
				_emptySubJobResult = SubJobResult.GetEmptySubJobResult(size, instanceName, includeZValuesOnRead: false);
			}

			SubJobResult.ClearSubJobResult(_emptySubJobResult);
			return _emptySubJobResult;
		}


		#endregion

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects).
					//if (_countsRepo != null)
					//{
					//	_countsRepo.Dispose();
					//}

					//if(DeleteRepoOnDispose)
					//{
					//	ValueRecords<KPoint, SubJobResult>.DeleteRepo(RepoFilename);
					//}

					if (FGenerator != null)
					{
						FGenerator.Dispose();
						Debug.WriteLine($"Job: {JobId} has just called Dispose on it''s FGenerator.");
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~Job() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}

		#endregion
	}

}
