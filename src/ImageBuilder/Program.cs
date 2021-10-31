using FSTypes;
using System.IO;

namespace ImageBuilder
{
	class Program
	{
		private const string BASE_PATH = @"C:\_Mbrodts";
		//private const string IMAGE_OUTPUT_FOLDER = @"C:\_Mbrodts";

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
			var mongoDbWriter = new MongoDbImporter(MSetConstants.BLOCK_WIDTH, MSetConstants.BLOCK_HEIGHT);
			mongoDbWriter.Import(path);

			//var pngBuilder = new PngBuilder(IMAGE_OUTPUT_FOLDER, MSetConstants.BLOCK_WIDTH, MSetConstants.BLOCK_HEIGHT);
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
