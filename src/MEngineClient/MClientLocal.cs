﻿using MEngineDataContracts;
using MSetGeneratorPrototype;
using MSS.Common;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MEngineClient
{
	public class MClientLocal : IMEngineClient
	{
		private static int _sectionCntr;
		private readonly MapSectionGeneratorSimd _generator;

		#region Constructors

		static MClientLocal()
		{
			_sectionCntr = 0;
		}

		public MClientLocal()
		{
			_generator = new MapSectionGeneratorSimd();		
		}

		#endregion

		#region Public Properties

		public string EndPointAddress => "CSharp_ScalerGenerator";
		public bool IsLocal => true;

		#endregion

		#region Async Methods

		public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest)
		{
			mapSectionRequest.ClientEndPointAddress = EndPointAddress;

			var stopWatch = Stopwatch.StartNew();
			var mapSectionResponse = await GenerateMapSectionAsyncInternal(mapSectionRequest);
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

			Debug.Assert(mapSectionResponse.ZValues == null && mapSectionResponse.ZValuesForLocalStorage == null, "The MapSectionResponse includes ZValues.");

			return mapSectionResponse;
		}

		private async Task<MapSectionResponse> GenerateMapSectionAsyncInternal(MapSectionRequest mapSectionRequest)
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

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			mapSectionRequest.ClientEndPointAddress = EndPointAddress;

			var stopWatch = Stopwatch.StartNew();
			var mapSectionResponse = GenerateMapSectionInternal(mapSectionRequest);
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

			Debug.Assert(mapSectionResponse.ZValues == null && mapSectionResponse.ZValuesForLocalStorage == null, "The MapSectionResponse includes ZValues.");

			return mapSectionResponse;
		}

		private MapSectionResponse GenerateMapSectionInternal(MapSectionRequest mapSectionRequest)
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