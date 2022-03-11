//using MapSectionRepo;
using MEngineClient;
using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace ImageBuilder
{
	class Program
	{
		//private const string BASE_PATH = @"C:\_Mbrodts";
		private const string IMAGE_OUTPUT_FOLDER = @"C:\_Mbrodts";

		private const string MONGO_DB_CONN_STRING = "mongodb://localhost:27017";

		private const string M_ENGINE_END_POINT_ADDRESS = "https://localhost:5001";
		private static readonly SizeInt _legacyBlockSize = new SizeInt(100, 100);
		private static readonly SizeInt _blockSize = RMapConstants.BLOCK_SIZE;

		static void Main(string[] args)
		{
			//int cmd = int.Parse(args[0] ?? "-1");

			int cmd = 5;

			switch (cmd)
			{
				case 0:
					{
						/* Write an MSetInfo to a JSON formatted file. */
						string fileName = MSetInfoBuilder.CIRCUS1_PROJECT_NAME;
						MSetInfoOld mSetInfo = MSetInfoBuilder.Build(fileName);

						//string path = GetFullPath(BASE_PATH, fileName);
						//MSetInfoReaderWriter.Write(mSetInfo, path);

						break;
					}
				case 1:
					{
						/* Create png image file */
						var pngBuilder = new PngBuilder(IMAGE_OUTPUT_FOLDER, _legacyBlockSize);

						string fileName = MSetInfoBuilder.CIRCUS1_PROJECT_NAME;
						var mSetInfo = MSetInfoBuilder.Build(fileName);
						//IMapSectionReader mapSectionReader = GetMapSectionReader(mSetInfo.Name, mSetInfo.IsHighRes);

						//pngBuilder.Build(mSetInfo, mapSectionReader);
						pngBuilder.Build(mSetInfo, null);
						break;
					}
				case 2:
					{
						/* MongoDb Import */
						//var fileName = MSetInfoBuilder.CIRCUS1_PROJECT_NAME;
						//var mSetInfoOld = MSetInfoBuilder.Build(fileName);
						//var cmes = mSetInfoOld.ColorMap.ColorMapEntries;
						//cmes.Add(new ColorMapEntry(mSetInfoOld.MapCalcSettings.MaxIterations, mSetInfoOld.ColorMap.HighColorEntry.StartColor.CssColor, ColorMapBlendStyle.None, mSetInfoOld.ColorMap.HighColorEntry.StartColor.CssColor));
						//var mSetInfo = new MSetInfo(RMapConstants.ENTIRE_SET_RECTANGLE, mSetInfoOld.MapCalcSettings, cmes.ToArray());

						////IMapSectionReader mapSectionReader = GetMapSectionReader(mSetInfoOld.Name, mSetInfoOld.IsHighRes);

						//var projectAdapter = MSetRepoHelper.GetProjectAdapter(MONGO_DB_CONN_STRING);
						//var mongoDbImporter = new MongoDbImporterNotUsed(projectAdapter);

						//var project = projectAdapter.GetOrCreateProject(mSetInfoOld.Name);
						//mongoDbImporter.Import(/*mapSectionReader, */project, mSetInfo, overwrite: true);
						break;
					}
				case 3:
					{
						/* Zoom Test #1*/
						//var fileName = MSetInfoBuilder.ZOOM_TEST_1;
						//var mSetInfoOld = MSetInfoBuilder.Build(fileName);

						//var projectAdapter = MSetRepoHelper.GetProjectAdapter(MONGO_DB_CONN_STRING);
						//var mongoDbImporter = new MongoDbImporterNotUsed(projectAdapter);

						//var project = projectAdapter.GetOrCreateProject(mSetInfoOld.Name);

						//var cmes = mSetInfoOld.ColorMap.ColorMapEntries;
						//cmes.Add(new ColorMapEntry(mSetInfoOld.MapCalcSettings.MaxIterations, mSetInfoOld.ColorMap.HighColorEntry.StartColor.CssColor, ColorMapBlendStyle.None, mSetInfoOld.ColorMap.HighColorEntry.StartColor.CssColor));
						//var mSetInfo = new MSetInfo(RMapConstants.ENTIRE_SET_RECTANGLE, mSetInfoOld.MapCalcSettings, mSetInfoOld.ColorMap.ColorMapEntries.ToArray());

						//mongoDbImporter.DoZoomTest1(project, mSetInfo, overwrite: true);
						break;
					}
				case 4:
					{
						//var mClient = new MClient(M_ENGINE_END_POINT_ADDRESS);

						//var x = mClient.SendHelloAsync().GetAwaiter().GetResult();

						//Debug.WriteLine($"The reply is {x.Message}");

						//Console.WriteLine($"reply is {x.Message}");

						break;
					}
				case 5:
					{
						var mClient = new MClient(M_ENGINE_END_POINT_ADDRESS);
						var request = BuildMapSectionRequest();

						try
						{
							var x = mClient.GenerateMapSectionAsync(request).GetAwaiter().GetResult();

							Debug.WriteLine($"The reply contains {x.Counts.Length} count values.");
							Console.WriteLine($"The reply contains {x.Counts.Length} count values.");
						}
						catch (Exception e)
						{
							Debug.WriteLine($"Got {e.Message}");
						}

						break;
					}

				default: throw new InvalidOperationException($"The value: {cmd} for cmd is not recognized or is not supported.");
			}
		}

		private static MapSectionRequest BuildMapSectionRequest()
		{
			var result = new MapSectionRequest
			{
				SubdivisionId = "TestId",
				BlockPosition = new BigVectorDto(new BigInteger[] { 0, 0 }),
				Position = new RPointDto(new BigInteger[] { 3, 1 }, -2),
				BlockSize = RMapConstants.BLOCK_SIZE,
				SamplePointsDelta = new RSizeDto(new BigInteger[] { 1, 1 }, -8),
				MapCalcSettings = new MapCalcSettings(targetIterations: 100, threshold: 4, iterationsPerRequest: 100)
			};

			//var tSize = new RSizeDto(new BigInteger[] { 11, 12 }, 3);

			//var tSize2 = new RSizeDto(tSize.GetValues(), 33);


			return result;
		}

		//private static IMapSectionReader GetMapSectionReader(string repoFilename, bool isHighRes)
		//{
		//	if (isHighRes)
		//	{
		//		return new MapSectionRepoReaderHiRes(repoFilename, _legacyBlockSize);
		//	}
		//	else
		//	{
		//		return new MapSectionReader(repoFilename, _legacyBlockSize);
		//	}
		//}

		private static Project BuildProject(string projectName)
		{
			var result = new Project(ObjectId.GenerateNewId(), projectName, description: null);

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
