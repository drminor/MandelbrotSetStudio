using MSetGeneratorPrototype;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Threading;
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

		public MClientLocal(bool useSingleLimb, bool useDepthFirst, bool useCImplementation)
		{
			UsingSingleLimb = useSingleLimb;
			UsingDepthFirst = useDepthFirst; 
			UseCImplementation = useCImplementation;

			if (useSingleLimb)
			{
				_generator = new MapSectionGeneratorSingleLimb();
				EndPointAddress = "CSharp_SingleLimbGenerator";
			}
			else if (UsingDepthFirst)
			{
				_generator = new MapSectionGeneratorDepthFirst(RMapConstants.DEFAULT_LIMB_COUNT, RMapConstants.BLOCK_SIZE, useCImplementation);
				EndPointAddress = "CSharp_DepthFirstGenerator";
			}
			else
			{
				_generator = new MapSectionGenerator(RMapConstants.BLOCK_SIZE, RMapConstants.DEFAULT_LIMB_COUNT);
				EndPointAddress = "CSharp_LimbFirstGenerator";
			}
		}

		#endregion

		#region Public Properties

		public bool UsingSingleLimb { get; init; }
		public bool UsingDepthFirst { get; init; }
		public bool UseCImplementation { get; init; }
		public string EndPointAddress { get; init; }
		public bool IsLocal => true;

		#endregion

		#region Async Methods

		public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			mapSectionRequest.ClientEndPointAddress = EndPointAddress;

			var stopWatch = Stopwatch.StartNew();
			var mapSectionResponse = await GenerateMapSectionAsyncInternal(mapSectionRequest, ct);
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

			//Debug.Assert(mapSectionResponse.ZValues == null && mapSectionResponse.ZValuesForLocalStorage == null, "The MapSectionResponse includes ZValues.");

			return mapSectionResponse;
		}

		private async Task<MapSectionResponse> GenerateMapSectionAsyncInternal(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			if (DateTime.Now > DateTime.Today.AddDays(1d))
			{
				await Task.Delay(100);
			}

			var mapSectionResponse = _generator.GenerateMapSection(mapSectionRequest, ct);

			if (++_sectionCntr % 10 == 0)
			{
				Debug.WriteLine($"The MEngineClient, {EndPointAddress} has processed {_sectionCntr} requests.");
			}

			//mapSectionResponse.IncludeZValues = false;

			return mapSectionResponse;
		}

		#endregion

		#region Synchronous Methods

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			if (ct.IsCancellationRequested)
			{
				return new MapSectionResponse(mapSectionRequest, isCancelled: true);
			}

			mapSectionRequest.ClientEndPointAddress = EndPointAddress;

			var stopWatch = Stopwatch.StartNew();
			var mapSectionResponse = GenerateMapSectionInternal(mapSectionRequest, ct);
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

			//Debug.Assert(mapSectionResponse.ZValues == null && mapSectionResponse.ZValuesForLocalStorage == null, "The MapSectionResponse includes ZValues.");

			return mapSectionResponse;
		}

		private MapSectionResponse GenerateMapSectionInternal(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			var mapSectionResponse = _generator.GenerateMapSection(mapSectionRequest, ct);

			if (++_sectionCntr % 10 == 0)
			{
				Debug.WriteLine($"The MEngineClient, {EndPointAddress} has processed {_sectionCntr} requests.");
			}

			//mapSectionResponse.IncludeZValues = false;

			return mapSectionResponse;
		}

		#endregion
	}
}
