using MSS.Types;
using System.Diagnostics;

namespace PercentageBandsTest
{
	public class RoundTripCbsTest
	{
		#region Tests

		[Fact]
		public void UsePercentages_HavePercentagesH1()
		{
			var colorBandSet = RMapConstants.BuildInitialColorBandSet("Test", maxIterations: 400, usePercentages: true);
			var histogram = GetHistogram1();

			ApplyHistogram(colorBandSet, histogram, usePercentages: true);

			ApplyHistogram(colorBandSet, histogram, usePercentages: true);
		}

		[Fact]
		public void UsePercentages_DoNotHavePercentagesH1()
		{
			var colorBandSet = RMapConstants.BuildInitialColorBandSet("Test", maxIterations: 400, usePercentages: false);
			var histogram = GetHistogram1();

			ApplyHistogram(colorBandSet, histogram, usePercentages: true);

			ApplyHistogram(colorBandSet, histogram, usePercentages: true);
		}

		[Fact]
		public void UseCutoffsH1()
		{
			var colorBandSet = RMapConstants.BuildInitialColorBandSet("Test", maxIterations: 400, usePercentages: false);
			var histogram = GetHistogram1();

			ApplyHistogram(colorBandSet, histogram, usePercentages: false);

			ApplyHistogram(colorBandSet, histogram, usePercentages: false);
		}

		[Fact]
		public void UsePercentages_HavePercentagesH2()
		{
			var colorBandSet = RMapConstants.BuildInitialColorBandSet("Test", maxIterations: 400, usePercentages: true);
			var histogram = GetHistogram2();

			ApplyHistogram(colorBandSet, histogram, usePercentages: true);

			ApplyHistogram(colorBandSet, histogram, usePercentages: true);
		}

		[Fact]
		public void UsePercentages_DoNotHavePercentagesH2()
		{
			var colorBandSet = RMapConstants.BuildInitialColorBandSet("Test", maxIterations: 400, usePercentages: false);
			var histogram = GetHistogram2();

			// Update Percentage from Cutoffs
			ApplyHistogram(colorBandSet, histogram, usePercentages: true);

			// Update Cutoffs from Percentages
			ApplyHistogram(colorBandSet, histogram, usePercentages: true);

			// Update Percentages from Cutoffs
			ApplyHistogram(colorBandSet, histogram, usePercentages: false);
		}

