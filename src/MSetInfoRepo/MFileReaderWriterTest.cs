using FSTypes;
using MFile;
using System.Collections.Generic;
using System.IO;

namespace MSetInfoRepo
{
	class MFileReaderWriterTest
	{
		private const string BASE_PATH = @"C:\_Mbrodts";
		private const string OUT_PUT_FOLDER = "TestOutput";

		public static void WriteTest()
		{
			string fileName = "test123";
			var mFileInfo = BuildMFileInfo();
			WriteToJson(mFileInfo, fileName);
		}

		private static void ReadTest()
		{
			string fileName = "test123";
			MFileInfo mFileInfo = ReadFromJson(fileName);
		}

		private static MFileInfo BuildMFileInfo()
		{
			string name = "Test123";

			var sCoords = new Coords("-7.66830587074704020221573662634195e-01", "-7.66830585754868944856241303572093e-01", "1.08316038593833397341534199100796e-01", "1.08316039471787068157292062147129e-01");

			IList<ColorMapEntry> colorMapEntrires = new List<ColorMapEntry>();
			colorMapEntrires.Add(new ColorMapEntry(10, "#f09ee6", ColorMapBlendStyle.None, "#c81788"));
			colorMapEntrires.Add(new ColorMapEntry(10, "#a09ee6", ColorMapBlendStyle.None, "#d81788"));

			string highColor = "#000000";

			MFileInfo result = new MFileInfo(name, sCoords, isHighRes:false, 1000, 4, 100, colorMapEntrires, highColor);

			return result;
		}

		private static void WriteToJson(MFileInfo mFileInfo, string fileName)
		{
			string fnWithExt = Path.ChangeExtension(fileName, "json");
			string path = Path.Combine(BASE_PATH, OUT_PUT_FOLDER, fnWithExt);

			MFileReaderWriter.Write(mFileInfo, path);
		}

		private static MFileInfo ReadFromJson(string fileName)
		{
			string fnWithExt = Path.ChangeExtension(fileName, "json");
			string path = Path.Combine(BASE_PATH, OUT_PUT_FOLDER, fnWithExt);

			MFileInfo mFileInfo = MFileReaderWriter.Read(path);
			return mFileInfo;
		}
	}
}
