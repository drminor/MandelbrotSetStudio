namespace MFile
{
    public class MapInfoWithColorMap
    {
        public readonly MapInfo MapInfo;
        public readonly ColorMap ColorMap;

        public MapInfoWithColorMap(MapInfo mapInfo, ColorMap colorMap)
        {
            MapInfo = mapInfo;
            ColorMap = colorMap;
        }
    }
}
