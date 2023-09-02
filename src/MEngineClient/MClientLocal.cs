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

		public MClientLocal(MSetGenerationStrategy mSetGenerationStrategy)
		{
			MSetGenerationStrategy = mSetGenerationStrategy;

			switch (mSetGenerationStrategy)
			{
				case MSetGenerationStrategy.UPointers:
					{
						_generator = new MapSectionGeneratorUPointers(RMapConstants.DEFAULT_LIMB_COUNT, RMapConstants.BLOCK_SIZE);
						EndPointAddress = "CSharp_UPointers";
						break;
					}

				case MSetGenerationStrategy.DepthFirst:
					{
						_generator = new MapSectionGeneratorDepthFirst(RMapConstants.DEFAULT_LIMB_COUNT, RMapConstants.BLOCK_SIZE);
						EndPointAddress = "CSharp_DepthFirstGenerator";
						break;
					}

				case MSetGenerationStrategy.UnManaged:
					throw new NotSupportedException();

				case MSetGenerationStrategy.SingleLimb:
					throw new NotSupportedException();

				case MSetGenerationStrategy.LimbFirst:
					throw new NotSupportedException();

				default:
					throw new NotSupportedException();
			}

			//if (useSingleLimb)
			//{
			//	_generator = new MapSectionGeneratorSingleLimb();
			//	EndPointAddress = "CSharp_SingleLimbGenerator";
			//}
			//else if (UsingDepthFirst)
			//{
			//	_generator = new MapSectionGeneratorDepthFirst(RMapConstants.DEFAULT_LIMB_COUNT, RMapConstants.BLOCK_SIZE, useCImplementation);
			//	EndPointAddress = "CSharp_DepthFirstGenerator";
			//}
			//else
			//{
			//	_generator = new MapSectionGeneratorLimbFirst(RMapConstants.BLOCK_SIZE, RMapConstants.DEFAULT_LIMB_COUNT);
			//	EndPointAddress = "CSharp_LimbFirstGenerator";
			//}
		}

		#endregion

		#region Public Properties

		public MSetGenerationStrategy MSetGenerationStrategy { get; init; }

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
				Debug.WriteLine($"The MClientLocal is skipping request with JobId/Request#: {mapSectionRequest.JobId}/{mapSectionRequest.RequestNumber}.");
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
			try
			{
				var mapSectionResponse = _generator.GenerateMapSection(mapSectionRequest, ct);

				if (++_sectionCntr % 10 == 0)
				{
					Debug.WriteLine($"The MEngineClient, {EndPointAddress} has processed {_sectionCntr} requests.");
				}

				//mapSectionResponse.IncludeZValues = false;

				return mapSectionResponse;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"GenerateMapSectionInternal raised Exception: {e}.");
				throw;
			}
		}

		#endregion
	}
}
