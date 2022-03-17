
namespace MSS.Types.MSet
{
	public class MSetInfo
	{
		public RRectangle Coords { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }

		public MSetInfo(RRectangle coords, MapCalcSettings mapCalcSettings)
		{
			Coords = coords;
			MapCalcSettings = mapCalcSettings;
		}

		public static MSetInfo UpdateWithNewCoords(MSetInfo source, RRectangle newCoords)
		{
			return new MSetInfo(newCoords.Clone(), source.MapCalcSettings);
		}

		public static MSetInfo UpdateWithNewIterations(MSetInfo source, int targetIterations, int iterationsPerRequest)
		{
			return new MSetInfo(source.Coords.Clone(), new MapCalcSettings(targetIterations, iterationsPerRequest));
		}

	}
}
