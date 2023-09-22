using MSetGeneratorPrototype;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Threading;

namespace MEngineClient
{
	public class MClientLocal : IMEngineClient
	{
		private static int _sectionCntr;

		private readonly MapSectionVectorProvider _mapSectionVectorProvider;
		private readonly IMapSectionGenerator _generator;

		private readonly bool _useDetailedDebug = false;

		#region Constructors

		static MClientLocal()
		{
			_sectionCntr = 0;
		}

		public MClientLocal(MSetGenerationStrategy mSetGenerationStrategy, MapSectionVectorProvider mapSectionVectorProvider, int clientNumber)
		{
			MSetGenerationStrategy = mSetGenerationStrategy;
			EndPointAddress = "CSharp_DepthFirstGenerator";

			_mapSectionVectorProvider = mapSectionVectorProvider;
			_generator = new MapSectionGeneratorDepthFirst(RMapConstants.DEFAULT_LIMB_COUNT, RMapConstants.BLOCK_SIZE);
			ClientNumber = clientNumber;	
		}

		#endregion

		#region Public Properties

		public int ClientNumber { get; init; }
		public MSetGenerationStrategy MSetGenerationStrategy { get; init; }
		public string EndPointAddress { get; init; }
		public bool IsLocal => true;

		#endregion

		#region Synchronous Methods

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			if (ct.IsCancellationRequested)
			{
				Debug.WriteLine($"MClientLocal JobId/Request#: {mapSectionRequest.JobId}/{mapSectionRequest.RequestNumber} is cancelled.");
				return new MapSectionResponse(mapSectionRequest, isCancelled: true);
			}
			else
			{
				mapSectionRequest.ClientEndPointAddress = EndPointAddress;

				if (_useDetailedDebug && mapSectionRequest.ScreenPosition.X == 0 && mapSectionRequest.ScreenPosition.Y == 0)
				{
					Debug.WriteLine($"MClientLocal::GenerateMapSection::ScreenPos = 0,0: ZVecs Leased: {_mapSectionVectorProvider.NumberOfMapSectionZVectorsLeased} Vecs Leased: {_mapSectionVectorProvider.NumberOfMapSectionVectorsLeased}; " +
						$"Vecs2 Leased: {_mapSectionVectorProvider.NumberOfMapSectionVectors2Leased}. Number MapSection returns refused: {_mapSectionVectorProvider.NumberOfRefusedMapSectionReturns}.");
				}

				var stopWatch = Stopwatch.StartNew();
				var mapSectionResponse = GenerateMapSectionInternal(mapSectionRequest, ct);
				mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

				return mapSectionResponse;
			}
		}


		public bool CancelGeneration(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			return true;
		}

