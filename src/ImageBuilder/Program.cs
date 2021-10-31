using FSTypes;
using System.IO;

namespace ImageBuilder
{
	class Program
	{
		private const string BASE_PATH = @"C:\_Mbrodts";
		//private const string OLD_JSON_FILES_PATH = @"C:\_Mbrodts\OldJsonMFiles";

		static void Main(string[] args)
		{
			//string fn = "MandlebrodtMapInfo (1)";
			string fn = "Circus1";
			//string fn = "CRhomCenter2";
			//string fn = "SCluster2";

			//isHighRes = true;
			//string fn = "CurRhombus5_2";


			//string path = GetFullPath(BASE_PATH, fn);
			//MFileReconstructor.Recreate(fn, path);

			string path = GetFullPath(BASE_PATH, fn);
			var mongoDbWriter = new MongoDbWriter(MSetConstants.BLOCK_WIDTH, MSetConstants.BLOCK_HEIGHT);
			mongoDbWriter.Build(path);

			//var pngBuilder = new PngBuilder();
			//pngBuilder.Build(fn);
		}

		private static string GetFullPath(string basePath, string fileName)
		{
			string fnWithExt = Path.ChangeExtension(fileName, "json");
			string result = Path.Combine(basePath, fnWithExt);

			return result;
		}

	}
}
