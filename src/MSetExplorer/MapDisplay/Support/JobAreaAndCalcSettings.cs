using MSS.Types;
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
			OwnerId = string.Empty;
			OwnerType = JobOwnerType.Project;
			MapAreaInfo = MapAreaInfo.Empty;
			MapCalcSettings = new MapCalcSettings();
		}

		public JobAreaAndCalcSettings(string ownerId, JobOwnerType ownerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			OwnerId = ownerId;
			OwnerType = ownerType;
			MapAreaInfo = mapAreaInfo;
			MapCalcSettings = mapCalcSettings;
		}

		public JobAreaAndCalcSettings(JobAreaAndCalcSettings current, MapCalcSettings newMapCalcSettings) : this(current.OwnerId, current.OwnerType, current.MapAreaInfo, newMapCalcSettings)
		{ }

		public string OwnerId { get; init; }
		public JobOwnerType OwnerType { get; init; }
		public MapAreaInfo MapAreaInfo { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }

		public bool IsEmpty => MapAreaInfo.IsEmpty;

		object ICloneable.Clone()
		{
			return Clone();
		}

		public JobAreaAndCalcSettings Clone()
		{
			return new JobAreaAndCalcSettings(OwnerId, OwnerType, MapAreaInfo.Clone(), MapCalcSettings.Clone());
		}
	}
}
