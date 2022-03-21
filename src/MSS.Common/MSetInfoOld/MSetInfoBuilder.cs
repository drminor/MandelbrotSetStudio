using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.MSetOld;
using System.Collections.Generic;

namespace MSS.Common
{
	public static class MSetInfoBuilder
	{
		#region Project Names

		public const string CIRCUS1_PROJECT_NAME = "Circus1";
		public const string ZOOM_TEST_1 = "ZoomTest1";

		public const string MAP_INFO_1_PROJECT_NAME = "MandlebrodtMapInfo (1)";
		public const string CRHOM_CENTER_2_PROJECT_NAME = "CRhomCenter2";
		public const string SCLUSTER_2_PROJECT_NAME = "SCluster2";
		public const string CUR_RHOMBUS_5_2 = "CurRhombus5_2";

		#endregion

		public static MSetInfoOld Build(string name)
		{
			var info = GetMFileInfo(name);
			return info;
		}

		private static MSetInfoOld GetMFileInfo(string name)
		{
			switch (name)
			{
				case "Circus1":
					{
						return BuildCircus1();
					}
				case "ZoomTest1":
					{
						return BuildZoomTest(name);
					}
				default:
					throw new KeyNotFoundException($"Cound not find a recreation script with name = {name}.");
			}
		}

		private static MSetInfoOld BuildCircus1()
		{
			var apCoords = new ApCoords(
				Sx: -7.66830587074704020221573662634195e-01,
				Ex: -7.66830585754868944856241303572093e-01,

				Sy: 1.08316038593833397341534199100796e-01,
				Ey: 1.08316039471787068157292062147129e-01
				);


			var isHighRes = false;
			var targetIterations = 4000;
			var mapCalcSettings = new MapCalcSettings(targetIterations, iterationsPerRequest: 1000);

			var colorBands = new List<ColorBandW>
			{
				new ColorBandW(375, "#ffffff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBandW(399, "#fafdf2", ColorBandBlendStyle.Next, "#000000"),
				new ColorBandW(407, "#98e498", ColorBandBlendStyle.Next, "#000000"),
				new ColorBandW(428, "#0000ff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBandW(446, "#f09ee6", ColorBandBlendStyle.Next, "#000000"),
				new ColorBandW(486, "#00ff00", ColorBandBlendStyle.Next, "#000000"),
				new ColorBandW(500, "#0000ff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBandW(523, "#ffffff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBandW(560, "#3ee2e2", ColorBandBlendStyle.Next, "#000000"),
				new ColorBandW(1011, "#e95ee8", ColorBandBlendStyle.End, "#758cb7")
			};

			var highColorCss = "#000000";
			colorBands.Add(new ColorBandW(targetIterations, highColorCss, ColorBandBlendStyle.None, highColorCss));
			var colorBandSet = new ColorBandSetW(colorBands);
			var colorMap = new ColorMap(colorBandSet);
			var result = new MSetInfoOld("Circus1", apCoords, isHighRes, mapCalcSettings, colorMap);

			return result;
		}

		private static MSetInfoOld BuildZoomTest(string projectName)
		{
			var apCoords = new ApCoords(
				Sx: -7.66830587074704020221573662634195e-01,
				Ex: -7.66830585754868944856241303572093e-01,

				Sy: 1.08316038593833397341534199100796e-01,
				Ey: 1.08316039471787068157292062147129e-01
				);


			var isHighRes = false;
			var targetIterations = 400;
			var mapCalcSettings = new MapCalcSettings(targetIterations, iterationsPerRequest: 100);
			var colorBands = new List<ColorBandW>
			{
				new ColorBandW(375, "#ffffff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBandW(1011, "#e95ee8", ColorBandBlendStyle.End, "#758cb7")
			};

			var highColorCss = "#000000";
			colorBands.Add(new ColorBandW(targetIterations, highColorCss, ColorBandBlendStyle.None, highColorCss));
			var colorBandSet = new ColorBandSetW(colorBands);
			var colorMap = new ColorMap(colorBandSet);
			var result = new MSetInfoOld(projectName, apCoords, isHighRes, mapCalcSettings, colorMap);

			return result;
		}

	}
}
