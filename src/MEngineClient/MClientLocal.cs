using MEngineDataContracts;
using MSetGeneratorPrototype;
using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MEngineClient
{
	public class MClientLocal : IMEngineClient
	{
		private static int _sectionCntr;

		private readonly IMapSectionGenerator _generator;

		#region Constructors

		static MClientLocal()
		{
			_sectionCntr = 0;
		}

		public MClientLocal(bool useDepthFirst)
		{
			UsingDepthFirst = useDepthFirst; 

			if (UsingDepthFirst)
			{
				_generator = new MapSectionGeneratorDepthFirst(RMapConstants.BLOCK_SIZE, limbCount: 2);
				EndPointAddress = "CSharp_DepthFirstSimdGenerator";
			}
			else
			{
				_generator = new MapSectionGenerator(RMapConstants.BLOCK_SIZE, limbCount: 2);
				EndPointAddress = "CSharp_SimdGenerator";
			}
		}

		#endregion

		#region Public Properties

		public bool UsingDepthFirst { get; init; }
		public string EndPointAddress { get; init; }
		public bool IsLocal => true;

		#endregion

		#region Async Methods

		public async Task<MapSectionServiceResponse> GenerateMapSectionAsync(MapSectionServiceRequest mapSectionRequest)
		{
			mapSectionRequest.ClientEndPointAddress = EndPointAddress;

			var stopWatch = Stopwatch.StartNew();
			var mapSectionResponse = await GenerateMapSectionAsyncInternal(mapSectionRequest);
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

			Debug.Assert(mapSectionResponse.ZValues == null && mapSectionResponse.ZValuesForLocalStorage == null, "The MapSectionResponse includes ZValues.");

			return mapSectionResponse;
		}

		private async Task<MapSectionServiceResponse> GenerateMapSectionAsyncInternal(MapSectionServiceRequest mapSectionRequest)
		{
			if (DateTime.Now > DateTime.Today.AddDays(1d))
			{
				await Task.Delay(100);
			}

			var mapSectionResponse = _generator.GenerateMapSection(mapSectionRequest);

			if (++_sectionCntr % 10 == 0)
			{
				Debug.WriteLine($"The MEngineClient, {EndPointAddress} has processed {++_sectionCntr} requests.");
			}

			mapSectionResponse.IncludeZValues = false;

			return mapSectionResponse;
		}

		#endregion

		#region Synchronous Methods

		public MapSectionServiceResponse GenerateMapSection(MapSectionServiceRequest mapSectionRequest)
		{
			mapSectionRequest.ClientEndPointAddress = EndPointAddress;

			var stopWatch = Stopwatch.StartNew();
			var mapSectionResponse = GenerateMapSectionInternal(mapSectionRequest);
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

			Debug.Assert(mapSectionResponse.ZValues == null && mapSectionResponse.ZValuesForLocalStorage == null, "The MapSectionResponse includes ZValues.");

			return mapSectionResponse;
		}

		private MapSectionServiceResponse GenerateMapSectionInternal(MapSectionServiceRequest mapSectionRequest)
		{
			var mapSectionResponse = _generator.GenerateMapSection(mapSectionRequest);

			if (++_sectionCntr % 10 == 0)
			{
				Debug.WriteLine($"The MEngineClient, {EndPointAddress} has processed {++_sectionCntr} requests.");
			}

			mapSectionResponse.IncludeZValues = false;

			return mapSectionResponse;
		}

		#endregion
	}
}
