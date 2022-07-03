using MongoDB.Bson;
using MSS.Common.MSetRepo;
using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSS.Common
{
	public class MapJobHelper
	{
		private readonly IMapSectionAdapter _mapSectionAdapter;

		public MapJobHelper(IMapSectionAdapter mapSectionAdapter)
		{
			_mapSectionAdapter = mapSectionAdapter;
		}

		#region Build Job

		public Job BuildJob(ObjectId? parentJobId, ObjectId projectId, SizeInt canvasSize, RRectangle coords, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings,
			TransformType transformType, RectangleInt? newArea, SizeInt blockSize)
		{
			var mapAreaInfo = GetMapAreaInfo(coords, canvasSize, blockSize);
			var result = BuildJob(parentJobId, projectId, mapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newArea);
			return result;
		}

		public Job BuildJob(ObjectId? parentJobId, ObjectId projectId, MapAreaInfo mapAreaInfo, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings,
			TransformType transformType, RectangleInt? newArea)
		{
			if (!parentJobId.HasValue && !(transformType == TransformType.None || transformType == TransformType.CanvasSizeUpdate))
			{
				throw new InvalidOperationException($"Attempting to create an new job with no parent and TransformType = {transformType}. Only jobs with TransformType = 'none' be parentless.");
			}

			// Determine how much of the canvas control can be covered by the new map.
			var canvasSize = mapAreaInfo.CanvasSize;

			//var displaySize = RMapHelper.GetCanvasSize(newArea.Size, canvasSize);

			// Use the exact canvas size -- do not adjust based on aspect ratio of the newArea.
			var displaySize = canvasSize;

			var canvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(displaySize, mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);

			var isPreferredChild = transformType != TransformType.CanvasSizeUpdate;
			var jobName = GetJobName(transformType);

			var job = new Job(parentJobId, isPreferredChild, projectId, jobName, transformType, newArea, mapAreaInfo, canvasSizeInBlocks, colorBandSetId, mapCalcSettings);

			return job;
		}

		public MapAreaInfo GetMapAreaInfo(RRectangle coords, SizeInt canvasSize, SizeInt blockSize)
		{
			// Determine how much of the canvas control can be covered by the new map.
			//var displaySize = RMapHelper.GetCanvasSize(newArea.Size, canvasSize);

			// Use the exact canvas size -- do not adjust based on aspect ratio of the newArea.
			var displaySize = canvasSize;

			// Using the size of the new map and the map coordinates, calculate the sample point size
			var updatedCoords = coords.Clone();
			var samplePointDelta = RMapHelper.GetSamplePointDelta(ref updatedCoords, displaySize);
			//Debug.WriteLine($"\nThe new coords are : {coordsWork},\n old = {mSetInfo.Coords}. (While calculating SamplePointDelta.)\n");

			//var samplePointDeltaD = RMapHelper.GetSamplePointDiag(coords, displaySize, out var newDCoords);
			//RMapHelper.ReportSamplePointDiff(samplePointDelta, samplePointDeltaD, mSetInfo.Coords, coordsWork, newDCoords);

			// Get a subdivision record from the database.
			var subdivision = GetSubdivision(samplePointDelta, blockSize);

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(ref updatedCoords, subdivision, out var canvasControlOffset);

			var result = new MapAreaInfo(updatedCoords, canvasSize, subdivision, mapBlockOffset, canvasControlOffset);

			return result;
		}

		// Find an existing subdivision record that the same SamplePointDelta
		private Subdivision GetSubdivision(RSize samplePointDelta, SizeInt blockSize)
		{
			if (! _mapSectionAdapter.TryGetSubdivision(samplePointDelta, blockSize, out var result))
			{
				result = new Subdivision(samplePointDelta, blockSize);
				_mapSectionAdapter.InsertSubdivision(result);
			}

			return result;
		}

		public static string GetJobName(TransformType transformType)
		{
			var result = transformType == TransformType.None ? "Home" : transformType.ToString();
			return result;
		}

		public static MapAreaInfo GetMapAreaInfo(Job job, SizeInt canvasSize)
		{
			var result = new MapAreaInfo(job.Coords, canvasSize, job.Subdivision, job.MapBlockOffset, job.CanvasControlOffset);

			return result;
		}

		//[Conditional("DEBUG")]
		//public static void CheckCanvasSize(SizeInt canvasSize, SizeInt blockSize)
		//{
		//	var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInBlocks(new SizeDbl(canvasSize), blockSize, keepSquare: true);

		//	if (sizeInWholeBlocks != new SizeInt(8))
		//	{
		//		Debug.WriteLine($"The canvas size is not 1024 x 1024.");
		//		//throw new InvalidOperationException("For testing we need the canvas size to be 1024 x 1024.");
		//	}
		//}

		#endregion

		public Poster CreatePoster(string name, string? description, SizeInt posterSize, ObjectId sourceJobId, RRectangle coords, ColorBandSet colorBandSet, 
			MapCalcSettings mapCalcSettings, SizeInt blockSize, IProjectAdapter projectAdapter)
		{
			var mapAreaInfo = GetMapAreaInfo(coords, posterSize, blockSize);

			var poster = new Poster(name, description, sourceJobId, mapAreaInfo, colorBandSet, mapCalcSettings);

			projectAdapter.CreatePoster(poster);

			return poster;
		}

	}
}