		[Fact]
		public void UseCutoffsH2()
		{
			var colorBandSet = RMapConstants.BuildInitialColorBandSet("Test", maxIterations: 400, usePercentages: false);
			var histogram = GetHistogram2();

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
			if (ColorBandSetHelper.TryGetPercentagesFromCutoffs(histCutoffsSnapShot, out var newPercentages, out var resultsAreComplete))
			{
				ColorBandSetHelper.ReportNewPercentages(newPercentages);

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
				ColorBandSetHelper.ReportNewCutoffs(histCutoffsSnapShot, histCutoffsSnapShot.PercentageBands, newCutoffBands);

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
				colorBandSet.Id,
				histogram.GetKeyValuePairs(),
				histogram.Length,
				histogram.UpperCatchAllValue,
				ColorBandSetHelper.GetPercentageBands(colorBandSet),
				colorBandSet.UsingPercentages
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

		private HistogramA GetHistogram2()
		{
			IDictionary<int, int> entries = new Dictionary<int, int>()
			{
				{29, 1647},
				{30, 8546},
				{31, 9338},
				{32, 8581},
				{33, 7925},
				{34, 6122},
				{35, 4475},
				{36, 3486},
				{37, 3185},
				{38, 3595},
				{39, 4930},
				{40, 7416},
				{41, 10124},
				{42, 11117},
				{43, 9523},
				{44, 7062},
				{45, 4960},
				{46, 3656},
				{47, 3019},
				{48, 2982},
				{49, 3785},
				{50, 5612},
				{51, 7902},
				{52, 9479},
				{53, 13955},
				{54, 12331},
				{55, 9348},
				{56, 7314},
				{57, 6292},
				{58, 6504},
				{59, 8203},
				{60, 11738},
				{61, 16291},
				{62, 18845},
				{63, 17108},
				{64, 13324},
				{65, 9769},
				{66, 7286},
				{67, 5990},
				{68, 5781},
				{69, 6720},
				{70, 9341},
				{71, 12927},
				{72, 15442},
				{73, 14817},
				{74, 12132},
				{75, 9216},
				{76, 6924},
				{77, 5733},
				{78, 5255},
				{79, 5789},
				{80, 8025},
				{81, 11055},
				{82, 13382},
				{83, 12796},
				{84, 10830},
				{85, 8400},
				{86, 6422},
				{87, 5278},
				{88, 4695},
				{89, 5113},
				{90, 6522},
				{91, 8600},
				{92, 10270},
				{93, 10325},
				{94, 8953},
				{95, 7083},
				{96, 5654},
				{97, 4621},
				{98, 4089},
				{99, 4305},
				{100, 5246},
				{101, 6735},
				{102, 8059},
				{103, 8370},
				{104, 7451},
				{105, 6158},
				{106, 5022},
				{107, 4141},
				{108, 3687},
				{109, 3813},
				{110, 4530},
				{111, 5640},
				{112, 6869},
				{113, 7159},
				{114, 6574},
				{115, 5476},
				{116, 4501},
				{117, 3809},
				{118, 3444},
				{119, 3348},
				{120, 4019},
				{121, 4818},
				{122, 5857},
				{123, 5961},
				{124, 5635},
				{125, 4796},
				{126, 4052},
				{127, 3387},
				{128, 3014},
				{129, 3076},
				{130, 3433},
				{131, 4159},
				{132, 4823},
				{133, 5164},
				{134, 4736},
				{135, 4277},
				{136, 3573},
				{137, 3043},
				{138, 2786},
				{139, 2735},
				{140, 3122},
				{141, 3698},
				{142, 4286},
				{143, 4510},
				{144, 4323},
				{145, 3938},
				{146, 3289},
				{147, 2956},
				{148, 2654},
				{149, 2609},
				{150, 2926},
				{151, 3318},
				{152, 3869},
				{153, 4090},
				{154, 3759},
				{155, 3558},
				{156, 3071},
				{157, 2695},
				{158, 2363},
				{159, 2335},
				{160, 2420},
				{161, 2892},
				{162, 3289},
				{163, 3411},
				{164, 3490},
				{165, 3203},
				{166, 2829},
				{167, 2408},
				{168, 2113},
				{169, 2129},
				{170, 2272},
				{171, 2663},
				{172, 2933},
				{173, 3153},
				{174, 3015},
				{175, 2846},
				{176, 2537},
				{177, 2255},
				{178, 2136},
				{179, 1987},
				{180, 2087},
				{181, 2409},
				{182, 2730},
				{183, 2826},
				{184, 2770},
				{185, 2589},
				{186, 2347},
				{187, 2016},
				{188, 1896},
				{189, 1883},
				{190, 1926},
				{191, 2173},
				{192, 2405},
				{193, 2471},
				{194, 2392},
				{195, 2278},
				{196, 2141},
				{197, 1886},
				{198, 1717},
				{199, 1708},
				{200, 1786},
				{201, 1992},
				{202, 2091},
				{203, 2299},
				{204, 2139},
				{205, 2078},
				{206, 1954},
				{207, 1717},
				{208, 1618},
				{209, 1472},
				{210, 1584},
				{211, 1843},
				{212, 1942},
				{213, 2087},
				{214, 1944},
				{215, 1911},
				{216, 1740},
				{217, 1649},
				{218, 1412},
				{219, 1401},
				{220, 1437},
				{221, 1729},
				{222, 1758},
				{223, 1807},
				{224, 1779},
				{225, 1685},
				{226, 1490},
				{227, 1419},
				{228, 1355},
				{229, 1324},
				{230, 1297},
				{231, 1445},
				{232, 1636},
				{233, 1676},
				{234, 1632},
				{235, 1525},
				{236, 1424},
				{237, 1356},
				{238, 1228},
				{239, 1199},
				{240, 1305},
				{241, 1417},
				{242, 1516},
				{243, 1566},
				{244, 1533},
				{245, 1433},
				{246, 1257},
				{247, 1183},
				{248, 1139},
				{249, 1086},
				{250, 1148},
				{251, 1216},
				{252, 1356},
				{253, 1317},
				{254, 1323},
				{255, 1286},
				{256, 1185},
				{257, 1075},
				{258, 1064},
				{259, 992},
				{260, 1014},
				{261, 1136},
				{262, 1179},
				{263, 1216},
				{264, 1271},
				{265, 1154},
				{266, 1074},
				{267, 960},
				{268, 954},
				{269, 915},
				{270, 969},
				{271, 1012},
				{272, 1093},
				{273, 1143},
				{274, 1086},
				{275, 1048},
				{276, 968},
				{277, 935},
				{278, 923},
				{279, 876},
				{280, 865},
				{281, 954},
				{282, 1054},
				{283, 1037},
				{284, 950},
				{285, 916},
				{286, 906},
				{287, 831},
				{288, 786},
				{289, 819},
				{290, 769},
				{291, 929},
				{292, 969},
				{293, 954},
				{294, 924},
				{295, 875},
				{296, 865},
				{297, 737},
				{298, 750},
				{299, 736},
				{300, 734},
				{301, 784},
				{302, 844},
				{303, 866},
				{304, 883},
				{305, 833},
				{306, 801},
				{307, 707},
				{308, 651},
				{309, 642},
				{310, 690},
				{311, 746},
				{312, 787},
				{313, 821},
				{314, 782},
				{315, 722},
				{316, 718},
				{317, 672},
				{318, 633},
				{319, 628},
				{320, 653},
				{321, 678},
				{322, 737},
				{323, 711},
				{324, 736},
				{325, 654},
				{326, 629},
				{327, 628},
				{328, 565},
				{329, 538},
				{330, 537},
				{331, 632},
				{332, 726},
				{333, 640},
				{334, 657},
				{335, 570},
				{336, 593},
				{337, 538},
				{338, 548},
				{339, 531},
				{340, 542},
				{341, 581},
				{342, 593},
				{343, 616},
				{344, 601},
				{345, 584},
				{346, 534},
				{347, 481},
				{348, 488},
				{349, 497},
				{350, 529},
				{351, 499},
				{352, 589},
				{353, 619},
				{354, 579},
				{355, 569},
				{356, 503},
				{357, 465},
				{358, 444},
				{359, 499},
				{360, 473},
				{361, 520},
				{362, 511},
				{363, 592},
				{364, 520},
				{365, 512},
				{366, 464},
				{367, 423},
				{368, 402},
				{369, 405},
				{370, 421},
				{371, 461},
				{372, 504},
				{373, 507},
				{374, 516},
				{375, 475},
				{376, 449},
				{377, 384},
				{378, 370},
				{379, 384},
				{380, 411},
				{381, 409},
				{382, 478},
				{383, 478},
				{384, 478},
				{385, 436},
				{386, 384},
				{387, 342},
				{388, 353},
				{389, 338},
				{390, 387},
				{391, 416},
				{392, 408},
				{393, 434},
				{394, 433},
				{395, 398},
				{396, 392},
				{397, 340},
				{398, 333},
				{399, 306},
			};

			//var HistogramALow = new HistogramALow(entries);
			//var result = new HistogramA(400);
			//result.Add(HistogramALow);
			//result.UpperCatchAllValue = 202155;


			var result = new HistogramA(entries);
			result.UpperCatchAllValue = 202155;

			return result;
		}

		#endregion
	}
}