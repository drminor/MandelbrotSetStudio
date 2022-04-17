using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Common
{
	public class ColorMap : IEquatable<ColorMap?>, IEqualityComparer<ColorMap>
    {
        private readonly int[] _cutOffs;
        private readonly int _lastCutOff;

		#region Constructor

		public ColorMap(ColorBandSet colorBandSet)
		{
            if (colorBandSet == null)
			{
                throw new ArgumentNullException(nameof(colorBandSet));
            }

            Debug.WriteLine($"A new Color Map is being constructed with Id: {colorBandSet.Id}.");

            _colorBandSet = colorBandSet;
            _lastCutOff = colorBandSet.ColorBands[^1].CutOff;
            _cutOffs = colorBandSet.Take(colorBandSet.Count - 1).Select(x => x.CutOff).ToArray();
        }

        #endregion

        #region Public Properties

        private ColorBandSet _colorBandSet;

        public ColorBand HighColorBand => _colorBandSet[^1];

        //public string Id => ColorBandSet.Id.ToString();

        #endregion

        #region Public Methods

        public void PlaceColor(int countVal, double escapeVelocity, Span<byte> destination)
		{
            var cme = GetColorBand(countVal);

            if (cme.BlendStyle == ColorBandBlendStyle.None)
            {
                PutColor(cme.StartColor.ColorComps, destination);
            }
            else
            {
                var stepFactor = GetStepFactor(countVal, escapeVelocity, cme);
                InterpolateAndPlace(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.ActualEndColor.ColorComps, stepFactor, destination);
            }
        }

        public byte[] GetColor(int countVal, double escapeVelocity)
        {
			byte[] result;

            var cme = GetColorBand(countVal);

            if (cme.BlendStyle == ColorBandBlendStyle.None)
            {
                result = cme.StartColor.ColorComps;
                return result;
            }

            var stepFactor = GetStepFactor(countVal, escapeVelocity, cme);
            result = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.ActualEndColor.ColorComps, stepFactor);

            return result;
        }

		#endregion

		#region Private Methods

        private double GetStepFactor(int countVal, double escapeVelocity, ColorBand cme)
		{
            var topBucketVal = Math.Min(countVal, cme.CutOff) + escapeVelocity;
            var botBucketVal = cme.PreviousCutOff;
            var bucketWidth = cme.BucketWidth;

            var stepFactor = (topBucketVal - botBucketVal) / bucketWidth;

            return stepFactor;
        }

        private ColorBand GetColorBand(int countVal)
		{
            ColorBand result;

            if (countVal >= _lastCutOff)
			{
                result = HighColorBand;
			}
            else
			{
                result = _colorBandSet.ColorBands[GetColorMapIndex(countVal)];
			}

            return result;
		}

		private int GetColorMapIndex(int countVal)
		{
			var newIndex = Array.BinarySearch(_cutOffs, countVal);

			if (newIndex < 0)
			{
				newIndex = ~newIndex;
			}
			else
			{
				newIndex++;
			}

			return newIndex;
		}

        private byte[] Interpolate(byte[] cStart, byte[] c1, byte[] c2, double factor)
        {
            if (factor == 0)
            {
                return cStart;
            }
            else
            {
                var rd = cStart[0] + (c2[0] - c1[0]) * factor;
                var gd = cStart[1] + (c2[1] - c1[1]) * factor;
                var bd = cStart[2] + (c2[2] - c1[2]) * factor;

                var r = (int)Math.Round(rd);
                var g = (int)Math.Round(gd);
                var b = (int)Math.Round(bd);

                if (r < 0 || r > 255)
                {
                    //console.log('Bad red value.');
                }

                if (g < 0 || g > 255)
                {
                    //console.log('Bad green value.');
                }

                if (b < 0 || b > 255)
                {
                    //console.log('Bad blue value.');
                }

                var result = new byte[3];
                result[0] = (byte)r;
                result[1] = (byte)g;
                result[2] = (byte)b;

                return result;
            }
        }

        private void InterpolateAndPlace(byte[] cStart, byte[] c1, byte[] c2, double factor, Span<byte> destination)
        {
            if (factor == 0)
            {
                PutColor(cStart, destination);
            }
            else
            {
                var rd = cStart[0] + (c2[0] - c1[0]) * factor;
                var gd = cStart[1] + (c2[1] - c1[1]) * factor;
                var bd = cStart[2] + (c2[2] - c1[2]) * factor;

                var r = Math.Round(rd);
                var g = Math.Round(gd);
                var b = Math.Round(bd);

                destination[0] = (byte)b;
                destination[1] = (byte)g;
                destination[2] = (byte)r;
                destination[3] = 255;
            }
        }

        /// <summary>
        /// Fills the destination with the Blue, Green, Red and Alpha values.
        /// </summary>
        /// <param name="comps"></param>
        /// <param name="destination"></param>
        private void PutColor(byte[] comps, Span<byte> destination)
        {
            destination[0] = comps[2];
            destination[1] = comps[1];
            destination[2] = comps[0];
            destination[3] = 255;
        }

        #endregion

        #region IEquatable and IEqualityComparer Support

        public override bool Equals(object? obj)
        {
            return Equals(obj as ColorMap);
        }

        public bool Equals(ColorMap? other)
        {
            return other != null &&
                   EqualityComparer<ColorBandSet>.Default.Equals(_colorBandSet, other._colorBandSet);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_colorBandSet);
        }

		public bool Equals(ColorMap? x, ColorMap? y)
		{
            if (x is null)
            {
                return y is null;
            }
            else
            {
                return x.Equals(y);
            }
        }

		public int GetHashCode([DisallowNull] ColorMap obj)
		{
            return GetHashCode(obj);
        }

        public static bool operator ==(ColorMap? left, ColorMap? right)
        {
            return EqualityComparer<ColorMap>.Default.Equals(left, right);
        }

        public static bool operator !=(ColorMap? left, ColorMap? right)
        {
            return !(left == right);
        }

		#endregion

		#region Old Not Used

		//     private byte[] GetBlendedColor(ColorBand cme, int countVal, double escapeVelocity)
		//     {
		//         byte[] result;

		//         //var cme = GetColorMapEntry(colorMapIndex);

		//         if (cme.BlendStyle == ColorBandBlendStyle.None)
		//         {
		//             result = cme.StartColor.ColorComps;
		//             return result;
		//         }

		//         var botBucketVal = _prevCutOffs[colorMapIndex];

		//         //int[] cStart;

		//         //if (countVal == botBucketVal)
		//         //{
		//         //	cStart = cme.StartColor.ColorComps;
		//         //}
		//         //else
		//         //{
		//         //	double stepFactor = (-1 + countVal - botBucketVal) / (double)cme.BucketWidth;
		//         //	cStart = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);
		//         //}

		//         ////double stepFactor = (countVal - botBucketVal) / (double)cme.BucketWidth;
		//         ////cStart = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);

		//         //double intraStepFactor = escapeVelocity / cme.BucketWidth;
		//         //result = Interpolate(cStart, cme.StartColor.ColorComps, cme.EndColor.ColorComps, intraStepFactor);

		//         var bucketWidth = _bucketWidths[colorMapIndex];
		//         var stepFactor = (countVal + escapeVelocity - botBucketVal) / bucketWidth;
		//         result = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);

		//         return result;
		//
		//     }

		#endregion
	}
}
