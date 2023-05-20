using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class AreaColorAndCalcSettings : ICloneable
	{
		private static readonly Lazy<AreaColorAndCalcSettings> _lazyAreaColorAndCalcSettings = new Lazy<AreaColorAndCalcSettings>(System.Threading.LazyThreadSafetyMode.PublicationOnly);
		public static readonly AreaColorAndCalcSettings Empty = _lazyAreaColorAndCalcSettings.Value;

		public AreaColorAndCalcSettings()
			: this(string.Empty, JobOwnerType.Project, MapAreaInfo2.Empty, new ColorBandSet(), new MapCalcSettings())
		{ }

		public AreaColorAndCalcSettings(string jobId, JobOwnerType jobOwnerType, MapAreaInfo2 mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings)
		{
			JobId = jobId;
			JobOwnerType = jobOwnerType;
			MapAreaInfo = mapAreaInfo;
			ColorBandSet = colorBandSet;
			MapCalcSettings = mapCalcSettings;
		}

		public string JobId { get; init; }
		public JobOwnerType JobOwnerType { get; init; }
		public MapAreaInfo2 MapAreaInfo { get; init; }
		public ColorBandSet ColorBandSet { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }

		public bool IsEmpty => MapAreaInfo.IsEmpty;

		object ICloneable.Clone()
		{
			return Clone();
		}

		public AreaColorAndCalcSettings Clone()
		{
			return new AreaColorAndCalcSettings(JobId, JobOwnerType, MapAreaInfo.Clone(), ColorBandSet.Clone(), MapCalcSettings.Clone());
		}

		public AreaColorAndCalcSettings UpdateWith(MapAreaInfo2 mapAreaInfo)
		{
			return new AreaColorAndCalcSettings(JobId, JobOwnerType, mapAreaInfo.Clone(), ColorBandSet, MapCalcSettings);
		}

		public AreaColorAndCalcSettings UpdateWith(ColorBandSet colorBandSet)
		{
			return new AreaColorAndCalcSettings(JobId, JobOwnerType, MapAreaInfo, colorBandSet, MapCalcSettings);

		}

	}
}
