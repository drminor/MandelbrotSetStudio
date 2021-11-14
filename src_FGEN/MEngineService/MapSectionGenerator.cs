using MEngineDataContracts;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using qdDotNet;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MEngineService
{
	public class MapSectionGenerator
	{
		private readonly DtoMapper _dtoMapper;

		public MapSectionGenerator()
		{
			_dtoMapper = new DtoMapper();
		}

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{

			//FGenJob? fGenJob = BuildFGenJob(mapSectionRequest);
			//FGenerator fGenerator = new FGenerator(fGenJob);

			var dd = new Dd(1d, 12132d);

			string strDd = dd.GetStringVal();

			var result = new MapSectionResponse
			{
				Status = 0,          // Ok
				QueuePosition = -1,   // Unknown
				Test = new double[] { dd.hi, dd.lo },
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
