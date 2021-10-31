using FileDictionaryLib;
using FSTypes;
using MapSectionRepo;
using MqMessages;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MClient
{
	public sealed class Job : JobBase, IDisposable
	{
		public const int SECTION_WIDTH = 100;
		public const int SECTION_HEIGHT = 100;

		private const string DiagTimeFormat = "HH:mm:ss ffff";

		private int _hSectionPtr;
		private int _vSectionPtr;

		private ValueRecords<KPoint, MapSectionWorkResult> _countsRepo;
		private readonly KPoint _position;
		private readonly object _repoLock = new();

		//public int WorkResultWriteCount = 0;
		//public int WorkResultReWriteCount = 0;


		public Job(SMapWorkRequest sMapWorkRequest) : base(sMapWorkRequest)
		{
			_position = new KPoint(sMapWorkRequest.Area.Point.X, sMapWorkRequest.Area.Point.Y);
			SamplePoints = GetSamplePoints(sMapWorkRequest);
			Reset();

			string filename = RepoFilename;
			Debug.WriteLine($"Creating new Repo. Name: {filename}, JobId: {JobId}.");
			_countsRepo = new ValueRecords<KPoint, MapSectionWorkResult>(filename, useHiRezFolder: false);

			//Debug.WriteLine($"Starting to get histogram for {RepoFilename} at {DateTime.Now.ToString(DiagTimeFormat)}.");
			//Dictionary<int, int> h = GetHistogram();
			//Debug.WriteLine($"Histogram complete for {RepoFilename} at {DateTime.Now.ToString(DiagTimeFormat)}.");
		}

		public void Reset()
		{
			_hSectionPtr = 0;
			_vSectionPtr = 0;
			IsCompleted = false;
		}

		public readonly SamplePoints<double> SamplePoints;

		public SubJob GetNextSubJob()
		{
			if (IsCompleted) return null;

			if (_hSectionPtr > SamplePoints.NumberOfHSections - 1)
			{
				_hSectionPtr = 0;
				_vSectionPtr++;

				if (_vSectionPtr > SamplePoints.NumberOfVSections - 1)
				{
					IsCompleted = true;
					return null;
				}
			}
			//System.Diagnostics.Debug.WriteLine($"Creating SubJob for hSection: {_hSectionPtr}, vSection: {_vSectionPtr}.");

			int left = _hSectionPtr * SECTION_WIDTH;
			int top = _vSectionPtr * SECTION_HEIGHT;

			RectangleInt mapSection = new(new PointInt(left, top), new SizeInt(SECTION_WIDTH, SECTION_HEIGHT));
			var mswr = new MapSectionWorkRequest(mapSection, MaxIterations, _hSectionPtr++, _vSectionPtr);
			SubJob result = new(this, mswr);

			return result;
		}

		public void WriteWorkResult(KPoint key, MapSectionWorkResult val, bool overwriteResults)
		{
			// When writing include the Area's offset.
			KPoint transKey = key.ToGlobal(_position);

			try
			{
				lock (_repoLock)
				{
					if(overwriteResults)
					{
						_countsRepo.Change(transKey, val);
						//WorkResultReWriteCount++;
					}
					else
					{
						_countsRepo.Add(transKey, val, saveOnWrite: false);
						//WorkResultWriteCount++;
					}
				}
			}
			catch
			{
				Debug.WriteLine($"Could not write data for x: {transKey.X} and y: {transKey.Y}.");
			}
		}

		public bool RetrieveWorkResultFromRepo(KPoint key, MapSectionWorkResult workResult)
		{
			// When writing include the Area's offset.
			KPoint transKey = key.ToGlobal(_position);

			lock (_repoLock)
			{
				bool result = _countsRepo.ReadParts(transKey, workResult);
				return result;
			}
		}

		public void DeleteCountsRepo()
		{
			Debug.WriteLine($"Starting to delete the old repo: {RepoFilename} at {DateTime.Now.ToString(DiagTimeFormat)}.");
			if (_countsRepo != null)
			{
				_countsRepo.Dispose();
				_countsRepo = null;
			}

			ValueRecords<RectangleInt, MapSectionWorkResult>.DeleteRepo(RepoFilename);
			Debug.WriteLine($"Completed deleting the old repo: {RepoFilename} at {DateTime.Now.ToString(DiagTimeFormat)}.");
		}

		public Dictionary<int, int> GetHistogram()
		{
			Dictionary<int, int> result = new();
			IEnumerable<MapSectionWorkResult> workResults = _countsRepo.GetValues(GetEmptyResult);

			foreach(MapSectionWorkResult wr in workResults)
			{
				foreach(int cntAndEsc in wr.Counts)
				{
					int cnt = cntAndEsc / 10000;
					if(result.TryGetValue(cnt, out int occurances))
					{
						result[cnt] = occurances + 1;
					}
					else
					{
						result[cnt] = 1;
					}
				}
			}

			return result;
		}

		private MapSectionWorkResult _emptyResult = null;
		private MapSectionWorkResult GetEmptyResult(KPoint key)
		{
			//if(area.Size.W != SECTION_WIDTH || area.Size.H != SECTION_HEIGHT)
			//{
			//	Debug.WriteLine("Wrong Area.");
			//}

			if (_emptyResult == null)
			{
				_emptyResult = new MapSectionWorkResult(SECTION_WIDTH * SECTION_HEIGHT, highRes: false, includeZValuesOnRead: false);
			}

			return _emptyResult;
		}

		// TODO: Create an SCoords to Coords converter
		private static SamplePoints<double> GetSamplePoints(SMapWorkRequest sMapWorkRequest)
		{
			//if (Coords.TryGetFromSCoords(sMapWorkRequest.SCoords, out Coords coords))
			//{
			//	double[][] xValueSections = BuildValueSections(coords.LeftBot.X, coords.RightTop.X,
			//		sMapWorkRequest.RectangleInt.Width, SECTION_WIDTH,
			//		sMapWorkRequest.Area.SectionAnchor.X, sMapWorkRequest.Area.RectangleInt.Width);


			//	double[][] yValueSections;
			//	if (!coords.IsUpsideDown)
			//	{
			//		yValueSections = BuildValueSections(coords.RightTop.Y, coords.LeftBot.Y,
			//			sMapWorkRequest.RectangleInt.Height, SECTION_HEIGHT,
			//			sMapWorkRequest.Area.SectionAnchor.Y, sMapWorkRequest.Area.RectangleInt.Height);
			//	}
			//	else
			//	{
			//		yValueSections = BuildValueSections(coords.LeftBot.Y, coords.RightTop.Y,
			//			sMapWorkRequest.RectangleInt.Height, SECTION_HEIGHT,
			//			sMapWorkRequest.Area.SectionAnchor.Y, sMapWorkRequest.Area.RectangleInt.Height);
			//	}

			//	return new SamplePoints<double>(xValueSections, yValueSections);
			//}
			//else
			//{
			//	throw new ArgumentException("Cannot parse the SCoords into a Coords value.");
			//}

			throw new NotImplementedException("Need an SCoords to Coords Converter.");
		}

		private static double[][] BuildValueSections(double start, double end, int extent, int sectionExtent, int areaStart, int areaExtent)
		{
			double mapExtent = end - start;
			double unitExtent = mapExtent / extent;
			int resultExtent = areaExtent * sectionExtent;

			double resultStart = start + unitExtent * (areaStart * sectionExtent);


			int sectionPtr = 0;
			int inSectPtr = 0;
			double[][] result = new double[areaExtent][]; // Number of sections, each with 100 pixels

			result[0] = new double[sectionExtent];

			for (int ptr = 0; ptr < resultExtent; ptr++)
			{
				result[sectionPtr][inSectPtr++] = resultStart + unitExtent * ptr;
				if (inSectPtr > sectionExtent - 1 && ptr < resultExtent - 1)
				{
					inSectPtr = 0;
					sectionPtr++;
					result[sectionPtr] = new double[sectionExtent];
				}
			}

			return result;
		}

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		private void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects).
					if (_countsRepo != null)
					{
						_countsRepo.Dispose();
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

