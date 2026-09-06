using MSS.Types.PColor;

namespace MSS.Types
{
	public struct BlendValsLCH
	{
		private readonly Lch _endLch;

		public double SLuminance { get; init; }
		public double SChroma { get; init; }
		public double SHue { get; init; }

		public double DiffLuminance { get; init; }
		public double DiffChroma { get; init; }
		public double DiffHue { get; init; }

		public BlendValsLCH()
		{
			SLuminance = 0;
			SChroma = 0;
			SHue = 0;

			DiffLuminance = 0;
			DiffChroma = 0;
			DiffHue = 0;

			_endLch = new Lch();
		}

		public BlendValsLCH(Lch startLch, Lch endLch)
		{
			SLuminance = startLch.L;
			SChroma = startLch.C;
			SHue = startLch.H;

			DiffLuminance = endLch.L - startLch.L;
			DiffChroma = endLch.C - startLch.C;
			DiffHue = endLch.H - startLch.H;

			_endLch = endLch;
		}

		public Lch Blend(double factor)
		{
			var l = factor * DiffLuminance + SLuminance;
			var c = factor * DiffChroma + SChroma;
			var h = factor * DiffHue + SHue;

			return new Lch(l, c, h);
		}

		public override string? ToString()
		{
			var result = $"BlendVal (Ending, Starting, Diff) Luminance: {_endLch.L}, {SLuminance}, {DiffLuminance}\tChroma: {_endLch.C}, {SChroma}, {DiffChroma}\tHue: {_endLch.H}, {SHue}, {DiffHue}";

			return result;
		}
	}


}
