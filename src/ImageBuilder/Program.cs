using MFile;
using System.Collections.Generic;
using System.IO;

namespace ImageBuilder
{
	class Program
	{
		public const int BLOCK_SIZE = 100;

		private const string BASE_PATH = @"C:\_Mbrodts";

		static void Main(string[] args)
		{
			bool useHiRez = false;

			//string fn = "MandlebrodtMapInfo (1)";
			//string fn = "Circus1";
			//string fn = "CRhomCenter2";
			string fn = "SCluster2";

			//useHiRez = true;
			//string fn = "CurRhombus5_2";

			var pngBuilder = new PngBuilder(BASE_PATH, BLOCK_SIZE, BLOCK_SIZE);
			pngBuilder.Build(fn, useHiRez);

		}


	}
}
