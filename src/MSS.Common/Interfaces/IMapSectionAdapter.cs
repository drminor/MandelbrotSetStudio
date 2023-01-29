using MongoDB.Bson;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System;
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

		Task<MapSectionResponse?> GetMapSectionAsync(ObjectId subdivisionId, BigVectorDto blockPosition, CancellationToken ct, Func<MapSectionValues> allocateMsvBuf);

		//Task<ValueTuple<byte[], byte[]>?> GetMapSectionZValuesAsync(ObjectId mapSectionId);
		Task<ZValues?> GetMapSectionZValuesAsync(ObjectId mapSectionId);

		Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse);

		Task<ObjectId?> SaveJobMapSectionAsync(MapSectionResponse mapSectionResponse);

		//Task<long?> UpdateMapSectionZValuesAsync(MapSectionServiceResponse mapSectionResponse);

		bool TryGetSubdivision(RSize samplePointDelta, SizeInt blockSize, [MaybeNullWhen(false)] out Subdivision subdivision);
		
		void InsertSubdivision(Subdivision subdivision);
	}

}
