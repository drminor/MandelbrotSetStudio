using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMapSectionAdapter : IMapSectionDuplicator, IMapSectionDeleter
	{
		void CreateCollections();
		//void DropCollections();
		//void DropSubdivisionsAndMapSectionsCollections();

		Task<MapSectionResponse?> GetMapSectionAsync(ObjectId subdivisionId, BigVectorDto blockPosition, bool includeZValues, CancellationToken ct);

		Task<ZValuesDto?> GetMapSectionZValuesAsync(ObjectId mapSectionId);

		Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse);

		Task<ObjectId?> SaveJobMapSectionAsync(MapSectionResponse mapSectionResponse);

		//Task<long?> UpdateMapSectionZValuesAsync(MapSectionServiceResponse mapSectionResponse);

		bool TryGetSubdivision(RSize samplePointDelta, SizeInt blockSize, [MaybeNullWhen(false)] out Subdivision subdivision);
		
		void InsertSubdivision(Subdivision subdivision);
	}

}
