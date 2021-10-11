using System;
using System.Text.Json.Serialization;
using FSTypes;

  //"mapInfo": {
  //  "name": "DMapInfo3E",
  //  "sCoords": {
  //    "botLeft": {
  //      "x": "-7.50475036374413900508711997650038e-01",
  //      "y": "1.64242643832785547757264761762201e-02"
  //    },
  //    "topRight": {
  //      "x": "-7.50336993123071127028863478243335e-01",
  //      "y": "1.65152406971566978715960616028891e-02"
  //    }
  //  },
  //  "maxIterations": 6000,
  //  "threshold": 4,
  //  "iterationsPerStep": 100,


namespace MFile
{
    public class MapInfo
    {
		[JsonPropertyName("name")]
		public string Name;

		[JsonPropertyName("sCoords")]
        public SCoords Coords;

        [JsonPropertyName("maxIterations")]
        public int MaxIterations;

        private MapInfo()
        {
			Name = null;
            Coords = null;
            MaxIterations = 0;
        }

        public MapInfo(SCoords coords, int maxIterations)
        {
            Coords = coords ?? throw new ArgumentNullException(nameof(coords));
            MaxIterations = maxIterations;
        }
    }
}
