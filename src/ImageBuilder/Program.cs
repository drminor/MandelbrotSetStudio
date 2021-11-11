using MSS.Types;
using MapSectionRepo;
using MSetInfoRepo;
using ProjectRepo;
using System;
using System.IO;
using MSS.Common;
using MSetDatabaseClient;
using MSS.Types.MSetRepo;
using MSS.Common.MSetRepo;
using MSS.Types.Base;
using MSetRepo;
using MEngineClient;
using System.Diagnostics;
using MSS.Types.MSet;
using MongoDB.Bson;
using MSS.Common.DataTransferObjects;
using MSS.Types.DataTransferObjects;

namespace ImageBuilder
{
	class Program
	{
		//private const string BASE_PATH = @"C:\_Mbrodts";
		private const string IMAGE_OUTPUT_FOLDER = @"C:\_Mbrodts";

		private const string MONGO_DB_CONN_STRING = "mongodb://localhost:27017";

		private const string M_ENGINE_END_POINT_ADDRESS = "https://localhost:5001";


		private const int BLOCK_WIDTH = 100;
		private const int BLOCK_HEIGHT = 100;

		private static readonly SizeInt _blockSize = new SizeInt(BLOCK_WIDTH, BLOCK_HEIGHT);

		static void Main(string[] args)
		{
			//int cmd = int.Parse(args[0] ?? "-1");

			int cmd = 4;

			switch (cmd)
			{
				case 0:
					{
						/* Write an MSetInfo to a JSON formatted file. */
						string fileName = MSetInfoBuilder.CIRCUS1_PROJECT_NAME;
						MSetInfo mSetInfo = MSetInfoBuilder.Build(fileName);

						//string path = GetFullPath(BASE_PATH, fileName);
						//MSetInfoReaderWriter.Write(mSetInfo, path);

						break;
					}
				case 1:
					{
						/* Create png image file */
						var pngBuilder = new PngBuilder(IMAGE_OUTPUT_FOLDER, _blockSize);

						string fileName = MSetInfoBuilder.CIRCUS1_PROJECT_NAME;
						var mSetInfo = MSetInfoBuilder.Build(fileName);
						IMapSectionReader mapSectionReader = GetMapSectionReader(mSetInfo.Name, mSetInfo.IsHighRes);

						pngBuilder.Build(mSetInfo, mapSectionReader);
						break;
					}
				case 2:
					{
						/* MongoDb Import */
						string fileName = MSetInfoBuilder.CIRCUS1_PROJECT_NAME;
						var mSetInfo = MSetInfoBuilder.Build(fileName);

						IMapSectionReader mapSectionReader = GetMapSectionReader(mSetInfo.Name, mSetInfo.IsHighRes);

						var mapSectionAdapter = GetMapSectionAdapter();
						var mongoDbImporter = new MongoDbImporter(mapSectionAdapter);

						var project = BuildProject(mSetInfo.Name);
						mongoDbImporter.Import(mapSectionReader, project, mSetInfo, overwrite: true);
						break;
					}
				case 3:
					{
						/* Zoom Test #1*/
						string fileName = MSetInfoBuilder.ZOOM_TEST_1;
						var mSetInfo = MSetInfoBuilder.Build(fileName);

						var mapSectionAdapter = GetMapSectionAdapter();
						var mongoDbImporter = new MongoDbImporter(mapSectionAdapter);

						var project = BuildProject(mSetInfo.Name);
						mongoDbImporter.DoZoomTest1(project, mSetInfo, overwrite: true);
						break;
					}
				case 4:
					{
						var mClient = new MClient(M_ENGINE_END_POINT_ADDRESS);

						var x = mClient.SendHelloAsync().GetAwaiter().GetResult();

						Debug.WriteLine($"The reply is {x.Message}");

						Console.WriteLine($"reply is {x.Message}");

						break;
					}
				default: throw new InvalidOperationException($"The value: {cmd} for cmd is not recognized or is not supported.");
			}
		}

		private static IMapSectionReader GetMapSectionReader(string repoFilename, bool isHighRes)
		{
			if (isHighRes)
			{
				return new MapSectionRepoReaderHiRes(repoFilename, _blockSize);
			}
			else
			{
				return new MapSectionReader(repoFilename, _blockSize);
			}
		}

		private static MapSectionAdapter GetMapSectionAdapter()
		{
			var dbProvider = new DbProvider(MONGO_DB_CONN_STRING);

			IMapper<RRectangle, RRectangleDto> dtoMapper = new DtoMapper();
			var coordsHelper = new CoordsHelper(dtoMapper);
			var mSetRecordMapper = new MSetRecordMapper(dtoMapper, coordsHelper);
			var mapSectionAdapter = new MapSectionAdapter(dbProvider, mSetRecordMapper, coordsHelper);

			return mapSectionAdapter;
		}

		private static Project BuildProject(string projectName)
		{
			var result = new Project(ObjectId.GenerateNewId(), projectName);

			return result;
		}

		private static string GetFullPath(string basePath, string fileName)
		{
			string fnWithExt = Path.ChangeExtension(fileName, "json");
			string result = Path.Combine(basePath, fnWithExt);

			return result;
		}

	}
}
