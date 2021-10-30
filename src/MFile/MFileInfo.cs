using System.Collections.Generic;

namespace MFile
{
	public record MFileInfo(string Name, SCoords Coords, int MaxIterations, int Threshold, int InterationsPerStep, 
		IList<ColorMapEntry> ColorMapEntries, string HighColorCss)
	{
	}

}


//{ 
//"Name": "DMapInfo3E",
//"MapInfo": {
//  "Coords": {
//    "BotLeft": {
//      "X": "-7.50475036374413900508711997650038e-01",
//      "Y": "1.64242643832785547757264761762201e-02"
//    },
//    "TopRight": {
//      "X": "-7.50336993123071127028863478243335e-01",
//      "Y": "1.65152406971566978715960616028891e-02"
//    }
//  },
//  "MaxIterations": 6000,
//  "Threshold": 4,
//  "IterationsPerStep": 100
//  }
//}
