using MEngineClient;
using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase
	{
		private const string M_ENGINE_END_POINT_ADDRESS = "https://localhost:5001";

		//private MSetInfo _mSetInfo;
		//public event EventHandler<MapSectionReadyEventArgs> MapSectionReady;

		private Task _generateMapSectionsTask;


		public MainWindowViewModel()
		{
			//_mSetInfo = null;
			_generateMapSectionsTask = null;
		}

		//public MSetInfo MSetInfo
		//{
		//	get => _mSetInfo;
		//	set
		//	{
		//		if (_mSetInfo == null)
		//		{
		//			if (value != null)
		//			{
		//				_mSetInfo = value;
		//				Task.Run(() => GetSectionsAsync(_mSetInfo));
		//			}
		//		}
		//	}
		//}

		public bool IsTaskComplete => _generateMapSectionsTask is null;

		public void GenerateMapSections(MSetInfo mSetInfo, IProgress<MapSectionResponse> progress)
		{
			if (!IsTaskComplete)
			{
				throw new InvalidOperationException("Cannot call GenerateMapSections until the current task is complete.");
			}

			_generateMapSectionsTask = GetSectionsAsync(mSetInfo, progress);
		}

		public async Task GetSectionsAsync(MSetInfo mSetInfo, IProgress<MapSectionResponse> progress)
		{
			var dtoMapper = new DtoMapper();
			var mClient = new MClient(M_ENGINE_END_POINT_ADDRESS);

			var samplePointsDelta = dtoMapper.MapTo(new RSize(BigInteger.One, BigInteger.One, -8));
			var xCoordNumerator = new BigInteger(-4);
			var yCoordNumerartor = new BigInteger(-2);

			int numVertBlocks = 1; // 4;
			int numHoriBlocks = 1; // 6;

			for (int yBlockPtr = 0; yBlockPtr < numVertBlocks; yBlockPtr++)
			{
				for (int xBlockPtr = 0; xBlockPtr < numHoriBlocks; xBlockPtr++)
				{
					var blockPosition = new PointInt(xBlockPtr, yBlockPtr);
					var mapSectionRequest = new MapSectionRequest
					{
						SubdivisionId = blockPosition.ToString(),
						BlockPosition = blockPosition,
						BlockSize = RMapConstants.BLOCK_SIZE,
						Position = dtoMapper.MapTo(new RPoint(xCoordNumerator++, yCoordNumerartor++, 2)),
						SamplePointsDelta = samplePointsDelta,
						MapCalcSettings = mSetInfo.MapCalcSettings
					};

					var mapSectionResponse = await mClient.GenerateMapSectionAsync(mapSectionRequest);
					progress.Report(mapSectionResponse);
				}
			}

			_generateMapSectionsTask = null;
		}

		public MSetInfo BuildInitialMSetInfo()
		{
			var canvasSize = new SizeInt(768, 512);
			var coords = RMapConstants.ENTIRE_SET_RECTANGLE;
			var mapCalcSettings = new MapCalcSettings(maxIterations: 4000, threshold: 4, iterationsPerStep: 100);

			IList<ColorMapEntry> colorMapEntries = new List<ColorMapEntry>
			{
				new ColorMapEntry(375, "#ffffff", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(399, "#fafdf2", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(407, "#98e498", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(428, "#0000ff", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(446, "#f09ee6", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(486, "#00ff00", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(500, "#0000ff", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(523, "#ffffff", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(560, "#3ee2e2", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(1011, "#e95ee8", ColorMapBlendStyle.End, "#758cb7")
			};

			string highColorCss = "#000000";
			var result = new MSetInfo(canvasSize, coords, mapCalcSettings, colorMapEntries, highColorCss);

			return result;
		}
	}
}
