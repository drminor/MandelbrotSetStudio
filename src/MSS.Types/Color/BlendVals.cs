using System;
using System.Diagnostics;

namespace MSS.Types
{
	public struct BlendVals
	{
		public byte Opacity { get; set; }

		public double SRed { get; init; }
		public double SGreen { get; init; }
		public double SBlue { get; init; }

		public double ERed { get; init; }
		public double EGreen { get; init; }
		public double EBlue { get; init; }


		public double DiffRed { get; init; }
		public double DiffGreen { get; init; }
		public double DiffBlue { get; init; }

		public BlendVals(byte[] startColor, byte[] endColor, byte opacity)
		{
			SRed = startColor[0];
			SGreen = startColor[1];
			SBlue = startColor[2];

			DiffRed = endColor[0] - startColor[0];
			DiffGreen = endColor[1] - startColor[1]; 
			DiffBlue = endColor[2] - startColor[2];

			Opacity = opacity;

			// For diagnostics only
			ERed = endColor[0];
			EGreen = endColor[1];
			EBlue = endColor[2];
		}

		public void BlendAndPlace(double factor, Span<byte> destination)
		{
			var rd = factor * DiffRed + SRed;
			var gd = factor * DiffGreen + SGreen;
			var bd = factor * DiffBlue + SBlue;

			var r = Math.Round(rd);
			var g = Math.Round(gd);
			var b = Math.Round(bd);

			if (r < 0 || r > 255)
			{
				Debug.WriteLine($"Bad red value. sf: {factor}, st: {SRed}, en: {ERed}.");
			}

			if (g < 0 || g > 255)
			{
				Debug.WriteLine($"Bad green value. sf: {factor}, st: {SGreen}, en: {EGreen}.");
			}

			if (b < 0 || b > 255)
			{
				Debug.WriteLine($"Bad blue value. sf: {factor}, st: {SBlue}, en: {EBlue}.");
			}

			destination[0] = (byte)b;
			destination[1] = (byte)g;
			destination[2] = (byte)r;
			destination[3] = Opacity;
		}

		public unsafe void BlendAndPlace(double factor, IntPtr destination)
		{
			var rd = factor * DiffRed + SRed;
			var gd = factor * DiffGreen + SGreen;
			var bd = factor * DiffBlue + SBlue;

			var r = Math.Round(rd);
			var g = Math.Round(gd);
			var b = Math.Round(bd);

			if (r < 0 || r > 255)
			{
				Debug.WriteLine($"Bad red value. sf: {factor}, st: {SRed}, en: {ERed}.");
			}

			if (g < 0 || g > 255)
			{
				Debug.WriteLine($"Bad green value. sf: {factor}, st: {SGreen}, en: {EGreen}.");
			}

			if (b < 0 || b > 255)
			{
				Debug.WriteLine($"Bad blue value. sf: {factor}, st: {SBlue}, en: {EBlue}.");
			}

			//destination[0] = (byte)b;
			//destination[1] = (byte)g;
			//destination[2] = (byte)r;
			//destination[3] = Opacity;

			//*(byte*)destination = (byte)b;
			//*((byte*)destination + 1) = (byte)g;
			//*((byte*)destination + 2) = (byte)r;
			//*((byte*)destination + 3) = Opacity;

			var pixelValue = (uint)b + (uint)((byte)g << 8) + (uint)((byte)r << 16) + (uint)(Opacity << 24);
			*(uint*)destination = pixelValue;
		}


	}

}
