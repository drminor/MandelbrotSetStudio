using FSTypes;
using MapSectionRepo;
using MSetInfoRepo;
using ProjectRepo;
using System;
using System.IO;

namespace ImageBuilder
{
	class Program
	{
		private const string BASE_PATH = @"C:\_Mbrodts";
		private const string IMAGE_OUTPUT_FOLDER = @"C:\_Mbrodts";

		private const string MONGO_DB_CONN_STRING = "mongodb://localhost:27017";

		private const int BLOCK_WIDTH = 100;
		private const int BLOCK_HEIGHT = 100;

		private static readonly SizeInt _blockSize = new SizeInt(BLOCK_WIDTH, BLOCK_HEIGHT);

		static void Main(string[] args)
		{
			string fileName = CIRCUS1_PROJECT_NAME;

			int cmd = int.Parse(args[0] ?? "-1");

			switch (cmd)
			{
				case 0:
					{
						/* Reconstructor */
						MSetInfo mSetInfo = MSetInfoBuilder.Recreate(fileName);

						string path = GetFullPath(BASE_PATH, fileName);
						MSetInfoReaderWriter.Write(mSetInfo, path);

						break;
					}
				case 1:
					{
						/* Create png image file */
						var pngBuilder = new PngBuilder(IMAGE_OUTPUT_FOLDER, _blockSize);

						string mFilePath = GetFullPath(BASE_PATH, fileName);
						var mSetInfo = MSetInfoReaderWriter.Read(mFilePath);

						var mapSectionReader = GetMapSectionReader(fileName);

						pngBuilder.Build(mSetInfo, mapSectionReader);
						break;
					}
				case 2:
					{
						/* MongoDb Import */
						IMapSectionReader mapSectionReader = GetMapSectionReader(fileName);

						var dbProvider = new DbProvider(MONGO_DB_CONN_STRING);
						var mongoDbImporter = new MongoDbImporter(dbProvider);

						var project = BuildProject();

						mongoDbImporter.Import(mapSectionReader, project);
						break;
					}

				default: throw new InvalidOperationException($"The value: {cmd} for cmd is not recognized or is not supported.");
			}
		}

		private static IMapSectionReader GetMapSectionReader(string fileName)
		{
			string mFilePath = GetFullPath(BASE_PATH, fileName);
			var mSetInfo = MSetInfoReaderWriter.Read(mFilePath);
			bool isHighRes = mSetInfo.IsHighRes;
			var repofilename = mSetInfo.Name;

			var countsRepoReader = GetReader(repofilename, isHighRes);
			return countsRepoReader;
		}

		private static IMapSectionReader GetReader(string repoFilename, bool isHighRes)
		{
			if (isHighRes)
			{
				return new CountsRepoReaderHiRes(repoFilename, _blockSize);
			}
			else
			{
				return new CountsRepoReader(repoFilename, _blockSize);
			}
		}

		private static Project BuildProject()
		{
			var canvasSize = new SizeInt(1280, 1280);

			Coords coords = new Coords(
				startingX: "-7.66830585754868944856241303572093e-01",
				startingY: "1.08316038593833397341534199100796e-01",
				endingX: "-7.66830587074704020221573662634195e-01",
				endingY: "1.08316039471787068157292062147129e-01"
				);

			var result = new Project("CircusTest", canvasSize, coords);

			return result;
		}

		private static string GetFullPath(string basePath, string fileName)
		{
			string fnWithExt = Path.ChangeExtension(fileName, "json");
			string result = Path.Combine(basePath, fnWithExt);

			return result;
		}


		#region Project Names

		const string CIRCUS1_PROJECT_NAME = "Circus1";
		const string MAP_INFO_1_PROJECT_NAME = "MandlebrodtMapInfo (1)";
		const string CRHOM_CENTER_2_PROJECT_NAME = "CRhomCenter2";
		const string SCLUSTER_2_PROJECT_NAME = "SCluster2";
		const string CUR_RHOMBUS_5_2 = "CurRhombus5_2";

		#endregion
	}
}
