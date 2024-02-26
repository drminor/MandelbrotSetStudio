using MSS.Types;
using System.Diagnostics;

namespace PercentageBandsTest
{
	public class RoundTripCbsTest
	{
		#region Tests

		[Fact]
		public void UsePercentages_HavePercentages()
		{
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(maxIterations: 400, usePercentages: true);
			var histogram = GetHistogram1();

			ApplyHistogram(colorBandSet, histogram, usePercentages: true);

			ApplyHistogram(colorBandSet, histogram, usePercentages: true);
		}

		[Fact]
		public void UsePercentages_DoNotHavePercentages()
		{
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(maxIterations: 400, usePercentages: false);
			var histogram = GetHistogram1();

			ApplyHistogram(colorBandSet, histogram, usePercentages: true);

			ApplyHistogram(colorBandSet, histogram, usePercentages: true);
		}

		[Fact]
		public void UseCutoffs()
		{
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(maxIterations: 400, usePercentages: false);
			var histogram = GetHistogram1();

			ApplyHistogram(colorBandSet, histogram, usePercentages: false);

			ApplyHistogram(colorBandSet, histogram, usePercentages: false);
		}

		#endregion

		#region Support Methods

		private bool ApplyHistogram(ColorBandSet colorBandSet, IHistogram histogram, bool usePercentages)
		{
			var histCutoffsSnapShot = GetHistCutoffsSnapShot(histogram, colorBandSet);

			var result = ApplyHistogram(colorBandSet, histCutoffsSnapShot, usePercentages);
			return result;
		}

		private bool ApplyHistogram(ColorBandSet colorBandSet, HistCutoffsSnapShot histCutoffsSnapShot, bool usePercentages)
		{
			if (histCutoffsSnapShot.HistKeyValuePairs.Length > 0)
			{
				if (usePercentages && histCutoffsSnapShot.HavePercentages)
				{
					// Cutoffs are adjusted based on Percentages
					UpdateCutoffs(colorBandSet, histCutoffsSnapShot);
				}
				else
				{
					// Percentages are adjusted based on Cutoffs
					UpdatePercentages(colorBandSet, histCutoffsSnapShot);
				}

				return true;
			}
			else
			{
				ClearPercentages(colorBandSet);
				return false;
			}
		}

		private PercentageBand[] UpdatePercentages(ColorBandSet colorBandSet, HistCutoffsSnapShot histCutoffsSnapShot)
		{
			if (ColorBandSetHelper.TryGetPercentagesFromCutoffs(histCutoffsSnapShot, out var newPercentages))
			{
				ApplyNewPercentages(colorBandSet, newPercentages);
				return newPercentages;
			}
			else
			{
				var result = ColorBandSetHelper.GetPercentageBands(colorBandSet); // TODO: This is a Hack.
				return result;
			}
		}

		private void ApplyNewPercentages(ColorBandSet colorBandSet, PercentageBand[] newPercentages)
		{
			colorBandSet.UpdatePercentagesNoCheck(newPercentages);
		}

		private void ClearPercentages(ColorBandSet colorBandSet)
		{
			colorBandSet.ClearPercentages();
		}

		private void UpdateCutoffs(ColorBandSet colorBandSet, HistCutoffsSnapShot histCutoffsSnapShot)
		{
			if (ColorBandSetHelper.TryGetCutoffsFromPercentages(histCutoffsSnapShot, out var newCutoffBands))
			{

				ColorBandSetHelper.CheckNewCutoffs(histCutoffsSnapShot.PercentageBands, newCutoffBands);
				ColorBandSetHelper.ReportNewCutoffs(histCutoffsSnapShot.PercentageBands, newCutoffBands);

				ApplyNewCutoffs(colorBandSet, newCutoffBands);
			}
		}

		private void ApplyNewCutoffs(ColorBandSet colorBandSet, CutoffBand[] newCutoffs)
		{
			colorBandSet.UpdateCutoffs(newCutoffs);
		}

