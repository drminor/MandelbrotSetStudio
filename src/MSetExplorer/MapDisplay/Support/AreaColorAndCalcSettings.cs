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
			: this(string.Empty, JobOwnerType.Project, MapAreaInfo.Empty, new ColorBandSet(), new MapCalcSettings())
		{ }

		public AreaColorAndCalcSettings(AreaColorAndCalcSettings current, MapCalcSettings newMapCalcSettings) 
			: this(current.OwnerId, current.OwnerType, current.MapAreaInfo, current.ColorBandSet, newMapCalcSettings)
		{ }

		public AreaColorAndCalcSettings(string ownerId, JobOwnerType ownerType, MapAreaInfo mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings)
		{
			OwnerId = ownerId;
			OwnerType = ownerType;
			MapAreaInfo = mapAreaInfo;
			ColorBandSet = colorBandSet;
			MapCalcSettings = mapCalcSettings;
		}

		public string OwnerId { get; init; }
		public JobOwnerType OwnerType { get; init; }
		public MapAreaInfo MapAreaInfo { get; init; }
		public ColorBandSet ColorBandSet { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }

		public bool IsEmpty => MapAreaInfo.IsEmpty;

		object ICloneable.Clone()
		{
			return Clone();
		}

		public AreaColorAndCalcSettings Clone()
		{
			return new AreaColorAndCalcSettings(OwnerId, OwnerType, MapAreaInfo.Clone(), ColorBandSet.Clone(), MapCalcSettings.Clone());
		}
	}
}
