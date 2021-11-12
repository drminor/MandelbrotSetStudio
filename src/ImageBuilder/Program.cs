﻿using MapSectionRepo;
using MEngineClient;
using MEngineDataContracts;
using MongoDB.Bson;
using MSetDatabaseClient;
using MSetRepo;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using ProjectRepo;
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
						MSetInfo mSetInfo = MSetInfoBuilder.Build(fileName);

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
							var x = mClient.SubmitMapSectionRequestAsync(request).GetAwaiter().GetResult();

							Debug.WriteLine($"The reply is {x.Status}");
							Console.WriteLine($"reply is {x.Status}");
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
				BlockPosition = new PointInt(0, 0),
				Position = new RPointDto(new BigInteger[] { 1, 2 }, 0),
				BlockSize = RMapConstants.BLOCK_SIZE,
				SamplePointsDelta = new RSizeDto(new BigInteger[] { 1, 1 }, -11),
				MapCalcSettings = new MapCalcSettings(maxIterations: 300, threshold: 4, iterationsPerStep: 100)
			};

			return result;
		}

		private static IMapSectionReader GetMapSectionReader(string repoFilename, bool isHighRes)
		{
			if (isHighRes)
			{
				return new MapSectionRepoReaderHiRes(repoFilename, _legacyBlockSize);
			}
			else
			{
				return new MapSectionReader(repoFilename, _legacyBlockSize);
			}
		}

		private static MapSectionAdapter GetMapSectionAdapter()
		{
			var dbProvider = new DbProvider(MONGO_DB_CONN_STRING);
			var dtoMapper = new DtoMapper();
			var coordsHelper = new CoordsHelper(dtoMapper);
			var mSetRecordMapper = new MSetRecordMapper(dtoMapper, coordsHelper);
			var mapSectionAdapter = new MapSectionAdapter(dbProvider, mSetRecordMapper, coordsHelper, _blockSize);

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
