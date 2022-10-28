using Grpc.Net.Client;
using MEngineDataContracts;
using MSS.Common;
using ProtoBuf.Grpc.Client;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MEngineClient
{
	public class MClient : IMEngineClient
	{
		private GrpcChannel? _grpcChannel;

		public MClient(string endPointAddress)
		{
			EndPointAddress = endPointAddress;
			_grpcChannel = null;
		}

		public string EndPointAddress { get; init; }
		public bool IsLocal => false;

		//public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest)
		//{
		//	var mEngineService = GetMapSectionService();
		//	var reply = await mEngineService.GenerateMapSectionAsync(mapSectionRequest);
		//	return reply;
		//}

		public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest)
		{
			var mEngineService = GetMapSectionService();
			mapSectionRequest.ClientEndPointAddress = EndPointAddress;

			var stopWatch = Stopwatch.StartNew();
			var mapSectionResponse = await mEngineService.GenerateMapSectionAsync(mapSectionRequest);
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

			Debug.Assert(mapSectionResponse.ZValues == null && mapSectionResponse.ZValuesForLocalStorage == null, "The MapSectionResponse includes ZValues.");

			return mapSectionResponse;
		}

		//public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		//{
		//	var mEngineService = GetMapSectionService();
		//	mapSectionRequest.ClientEndPointAddress = EndPointAddress;

		//	var stopWatch = Stopwatch.StartNew();
		//	var reply = mEngineService.GenerateMapSection(mapSectionRequest);
		//	mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

		//	return reply;
		//}

		private IMapSectionService GetMapSectionService()
		{
			try
			{
				var client = Channel.CreateGrpcService<IMapSectionService>();
				return client;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Got exc: {e.GetType()}:{e.Message}");
				throw;
			}
		}

		private GrpcChannel Channel
		{
			get
			{
				if (_grpcChannel == null)
				{
					_grpcChannel = GrpcChannel.ForAddress(EndPointAddress);
				}

				return _grpcChannel;
			}
		}
	}
}
