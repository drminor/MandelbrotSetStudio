using System.IO;

namespace ImageBuilder
{
	class Program
	{
		public const int BLOCK_SIZE = 100;

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
			MongoDbWriter.Build(path);


			//var pngBuilder = new PngBuilder(BASE_PATH, BLOCK_SIZE, BLOCK_SIZE);
			//pngBuilder.Build(fn);


		}

		private static string GetFullPath(string basePath, string fn)
		{
			string fnWithExt = Path.ChangeExtension(fn, "json");
			string path = Path.Combine(basePath, fnWithExt);
			return path;
		}



	}
}
