using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ImageBuilderWPF
{
	public interface IImageBuilderWPF
	{
		long NumberOfCountValSwitches { get; }

		//Task<bool> BuildAsync(IImageWriter imageWriter, ObjectId jobId, OwnerType ownerType, MapPositionSizeAndDelta mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings,
		//	bool useEscapeVelocities, CancellationToken ct, Action<double>? statusCallback);

		Task<bool> FillAsync(IImageWriter imageWriter, ObjectId jobId, OwnerType ownerType, MapPositionSizeAndDelta mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings,
			bool useEscapeVelocities, CancellationToken ct, Action<double>? statusCallback = null);
	}
}