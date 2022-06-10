using System;

namespace MSS.Types.MSet
{
	public class JobAreaAndCalcSettings : ICloneable
	{
		private static readonly Lazy<JobAreaAndCalcSettings> _lazyJobAreaAndCalcSettings = new Lazy<JobAreaAndCalcSettings>(System.Threading.LazyThreadSafetyMode.PublicationOnly);
		public static readonly JobAreaAndCalcSettings Empty = _lazyJobAreaAndCalcSettings.Value;

		public JobAreaAndCalcSettings()
		{
			JobAreaInfo = JobAreaInfo.Empty;
			MapCalcSettings = new MapCalcSettings();
		}

		public JobAreaAndCalcSettings(JobAreaInfo jobAreaInfo, MapCalcSettings mapCalcSettings)
		{
			JobAreaInfo = jobAreaInfo;
			MapCalcSettings = mapCalcSettings;
		}

		public JobAreaInfo JobAreaInfo { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }

		public bool IsEmpty => JobAreaInfo.IsEmpty;

		object ICloneable.Clone()
		{
			throw new NotImplementedException();
		}

		public JobAreaAndCalcSettings Clone()
		{
			return new JobAreaAndCalcSettings(JobAreaInfo.Clone(), MapCalcSettings.Clone());
		}
	}
}
