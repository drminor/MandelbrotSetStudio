﻿using MongoDB.Bson;
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
		void DropMapSections();
		void DropMapSectionsAndSubdivisions();

		Task<MapSectionResponse?> GetMapSectionAsync(ObjectId subdivisionId, BigVectorDto blockPosition, CancellationToken ct, MapSectionVectors mapSectionVectors);

		Task<ZValues?> GetMapSectionZValuesAsync(ObjectId mapSectionId);

		Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse);

		Task<ObjectId?> SaveJobMapSectionAsync(MapSectionResponse mapSectionResponse);

		Task<long?> UpdateMapSectionZValuesAsync(MapSectionResponse mapSectionResponse);

		bool TryGetSubdivision(RSize samplePointDelta, BigVector baseMapPosition, [MaybeNullWhen(false)] out Subdivision subdivision);
		
		Subdivision InsertSubdivision(Subdivision subdivision);
	}

}
