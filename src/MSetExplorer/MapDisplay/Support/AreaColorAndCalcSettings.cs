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
			: this(string.Empty, OwnerType.Project, MapCenterAndDelta.Empty, new ColorBandSet(), new MapCalcSettings())
		{ }

		public AreaColorAndCalcSettings(string jobId, OwnerType jobOwnerType, MapCenterAndDelta mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings)
		{
			JobId = jobId;
			JobOwnerType = jobOwnerType;
			MapAreaInfo = mapAreaInfo;
			ColorBandSet = colorBandSet;
			MapCalcSettings = mapCalcSettings;
		}

		public string JobId { get; init; }
		public OwnerType JobOwnerType { get; init; }
		public MapCenterAndDelta MapAreaInfo { get; init; }
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

		public AreaColorAndCalcSettings UpdateWith(MapCenterAndDelta mapAreaInfo)
		{
			return new AreaColorAndCalcSettings(JobId, JobOwnerType, mapAreaInfo.Clone(), ColorBandSet, MapCalcSettings);
		}

		public AreaColorAndCalcSettings UpdateWith(ColorBandSet colorBandSet)
		{
			return new AreaColorAndCalcSettings(JobId, JobOwnerType, MapAreaInfo, colorBandSet, MapCalcSettings);
		}

		public AreaColorAndCalcSettings UpdateWith(MapCalcSettings mapCalcSettings)
		{
			return new AreaColorAndCalcSettings(JobId, JobOwnerType, MapAreaInfo, ColorBandSet, mapCalcSettings);
		}

	}
}
