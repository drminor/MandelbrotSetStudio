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

		Task<byte[]?> BuildAsync(ObjectId jobId, JobOwnerType ownerType, MapAreaInfo mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, bool useEscapeVelocities, CancellationToken ct, Action<double>? statusCallBack = null);
	}
}