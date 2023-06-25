using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IImageBuilder
	{
		long NumberOfCountValSwitches { get; }

		Task<bool> BuildAsync(string imageFilePath, ObjectId jobId, MapAreaInfo mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, Action<double> statusCallBack, CancellationToken ct);
	}
}