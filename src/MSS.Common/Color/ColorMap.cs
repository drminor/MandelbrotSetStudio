using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MSS.Common
{
    public class ColorMap : IEquatable<ColorMap?>, IEqualityComparer<ColorMap>
    {
        private readonly ColorBandSet _colorBandSet;
        private readonly int[] _cutoffs;

        #region Constructor

        public ColorMap(ColorBandSet colorBandSet)
        {
			_colorBandSet = colorBandSet ?? throw new ArgumentNullException(nameof(colorBandSet));
            //Debug.WriteLine($"A new Color Map is being constructed with Id: {colorBandSet.Id}.");

            //_colorBandSet.Fix();
            _cutoffs = colorBandSet.Take(colorBandSet.Count - 1).Select(x => x.Cutoff).ToArray();

            foreach (var colorBand in _colorBandSet)
			{
                if (colorBand.BlendStyle != ColorBandBlendStyle.None)
				{
                    colorBand.BlendVals = new BlendVals(colorBand.StartColor.ColorComps, colorBand.ActualEndColor.ColorComps, opacity: 255);
				}
			}
        }

        #endregion

        #region Public Properties

        public ColorBand HighColorBand => _colorBandSet[^1];

        public bool UseEscapeVelocities { get; set; }

        public bool HighlightSelectedColorBand { get; set; }

		#endregion

		#region Public Methods

		public void PlaceColor(int countVal, double escapeVelocity, IntPtr destination)
		{
			var idx = GetColorMapIndex(countVal);
			var cme = _colorBandSet[idx];

			if (cme.BlendStyle == ColorBandBlendStyle.None)
			{
				PutColor(cme.StartColor.ColorComps, destination);
			}
			else
			{
				var stepFactor = GetStepFactor(countVal, escapeVelocity, cme);
				cme.BlendVals.BlendAndPlace(stepFactor, destination);
			}

			if (HighlightSelectedColorBand && cme != _colorBandSet.SelectedColorBand)
			{
                unsafe
                {
                    //destination[3] = 25;
                    *((byte*)destination + 3) = 25;  // set the opacity to 25, instead of 255.
                }
			}
		}

		public void PlaceColor(int countVal, double escapeVelocity, Span<byte> destination)
        {
            var idx = GetColorMapIndex(countVal);
            var cme = _colorBandSet[idx];

            if (cme.BlendStyle == ColorBandBlendStyle.None)
            {
                PutColor(cme.StartColor.ColorComps, destination);
            }
            else
            {
                var stepFactor = GetStepFactor(countVal, escapeVelocity, cme);
                cme.BlendVals.BlendAndPlace(stepFactor, destination);
            }

            if (HighlightSelectedColorBand && cme != _colorBandSet.SelectedColorBand)
			{
                destination[3] = 25; // set the opacity to 25, instead of 255.
			}
        }

        public byte[] GetColor(int countVal, double escapeVelocity)
        {
            byte[] result;

            var idx = GetColorMapIndex(countVal);
            var cme = _colorBandSet[idx];

            if (cme.BlendStyle == ColorBandBlendStyle.None)
            {
                result = cme.StartColor.ColorComps;
            }
            else
			{
                result = new byte[3];
                var stepFactor = GetStepFactor(countVal, escapeVelocity, cme);
                cme.BlendVals.BlendAndPlace(stepFactor, result);
            }

            return result;
        }

        #endregion

        #region Private Methods

        private double GetStepFactor(int countVal, double escapeVelocity, ColorBand cme)
        {
			var botBucketVal = 1 + cme.PreviousCutoff ?? 0;
            var bucketDistance = countVal - botBucketVal;
            var bucketWidth = cme.BucketWidth;
            bucketWidth += UseEscapeVelocities ? 1 : 0;

			var stepFactor = (bucketDistance + escapeVelocity) / bucketWidth;

            CheckStepFactor(countVal, cme.Cutoff, botBucketVal, bucketWidth, stepFactor);

			return stepFactor;
        }

        [Conditional("DEBUG2")]
        private void CheckStepFactor(int countVal, int cutoff, int botBucketVal, int bucketWidth, double stepFactor)
		{
            if (countVal > 5 && countVal == cutoff)
            {
                //Debug.WriteLine("HereA");
            }

            if (countVal > 5 && countVal == botBucketVal)
            {
                //Debug.WriteLine("HereB");
            }

            var bucketDistance = countVal - botBucketVal;

            if (bucketDistance < 0 || bucketDistance > bucketWidth || stepFactor > 1.0)
            {
                Debug.WriteLine($"Step Distance is out of range: val: {countVal}, bot: {botBucketVal}, top: {cutoff}, width: {bucketWidth}, stepFactor: {stepFactor}.");
            }
        }

        /// <summary>
        /// Returns the ColorBand with the specified Cutoff or if not found,
        /// the ColorBand having a StartingCutoff > value and a Cutoff < value.
        /// </summary>
        /// <param name="countVal"></param>
        /// <returns></returns>
		private int GetColorMapIndex(int countVal)
        {
            int result;

            if (countVal >= HighColorBand.Cutoff)
            {
                result = _colorBandSet.Count - 1;
            }
            else
            {
                // Returns the index to the item with the matched cutoff value
                // or the (bitwise complement of the) index of the item with the smallest cutoff larger than the sought value.
                // If there is no element with a cutoff larger than the sought value, the (bitwise complement of the) 1 + the index of the last item.
                var newIndex = Array.BinarySearch(_cutoffs, countVal);

                result = newIndex < 0 ? ~newIndex : newIndex;
			}

            return result;
        }

        /// <summary>
        /// Fills the destination with the Blue, Green, Red and Alpha values.
        /// </summary>
        /// <param name="comps"></param>
        /// <param name="destination"></param>
        private void PutColor(byte[] comps, Span<byte> destination)
        {
            destination[0] = comps[2];  // Blue
            destination[1] = comps[1];  // Green
            destination[2] = comps[0];  // Red
            destination[3] = 255;
        }

		private unsafe void PutColor(byte[] comps, IntPtr destination)
		{
			//destination[0] = comps[2];  // Blue
			//destination[1] = comps[1];  // Green
			//destination[2] = comps[0];  // Red
			//destination[3] = 255;

			//byte alpha = 255;
			//uint pixelValue = (uint)red + (uint)(green << 8) + (uint)(blue << 16) + (uint)(alpha << 24);

			//pixelValues[y * width + x] = pixelValue;

			//*(byte*)destination = comps[2];
			//*((byte*)destination + 1) = comps[1];
			//*((byte*)destination + 2) = comps[0];
			//*((byte*)destination + 3) = 255;

			var pixelValue = comps[0] + (uint)(comps[1] << 8) + (uint)(comps[2] << 16) + (255u << 24);
			*(uint*)destination = pixelValue;
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
            return GetHashCode();
        }

        public override int GetHashCode()
        {
            return _colorBandSet.GetHashCode();
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

        //         var botBucketVal = _prevCutoffs[colorMapIndex];

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