		private HistCutoffsSnapShot GetHistCutoffsSnapShot(IHistogram histogram, ColorBandSet colorBandSet)
		{
			HistCutoffsSnapShot result;

			result = new HistCutoffsSnapShot(
				histogram.GetKeyValuePairs(),
				histogram.Length,
				histogram.UpperCatchAllValue,
				colorBandSet.HavePercentages,
				ColorBandSetHelper.GetPercentageBands(colorBandSet)
			);

			return result;
		}

		#endregion

		#region DATA

		//private HistogramA GetHistogramZero()
		//{
		//	var entries = new Dictionary<int, int>
		//	{
		//		{ 0, 0 },
		//	};

		//	HistogramA result = new HistogramA(entries);

		//	return result;
		//}

		private HistogramA GetHistogram1()
		{
			IDictionary<int, int> entries = new Dictionary<int, int>()
			{
				{2, 108},
				{3, 111172},
				{4, 133450},
				{5, 122464},
				{6, 105286},
				{7, 87898},
				{8, 71392},
				{9, 58462},
				{10, 47588},
				{11, 38054},
				{12, 30784},
				{13, 24694},
				{14, 19954},
				{15, 16446},
				{16, 13116},
				{17, 10622},
				{18, 8460},
				{19, 7086},
				{20, 5594},
				{21, 4468},
				{22, 3788},
				{23, 3156},
				{24, 2622},
				{25, 2232},
				{26, 1884},
				{27, 1552},
				{28, 1322},
				{29, 1160},
				{30, 1056},
				{31, 786},
				{32, 676},
				{33, 642},
				{34, 628},
				{35, 488},
				{36, 486},
				{37, 388},
				{38, 356},
				{39, 312},
				{40, 280},
				{41, 224},
				{42, 248},
				{43, 258},
				{44, 190},
				{45, 214},
				{46, 224},
				{47, 222},
				{48, 200},
				{49, 206},
				{50, 128},
				{51, 160},
				{52, 158},
				{53, 124},
				{54, 136},
				{55, 112},
				{56, 110},
				{57, 112},
				{58, 100},
				{59, 88},
				{60, 90},
				{61, 106},
				{62, 96},
				{63, 84},
				{64, 72},
				{65, 92},
				{66, 60},
				{67, 50},
				{68, 106},
				{69, 82},
				{70, 74},
				{71, 62},
				{72, 70},
				{73, 58},
				{74, 64},
				{75, 42},
				{76, 64},
				{77, 40},
				{78, 54},
				{79, 54},
				{80, 46},
				{81, 52},
				{82, 48},
				{83, 36},
				{84, 40},
				{85, 40},
				{86, 36},
				{87, 54},
				{88, 46},
				{89, 40},
				{90, 44},
				{91, 36},
				{92, 44},
				{93, 32},
				{94, 22},
				{95, 36},
				{96, 16},
				{97, 36},
				{98, 40},
				{99, 26},
				{100, 26},
				{101, 22},
				{102, 26},
				{103, 24},
				{104, 32},
				{105, 32},
				{106, 28},
				{107, 34},
				{108, 20},
				{109, 28},
				{110, 18},
				{111, 24},
				{112, 30},
				{113, 26},
				{114, 22},
				{115, 20},
				{116, 38},
				{117, 24},
				{118, 16},
				{119, 22},
				{120, 18},
				{121, 8},
				{122, 22},
				{123, 18},
				{124, 36},
				{125, 18},
				{126, 22},
				{127, 14},
				{128, 14},
				{129, 16},
				{130, 24},
				{131, 34},
				{132, 14},
				{133, 18},
				{134, 14},
				{135, 20},
				{136, 20},
				{137, 14},
				{138, 14},
				{139, 18},
				{140, 10},
				{141, 10},
				{142, 18},
				{143, 10},
				{144, 24},
				{145, 10},
				{146, 20},
				{147, 18},
				{148, 6},
				{149, 16},
				{150, 16},
				{151, 10},
				{152, 10},
				{153, 10},
				{154, 10},
				{155, 12},
				{156, 18},
				{157, 6},
				{158, 6},
				{159, 14},
				{160, 12},
				{161, 4},
				{162, 10},
				{163, 12},
				{164, 12},
				{165, 6},
				{166, 8},
				{167, 14},
				{168, 8},
				{169, 14},
				{170, 14},
				{171, 6},
				{172, 2},
				{173, 10},
				{174, 4},
				{175, 18},
				{176, 6},
				{177, 10},
				{178, 16},
				{179, 2},
				{180, 8},
				{181, 16},
				{182, 14},
				{183, 6},
				{184, 8},
				{185, 10},
				{186, 4},
				{187, 12},
				{188, 10},
				{189, 4},
				{190, 8},
				{191, 6},
				{192, 4},
				{193, 10},
				{194, 4},
				{195, 8},
				{196, 8},
				{197, 6},
				{198, 8},
				{199, 6},
				{200, 2},
				{201, 4},
				{202, 4},
				{203, 2},
				{204, 10},
				{205, 6},
				{206, 4},
				{207, 10},
				{208, 12},
				{209, 4},
				{210, 8},
				{211, 6},
				{212, 10},
				{213, 2},
				{214, 6},
				{215, 12},
				{216, 2},
				{217, 4},
				{218, 6},
				{219, 2},
				{220, 6},
				{222, 8},
				{223, 6},
				{224, 2},
				{225, 6},
				{226, 2},
				{227, 4},
				{228, 6},
				{229, 2},
				{230, 10},
				{231, 6},
				{232, 4},
				{233, 8},
				{234, 4},
				{235, 4},
				{236, 12},
				{237, 8},
				{238, 4},
				{239, 6},
				{240, 2},
				{241, 2},
				{242, 8},
				{243, 2},
				{244, 6},
				{245, 8},
				{248, 2},
				{249, 4},
				{250, 10},
				{251, 6},
				{252, 4},
				{253, 8},
				{254, 2},
				{255, 8},
				{256, 12},
				{257, 4},
				{259, 2},
				{260, 2},
				{261, 2},
				{262, 4},
				{263, 4},
				{264, 10},
				{265, 4},
				{266, 2},
				{267, 2},
				{268, 6},
				{270, 2},
				{272, 6},
				{273, 4},
				{274, 4},
				{275, 4},
				{276, 4},
				{277, 2},
				{278, 8},
				{280, 2},
				{281, 2},
				{282, 2},
				{283, 10},
				{284, 6},
				{285, 2},
				{286, 4},
				{287, 8},
				{289, 2},
				{292, 2},
				{294, 4},
				{295, 4},
				{297, 4},
				{299, 4},
				{300, 2},
				{301, 4},
				{302, 2},
				{303, 2},
				{306, 2},
				{307, 2},
				{308, 2},
				{309, 4},
				{312, 2},
				{313, 4},
				{314, 2},
				{315, 6},
				{316, 6},
				{317, 2},
				{319, 2},
				{321, 4},
				{324, 4},
				{327, 2},
				{328, 8},
				{329, 4},
				{331, 4},
				{332, 4},
				{333, 4},
				{336, 2},
				{337, 2},
				{338, 2},
				{339, 2},
				{340, 2},
				{341, 2},
				{342, 4},
				{347, 4},
				{348, 2},
				{349, 4},
				{351, 4},
				{352, 2},
				{353, 2},
				{355, 4},
				{357, 2},
				{361, 4},
				{362, 2},
				{363, 2},
				{364, 4},
				{366, 2},
				{367, 4},
				{370, 4},
				{373, 4},
				{374, 4},
				{375, 4},
				{376, 4},
				{377, 6},
				{378, 6},
				{379, 4},
				{380, 2},
				{383, 4},
				{384, 4},
				{385, 4},
				{386, 2},
				{388, 2},
				{389, 6},
				{391, 2},
				{393, 2},
				{394, 2},
				{396, 2},
				{397, 4},
				{398, 4}
			};

			var HistogramALow = new HistogramALow(entries);

			var result = new HistogramA(400);
			result.Add(HistogramALow);

			result.UpperCatchAllValue = 100118;

			return result;

		}

		#endregion
	}
}