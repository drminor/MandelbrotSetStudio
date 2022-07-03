using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	internal class JobAreaAndCalcSettings : ICloneable
	{
		private static readonly Lazy<JobAreaAndCalcSettings> _lazyJobAreaAndCalcSettings = new Lazy<JobAreaAndCalcSettings>(System.Threading.LazyThreadSafetyMode.PublicationOnly);
		public static readonly JobAreaAndCalcSettings Empty = _lazyJobAreaAndCalcSettings.Value;

		public JobAreaAndCalcSettings()
		{
			MapAreaInfo = MapAreaInfo.Empty;
			MapCalcSettings = new MapCalcSettings();
		}

		public JobAreaAndCalcSettings(MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			MapAreaInfo = mapAreaInfo;
			MapCalcSettings = mapCalcSettings;
		}

		public MapAreaInfo MapAreaInfo { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }

		public bool IsEmpty => MapAreaInfo.IsEmpty;

		object ICloneable.Clone()
		{
			return Clone();
		}

		public JobAreaAndCalcSettings Clone()
		{
			return new JobAreaAndCalcSettings(MapAreaInfo.Clone(), MapCalcSettings.Clone());
		}
	}
}
