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
        private readonly ColorBandSet _colorBandSet;
        private readonly int[] _cutOffs;

        #region Constructor

        public ColorMap(ColorBandSet colorBandSet)
        {
            if (colorBandSet == null)
            {
                throw new ArgumentNullException(nameof(colorBandSet));
            }

            Debug.WriteLine($"A new Color Map is being constructed with Id: {colorBandSet.Id}.");

            _colorBandSet = colorBandSet;
            //_colorBandSet.Fix();
            _cutOffs = colorBandSet.Take(colorBandSet.Count - 1).Select(x => x.CutOff).ToArray();

            foreach(var colorBand in _colorBandSet)
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

        #endregion

        #region Public Methods

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

                if (cme.BlendVals != null)
				{
                    cme.BlendVals.Value.BlendAndPlace(stepFactor, destination);
				}
                else
				{
                    throw new InvalidOperationException("BlendVals is null for a CME with BlendStyle != none.");
				}
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
                if (cme.BlendVals != null)
                {
                    result = new byte[3];
                    var stepFactor = GetStepFactor(countVal, escapeVelocity, cme);
                    cme.BlendVals.Value.BlendAndPlace(stepFactor, result);
                }
                else
                {
                    throw new InvalidOperationException("BlendVals is null for a CME with BlendStyle != none.");
                }
            }

            return result;
        }

        #endregion

        #region Private Methods

        private double GetStepFactor(int countVal, double escapeVelocity, ColorBand cme)
        {
			var botBucketVal = 1 + cme.PreviousCutOff ?? 0;
            var bucketDistance = countVal - botBucketVal;
            var bucketWidth = cme.BucketWidth;
            bucketWidth += UseEscapeVelocities ? 1 : 0;

			var stepFactor = (bucketDistance + escapeVelocity) / bucketWidth;

            CheckStepFactor(countVal, cme.CutOff, botBucketVal, bucketWidth, stepFactor);

			return stepFactor;
        }

        [Conditional("DEBUG")]
        private void CheckStepFactor(int countVal, int cutOff, int botBucketVal, int bucketWidth, double stepFactor)
		{
            if (countVal > 5 && countVal == cutOff)
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
                Debug.WriteLine($"Step Distance is out of range: val: {countVal}, bot: {botBucketVal}, top: {cutOff}, width: {bucketWidth}, stepFactor: {stepFactor}.");
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

            if (countVal >= HighColorBand.CutOff)
            {
                result = _colorBandSet.Count - 1;
            }
            else
            {
                // Returns the index to the item with the matched cutoff value
                // or the index of the item with the smallest cutoff larger than the sought value
                var newIndex = Array.BinarySearch(_cutOffs, countVal);

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
