﻿using MSS.Common;
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

		public MClientLocal(int clientNumber, IMapSectionGenerator mapSectionGenerator, MapSectionVectorProvider mapSectionVectorProvider)
		{
			ClientNumber = clientNumber;
			EndPointAddress = "LocalClient";

			//_generator = new MapSectionGeneratorDepthFirst(RMapConstants.DEFAULT_LIMB_COUNT, RMapConstants.BLOCK_SIZE);
			_generator = mapSectionGenerator;

			_mapSectionVectorProvider = mapSectionVectorProvider;
		}

		#endregion

		#region Public Properties

		public int ClientNumber { get; init; }
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

				if (/*_useDetailedDebug && */mapSectionRequest.ScreenPosition.X == 0 && mapSectionRequest.ScreenPosition.Y == 0)
				{
					Debug.WriteLine($"MClientLocal::GenerateMapSection::ScreenPos = 0,0: ZVecs Leased: {_mapSectionVectorProvider.NumberOfMapSectionZVectorsLeased} Vecs Leased: {_mapSectionVectorProvider.NumberOfMapSectionVectorsLeased}; " +
						$"Vecs2 Leased: {_mapSectionVectorProvider.NumberOfMapSectionVectors2Leased}. Number MapSection returns refused: {_mapSectionVectorProvider.NumberOfRefusedMapSectionReturns}.");
				}

				var stopWatch = Stopwatch.StartNew();
				var mapSectionResponse = GenerateMapSectionInternal(mapSectionRequest, ct);
				mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

				if (mapSectionResponse.AllRowsHaveEscaped && mapSectionResponse.MapSectionZVectors != null)
				{
					_mapSectionVectorProvider.ReturnMapSectionZVectors(mapSectionResponse.MapSectionZVectors);
					mapSectionResponse.MapSectionZVectors = null;
				}

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
	}
}