		private MapSectionResponse GenerateMapSectionInternal(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			try
			{
				if (mapSectionRequest.MapSectionVectors2 == null)
				{
					var mapSectionVectors2 = new MapSectionVectors2(RMapConstants.BLOCK_SIZE);
					mapSectionRequest.MapSectionVectors2 = mapSectionVectors2;
				}

				if (mapSectionRequest.MapCalcSettings.SaveTheZValues && mapSectionRequest.MapSectionZVectors == null)
				{
					var mapSectionZVectors = _mapSectionVectorProvider.ObtainMapSectionZVectors(mapSectionRequest.LimbCount);
					mapSectionZVectors.ResetObject();
					mapSectionRequest.MapSectionZVectors = mapSectionZVectors;
				}

				Debug.WriteLineIf(_useDetailedDebug, $"MClientLocal #{ClientNumber} is starting the call to Generate MapSection: {mapSectionRequest.ScreenPosition}.");
				var mapSectionResponse = _generator.GenerateMapSection(mapSectionRequest, ct);
				Debug.WriteLineIf(_useDetailedDebug, $"MClientLocal #{ClientNumber} is completing the call to Generate MapSection: {mapSectionRequest.ScreenPosition}. Request is Cancelled = {ct.IsCancellationRequested}.");

				if (++_sectionCntr % 10 == 0)
				{
					Debug.WriteLine($"MClientLocal #{ClientNumber} has processed {_sectionCntr} requests.");
				}

				return mapSectionResponse;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"MClientLocal: GenerateMapSectionInternal raised Exception: {e}.");
				throw;
			}
		}

		#endregion

		#region Not Used

		//#region Async Methods

		//public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CancellationToken ct)
		//{
		//	mapSectionRequest.ClientEndPointAddress = EndPointAddress;

		//	var stopWatch = Stopwatch.StartNew();
		//	var mapSectionResponse = await GenerateMapSectionAsyncInternal(mapSectionRequest, ct);
		//	mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

		//	//Debug.Assert(mapSectionResponse.ZValues == null && mapSectionResponse.ZValuesForLocalStorage == null, "The MapSectionResponse includes ZValues.");

		//	return mapSectionResponse;
		//}

		//private async Task<MapSectionResponse> GenerateMapSectionAsyncInternal(MapSectionRequest mapSectionRequest, CancellationToken ct)
		//{
		//	if (DateTime.Now > DateTime.Today.AddDays(1d))
		//	{
		//		await Task.Delay(100);
		//	}

		//	var mapSectionResponse = _generator.GenerateMapSection(mapSectionRequest, ct);

		//	if (++_sectionCntr % 10 == 0)
		//	{
		//		Debug.WriteLine($"The MEngineClient, {EndPointAddress} has processed {_sectionCntr} requests.");
		//	}

		//	//mapSectionResponse.IncludeZValues = false;

		//	return mapSectionResponse;
		//}

		//#endregion

		// Old Constructor
		//public MClientLocal(MSetGenerationStrategy mSetGenerationStrategy)
		//{
		//	MSetGenerationStrategy = mSetGenerationStrategy;

		//	_generator = new MapSectionGeneratorDepthFirst(RMapConstants.DEFAULT_LIMB_COUNT, RMapConstants.BLOCK_SIZE);
		//	EndPointAddress = "CSharp_DepthFirstGenerator";


		//	//switch (mSetGenerationStrategy)
		//	//{
		//	//	case MSetGenerationStrategy.UPointers:
		//	//		{
		//	//			_generator = new MapSectionGeneratorUPointers(RMapConstants.DEFAULT_LIMB_COUNT, RMapConstants.BLOCK_SIZE);
		//	//			EndPointAddress = "CSharp_UPointers";
		//	//			break;
		//	//		}

		//	//	case MSetGenerationStrategy.DepthFirst:
		//	//		{
		//	//			_generator = new MapSectionGeneratorDepthFirst(RMapConstants.DEFAULT_LIMB_COUNT, RMapConstants.BLOCK_SIZE);
		//	//			EndPointAddress = "CSharp_DepthFirstGenerator";
		//	//			break;
		//	//		}

		//	//	case MSetGenerationStrategy.UnManaged:
		//	//		throw new NotSupportedException();

		//	//	case MSetGenerationStrategy.SingleLimb:
		//	//		throw new NotSupportedException();

		//	//	case MSetGenerationStrategy.LimbFirst:
		//	//		throw new NotSupportedException();

		//	//	default:
		//	//		throw new NotSupportedException();
		//	//}

		//	//if (useSingleLimb)
		//	//{
		//	//	_generator = new MapSectionGeneratorSingleLimb();
		//	//	EndPointAddress = "CSharp_SingleLimbGenerator";
		//	//}
		//	//else if (UsingDepthFirst)
		//	//{
		//	//	_generator = new MapSectionGeneratorDepthFirst(RMapConstants.DEFAULT_LIMB_COUNT, RMapConstants.BLOCK_SIZE, useCImplementation);
		//	//	EndPointAddress = "CSharp_DepthFirstGenerator";
		//	//}
		//	//else
		//	//{
		//	//	_generator = new MapSectionGeneratorLimbFirst(RMapConstants.BLOCK_SIZE, RMapConstants.DEFAULT_LIMB_COUNT);
		//	//	EndPointAddress = "CSharp_LimbFirstGenerator";
		//	//}
		//}

		// Old Count+EscapeVelocities Logic

		//private const int VALUE_FACTOR = 10000;
		//public static int[] CombineCountsAndEscapeVelocities(ushort[] counts, ushort[] escapeVelocities)
		//{
		//	var result = new int[counts.Length];

		//	for(var i = 0; i < counts.Length; i++)
		//	{
		//		result[i] = (counts[i] * VALUE_FACTOR) + escapeVelocities[i];
		//	}

		//	return result;
		//}

		//public static ushort[] SplitCountsAndEscapeVelocities(int[] rawCounts, out ushort[] escapeVelocities)
		//{
		//	var result = new ushort[rawCounts.Length];
		//	escapeVelocities = new ushort[rawCounts.Length];

		//	for (var i = 0; i < rawCounts.Length; i++)
		//	{
		//		result[i] = (ushort)Math.DivRem(rawCounts[i], VALUE_FACTOR, out var ev);
		//		escapeVelocities[i] = (ushort)ev;
		//	}

		//	return result;
		//}

		//public static void SplitCountsAndEscapeVelocities(int[] rawCounts, Span<ushort> counts, Span<ushort> escapeVelocities)
		//{
		//	for (var i = 0; i < rawCounts.Length; i++)
		//	{
		//		counts[i] = (ushort)Math.DivRem(rawCounts[i], VALUE_FACTOR, out var ev);
		//		escapeVelocities[i] = (ushort)ev;
		//	}
		//}

		#endregion
	}
}
