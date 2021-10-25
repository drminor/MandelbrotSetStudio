using MFile;
using System.Collections.Generic;
using System.IO;

namespace ImageBuilder
{
	class Program
	{
		public const int BLOCK_SIZE = 100;

		private const string BASE_PATH = @"C:\_Mbrodts";
		private const string OUT_PUT_FOLDER = "TestOutput";


		//static void Main(string[] args)
		//{
		//	bool useHiRez = false;

		//	//string fn = "MandlebrodtMapInfo (1)";
		//	//string fn = "Circus1";
		//	//string fn = "CRhomCenter2";
		//	string fn = "SCluster2";

		//	//useHiRez = true;
		//	//string fn = "CurRhombus5_2";

		//	var pngBuilder = new PngBuilder(BasePath, BLOCK_SIZE, BLOCK_SIZE);
		//	pngBuilder.Build(fn, useHiRez);

		//}


		static void Main(string[] args)
		{
			string fileName = "test123";
			var mFileInfo = BuildMFileInfo();
			WriteToJson(mFileInfo, fileName);
		}

		private static MFileInfo BuildMFileInfo()
		{
			string name = "Test123";

			var sCoords = new SCoords("-7.66830587074704020221573662634195e-01", "-7.66830585754868944856241303572093e-01", "1.08316038593833397341534199100796e-01", "1.08316039471787068157292062147129e-01");

			IList<ColorMapEntry> colorMapEntrires = new List<ColorMapEntry>();

			colorMapEntrires.Add(new ColorMapEntry(10, "#f09ee6", ColorMapEntry.BLEND_STYLE_NONE, "#c81788"));
			colorMapEntrires.Add(new ColorMapEntry(10, "#a09ee6", ColorMapEntry.BLEND_STYLE_NONE, "#d81788"));

			string highColor = "#000000";


			MFileInfo result = new MFileInfo(name, sCoords, 1000, 4, 100, colorMapEntrires, highColor);

			return result;
		}

		private static void WriteToJson(MFileInfo mFileInfo, string fileName)
		{
			string fnWithExt = Path.ChangeExtension(fileName, "json");
			string path = Path.Combine(BASE_PATH, OUT_PUT_FOLDER, fnWithExt);

			var mapInfoReaderWriter = new MFileReaderWriter();
			string jsonContent = mapInfoReaderWriter.Write(mFileInfo);

			File.WriteAllText(path, jsonContent);
		}

		//private MapInfoWithColorMap ReadFromJson(string fn)
		//{
		//	string fnWithExt = Path.ChangeExtension(fn, "json");
		//	string path = Path.Combine(BasePath, fnWithExt);

		//	var mapInfoReaderWriter = new MapInfoReaderWriter();
		//	MapInfoWithColorMap miwcm = mapInfoReaderWriter.Read(path);
		//	return miwcm;
		//}


	}
}
