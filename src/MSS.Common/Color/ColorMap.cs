using MSS.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Common
{
    public class ColorMap : IEquatable<ColorMap?>, IEqualityComparer<ColorMap>, IDisposable
    {
        private readonly ColorBandSet _colorBandSet;
        private ColorBand[] _colorBands;
        private readonly int[] _cutoffs;

        private readonly int _highColorBandCutoff;
        private readonly int _highColorBandIndex;

		private int _selectedColorBandIndex;
        private bool _disposedValue;

        #region Constructor

        public ColorMap(ColorBandSet colorBandSet)
        {
			_colorBandSet = colorBandSet ?? throw new ArgumentNullException(nameof(colorBandSet));
            //Debug.WriteLine($"A new Color Map is being constructed with Id: {colorBandSet.Id}.");

            _selectedColorBandIndex = _colorBandSet.SelectedColorBandIndex;
            if (_colorBandSet is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged += ColorBandSet_PropertyChanged;
            }
            else
            {
                throw new InvalidOperationException("Expecting the ColorBandSet to implement INotifyPropertyChanged.");
            }

            _cutoffs = colorBandSet.Take(colorBandSet.Count - 1).Select(x => x.Cutoff).ToArray();
            _colorBands = BuildOurColorBands(_colorBandSet);
			ReportBlendValues(_colorBands);

			_highColorBandIndex = colorBandSet.Count - 1;
			_highColorBandCutoff = _colorBands[^1].Cutoff;
		}

        private ColorBand[] BuildOurColorBands(ColorBandSet colorBandSet)
        {
            var result = new ColorBand[colorBandSet.Count];

			for (var i = 0; i < colorBandSet.Count; i++)
			{
				var sourceCB = colorBandSet[i];

				var colorBand = new ColorBand(sourceCB.Cutoff, sourceCB.StartColor, sourceCB.BlendStyle, sourceCB.ActualEndColor, sourceCB.StartingCutoff, sourceCB.BucketWidth);

				if (colorBand.BlendStyle != ColorBandBlendStyle.None)
				{
					colorBand.BlendVals = new BlendVals(colorBand.StartColor.ColorComps, colorBand.EndColor.ColorComps, opacity: 255);
				}

				result[i] = colorBand;
			}

            return result;
		}

        private void ReportBlendValues(ColorBand[] colorBands)
        {
			for (var i = 0; i < colorBands.Length; i++)
			{
				var blendVals = colorBands[i].BlendVals;

                Debug.WriteLine($"{i}: {blendVals}");
			}
		}

		private void ColorBandSet_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ColorBandSet.SelectedColorBand))
            {
                _selectedColorBandIndex = _colorBandSet.SelectedColorBandIndex;
            }
        }

        #endregion

        #region Public Properties


        public bool UseEscapeVelocities { get; set; }

        public bool HighlightSelectedColorBand { get; set; }

		#endregion

		#region Public Methods

		//public void PlaceColor(int countVal, double escapeVelocity, IntPtr destination)
		//{
		//	var idx = GetColorMapIndex(countVal);
		//	var cme = _colorBandSet[idx];

		//	if (cme.BlendStyle == ColorBandBlendStyle.None)
		//	{
		//		PutColor(cme.StartColor.ColorComps, destination);
		//	}
		//	else
		//	{
		//		var stepFactor = GetStepFactor(countVal, escapeVelocity, cme);
		//		cme.BlendVals.BlendAndPlace(stepFactor, destination);
		//	}

		//	if (HighlightSelectedColorBand && cme != _colorBandSet.SelectedColorBand)
		//	{
  //              unsafe
  //              {
  //                  //destination[3] = 25;
  //                  *((byte*)destination + 3) = 25;  // set the opacity to 25, instead of 255.
  //              }
		//	}
		//}

		public int PlaceColor(int countVal, double escapeVelocity, Span<byte> destination)
        {
            var errors = 0;

            var idx = GetColorMapIndex(countVal);
            //var cme = _colorBandSet[idx];
            var cme = _colorBands[idx];

            if (cme.BlendStyle == ColorBandBlendStyle.None)
            {
                PutColor(cme.StartColor.ColorComps, destination);
            }
            else
            {
                var stepFactor = GetStepFactor(countVal, escapeVelocity, cme);
                errors = cme.BlendVals.BlendAndPlace(stepFactor, destination);
            }

            if (HighlightSelectedColorBand && idx != _selectedColorBandIndex)
			{
                destination[3] = 25; // set the opacity to 25, instead of 255.
			}

            return errors;
        }

   //     public byte[] GetColor(int countVal, double escapeVelocity)
   //     {
   //         byte[] result;

   //         var idx = GetColorMapIndex(countVal);
   //         var cme = _colorBandSet[idx];

   //         if (cme.BlendStyle == ColorBandBlendStyle.None)
   //         {
   //             result = cme.StartColor.ColorComps;
   //         }
   //         else
			//{
   //             result = new byte[3];
   //             var stepFactor = GetStepFactor(countVal, escapeVelocity, cme);
   //             cme.BlendVals.BlendAndPlace(stepFactor, result);
   //         }

   //         return result;
   //     }

        #endregion

        #region Private Methods

        private double GetStepFactor(int countVal, double escapeVelocity, ColorBand cme)
        {
            var bucketDistance = countVal + escapeVelocity - cme.StartingCutoff;
            var bucketWidth = cme.BucketWidth;
            bucketWidth += UseEscapeVelocities ? 1 : 0;

            var stepFactor = bucketDistance > 0 ? bucketDistance / bucketWidth : 0;

            CheckStepFactor(countVal, cme.Cutoff, cme.StartingCutoff, bucketWidth, stepFactor, escapeVelocity);

			return stepFactor;
        }

        [Conditional("DEBUG")]
        private void CheckStepFactor(int countVal, int cutoff, int startingCutoff, int bucketWidth, double stepFactor, double escapeVelocity)
		{
            var bucketDistance = countVal + escapeVelocity - startingCutoff;

            if (bucketDistance < 0)
            {
                Debug.WriteLine($"BucketDistance < 0: val: {countVal}, bot: {startingCutoff}, top: {cutoff}, width: {bucketWidth}, stepFactor: {stepFactor}. Escape Velolocity: {escapeVelocity}");
            }

			if (bucketDistance > bucketWidth)
			{
				Debug.WriteLine($"bucketDistance > bucketWidth: val: {countVal}, bot: {startingCutoff}, top: {cutoff}, width: {bucketWidth}, stepFactor: {stepFactor}. Escape Velolocity: {escapeVelocity}");
			}
			
            if (stepFactor > 1.0)
			{
				Debug.WriteLine($"StepFactor > 1: val: {countVal}, bot: {startingCutoff}, top: {cutoff}, width: {bucketWidth}, stepFactor: {stepFactor}. Escape Velolocity: {escapeVelocity}");
			}
			
            if (bucketDistance + startingCutoff > (1 + cutoff))
			{
				Debug.WriteLine($"bucketDistance + startingCutoff > 1 + endingCutoff: val: {countVal}, bot: {startingCutoff}, top: {cutoff}, width: {bucketWidth}, stepFactor: {stepFactor}. Escape Velolocity: {escapeVelocity}");
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

            if (countVal >= _highColorBandCutoff)
            {
                result = _highColorBandIndex;
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

		//private unsafe void PutColor(byte[] comps, IntPtr destination)
		//{
		//	//destination[0] = comps[2];  // Blue
		//	//destination[1] = comps[1];  // Green
		//	//destination[2] = comps[0];  // Red
		//	//destination[3] = 255;

		//	//byte alpha = 255;
		//	//uint pixelValue = (uint)red + (uint)(green << 8) + (uint)(blue << 16) + (uint)(alpha << 24);

		//	//pixelValues[y * width + x] = pixelValue;

		//	//*(byte*)destination = comps[2];
		//	//*((byte*)destination + 1) = comps[1];
		//	//*((byte*)destination + 2) = comps[0];
		//	//*((byte*)destination + 3) = 255;

		//	var pixelValue = comps[0] + (uint)(comps[1] << 8) + (uint)(comps[2] << 16) + (255u << 24);
		//	*(uint*)destination = pixelValue;
		//}

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

		#region IDisposable Support

		protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
					// Dispose managed state (managed objects)
					if (_colorBandSet is INotifyPropertyChanged inpc)
					{
						inpc.PropertyChanged -= ColorBandSet_PropertyChanged;
					}
				}

				_disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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


		private class ColorBand
		{
			#region Constructor

			public ColorBand(int cutoff, ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor, 
                int startingCutoff, /*ColorBandColor? successorStartColor, bool isFirst, bool isLast, */
                int bucketWidth)
			{
				Cutoff = cutoff;
				StartColor = startColor;
			    BlendStyle = blendStyle;
				EndColor = endColor;
                StartingCutoff = startingCutoff;
                BucketWidth = bucketWidth;
			}

			#endregion

			#region Public Properties

			public int Cutoff { get; init; }

			public ColorBandColor StartColor { get; init; }
			public ColorBandBlendStyle BlendStyle { get; init; }
			public ColorBandColor EndColor { get; init; }

			public int StartingCutoff { get; init; }
			public int BucketWidth { get; init; }

			public BlendVals BlendVals { get; set; }

			//public ColorBandColor? SuccessorStartColor { get; init; }
			//public bool IsFirst { get; init; }
			//public bool IsLast { get; init; }

			#endregion

			public override string? ToString()
			{
				return $"Starting Cutoff: {StartingCutoff}, Ending Cutoff: {Cutoff}, Start: {StartColor.GetCssColor()}, Blend: {BlendStyle}, End: {EndColor.GetCssColor()}.";
			}
		}
	}


}
