using MEngineDataContracts;
using MSS.Common;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MSetGenP
{
	public class MClientLocalVector : IMEngineClient
	{
		private static int _sectionCntr;
		private readonly MapSectionGeneratorVector _generator;

		static MClientLocalVector()
		{
			_sectionCntr = 0;
		}

		public MClientLocalVector()
		{
			_generator = new MapSectionGeneratorVector();		
		}

		public string EndPointAddress => "CSharp_ScalerGenerator";
		public bool IsLocal => true;

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
