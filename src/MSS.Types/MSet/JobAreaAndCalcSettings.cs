using System;

namespace MSS.Types.MSet
{
	public class JobAreaAndCalcSettings : ICloneable
	{
		public JobAreaAndCalcSettings(JobAreaInfo jobAreaInfo, MapCalcSettings mapCalcSettings)
		{
			JobAreaInfo = jobAreaInfo;
			MapCalcSettings = mapCalcSettings;
		}

		public JobAreaInfo JobAreaInfo { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }

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
