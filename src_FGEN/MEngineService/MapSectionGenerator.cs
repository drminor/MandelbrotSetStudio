using MEngineDataContracts;
using MSS.Common.DataTransferObjects;
using System.Runtime.InteropServices;

namespace MEngineService
{
	public class MapSectionGenerator
	{
		[DllImport("..\\..\\..\\..\\..\\..\\x64\\Debug\\MSetGenerator.dll")]
		public static extern void DisplayHelloFromDLL();

		private readonly DtoMapper _dtoMapper;

		public MapSectionGenerator()
		{
			_dtoMapper = new DtoMapper();
		}

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			//FGenJob? fGenJob = BuildFGenJob(mapSectionRequest);
			//FGenerator fGenerator = new FGenerator(fGenJob);

			DisplayHelloFromDLL();

			//var dd = new Dd(11d);
			//string strDd = dd.GetStringVal();
			string strDd = "hi";

			var result = new MapSectionResponse
			{
				Status = 0,          // Ok
				QueuePosition = -1,   // Unknown
				Test = new double[] { 1, 2 },
				TestString = strDd
			};

			return result;
		}

		//private FGenJob? BuildFGenJob(MapSectionRequest mapSectionRequest)
		//{
		//	var position = _dtoMapper.MapFrom(mapSectionRequest.Position);
		//	var samplePointsDelta = _dtoMapper.MapFrom(mapSectionRequest.SamplePointsDelta);
		//	var blockSize = mapSectionRequest.BlockSize;
		//	var mapCalcSettings = mapSectionRequest.MapCalcSettings;


		//	//PointDd start = new PointDd(new Dd(fJobRequest.Coords.StartingX), new Dd(fJobRequest.Coords.StartingY));
		//	//PointDd end = new PointDd(new Dd(fJobRequest.Coords.EndingX), new Dd(fJobRequest.Coords.EndingY));

		//	//qdDotNet.SizeInt samplePoints = new qdDotNet.SizeInt(fJobRequest.SamplePoints.Width, fJobRequest.SamplePoints.Height);
		//	//qdDotNet.RectangleInt area = new qdDotNet.RectangleInt(
		//	//	new qdDotNet.PointInt(fJobRequest.Area.Point.X, fJobRequest.Area.Point.Y),
		//	//	new qdDotNet.SizeInt(fJobRequest.Area.Size.Width, fJobRequest.Area.Size.Height));

		//	//FGenJob fGenJob = new FGenJob(fJobRequest.JobId, start, end, samplePoints, fJobRequest.MaxIterations, area);

		//	FGenJob? fGenJob = null;

		//	return fGenJob;
		//}


	}
}
