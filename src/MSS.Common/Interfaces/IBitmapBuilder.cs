﻿using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IBitmapBuilder
	{
		long NumberOfCountValSwitches { get; }

		Task<byte[]?> BuildAsync(ObjectId jobId, OwnerType ownerType, MapPositionSizeAndDelta mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings,
			bool useEscapeVelocities, CancellationToken ct, SynchronizationContext synchronizationContext, Action<double>? statusCallback = null);


		//Task FillAsync(WriteableBitmap writeableBitmap, ObjectId jobId, OwnerType ownerType, MapPositionSizeAndDelta mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, 
		//	bool useEscapeVelocities, CancellationToken ct, SynchronizationContext synchronizationContext);

	}
}