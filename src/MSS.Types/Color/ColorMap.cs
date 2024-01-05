using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Types
{
    public class ColorMap : IEquatable<ColorMap?>, IEqualityComparer<ColorMap>, IDisposable
    {
		#region Private Fields

		private readonly ColorBandSet _colorBandSet;
        private ColorMapEntry[] _colorMapEntries;
        private readonly int[] _cutoffs;

        private readonly int _highColorBandCutoff;
        private readonly int _highColorBandIndex;

		private int _currentColorBandIndex;
        private bool _disposedValue;

		#endregion

		#region Constructor

		public ColorMap(ColorBandSet colorBandSet)
        {
			_colorBandSet = colorBandSet ?? throw new ArgumentNullException(nameof(colorBandSet));
            //Debug.WriteLine($"A new Color Map is being constructed with Id: {colorBandSet.Id}.");

            _currentColorBandIndex = _colorBandSet.HilightedColorBandIndex;
            if (_colorBandSet is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged += ColorBandSet_PropertyChanged;
            }
            else
            {
                throw new InvalidOperationException("Expecting the ColorBandSet to implement INotifyPropertyChanged.");
            }

			//_cutoffs = colorBandSet.Take(colorBandSet.Count - 1).Select(x => x.Cutoff).ToArray();
			//_colorMapEntries = BuildColorMapEntries(_colorBandSet);
			//ReportBlendValues(_colorMapEntries);

			//_highColorBandIndex = colorBandSet.Count - 1;
			//_highColorBandCutoff = _colorMapEntries[^1].Cutoff;

			_colorMapEntries = BuildColorMapEntries(_colorBandSet);
			ReportBlendValues(_colorMapEntries);

			_cutoffs = _colorMapEntries.Take(_colorMapEntries.Length - 1).Select(x => x.Cutoff).ToArray();

			_highColorBandIndex = _colorMapEntries.Length - 1;
			_highColorBandCutoff = _colorMapEntries[^1].Cutoff;
		}

		#endregion

		#region Event Handlers

		private void ColorBandSet_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
			//if (e.PropertyName == nameof(ColorBandSet.SelectedColorBand))
			//{
			//    _selectedColorBandIndex = _colorBandSet.CurrentColorBandIndex;
			//}

			if (e.PropertyName == nameof(ColorBandSet.HilightedColorBandIndex))
			{
				_currentColorBandIndex = _colorBandSet.HilightedColorBandIndex;
			}
		}

		#endregion

		#region Public Properties


		public bool UseEscapeVelocities { get; set; }

        public bool HighlightSelectedColorBand { get; set; }

		#endregion

		#region Public Methods

		public int PlaceColor(int countVal, double escapeVelocity, Span<byte> destination)
        {
            var errors = 0;

            var idx = GetColorMapIndex(countVal);
            var cme = _colorMapEntries[idx];

            if (cme.BlendStyle == ColorBandBlendStyle.None)
            {
                PutColor(cme.StartColor.ColorComps, destination);
            }
            else
            {
				if (idx == _highColorBandIndex && countVal > _highColorBandCutoff)
				{
					countVal = _highColorBandCutoff + 1;
				}

				var stepFactor = GetStepFactor(countVal, escapeVelocity, cme);
                errors = cme.BlendVals.BlendAndPlace(stepFactor, destination);
            }

            if (HighlightSelectedColorBand && idx != _currentColorBandIndex)
			{
                destination[3] = 25; // set the opacity to 25, instead of 255.
			}

            return errors;
        }

		#endregion

		#region Private Methods

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

		private double GetStepFactor(int countVal, double escapeVelocity, ColorMapEntry cme)
		{
			var bucketDistance = countVal + escapeVelocity - cme.StartingCutoff;
			var bucketWidth = cme.BucketWidth;
			bucketWidth += UseEscapeVelocities ? 1 : 0;

			var stepFactor = bucketDistance > 0 ? bucketDistance / bucketWidth : 0;

			CheckStepFactor(countVal, cme.Cutoff, cme.StartingCutoff, bucketWidth, stepFactor, escapeVelocity);

			return stepFactor;
		}

		private ColorMapEntry[] BuildColorMapEntries(ColorBandSet colorBandSet)
		{
			var result = new ColorMapEntry[colorBandSet.Count];

			for (var i = 0; i < colorBandSet.Count; i++)
			{
				var colorBand = new ColorMapEntry(colorBandSet[i]);

				if (colorBand.BlendStyle != ColorBandBlendStyle.None)
				{
					colorBand.BlendVals = new BlendVals(colorBand.StartColor.ColorComps, colorBand.EndColor.ColorComps, opacity: 255);
				}

				result[i] = colorBand;
			}

			return result;
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void ReportBlendValues(ColorMapEntry[] colorMapEntries)
		{
			for (var i = 0; i < colorMapEntries.Length; i++)
			{
				var blendVals = colorMapEntries[i].BlendVals;

				Debug.WriteLine($"{i}: {blendVals}");
			}
		}

		[Conditional("DEBUG2")]
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

		#region Unsafe Versions

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
		//		unsafe
		//		{
		//			//destination[3] = 25;
		//			*((byte*)destination + 3) = 25;  // set the opacity to 25, instead of 255.
		//		}
		//	}
		//}

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
		
		#region Old Not Used

		//private byte[] GetBlendedColor(ColorBand cme, int countVal, double escapeVelocity)
		//{
		//	byte[] result;

		//	//var cme = GetColorMapEntry(colorMapIndex);

		//	if (cme.BlendStyle == ColorBandBlendStyle.None)
		//	{
		//		result = cme.StartColor.ColorComps;
		//		return result;
		//	}

		//	var botBucketVal = _prevCutoffs[colorMapIndex];

		//	//int[] cStart;

		//	//if (countVal == botBucketVal)
		//	//{
		//	//	cStart = cme.StartColor.ColorComps;
		//	//}
		//	//else
		//	//{
		//	//	double stepFactor = (-1 + countVal - botBucketVal) / (double)cme.BucketWidth;
		//	//	cStart = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);
		//	//}

		//	////double stepFactor = (countVal - botBucketVal) / (double)cme.BucketWidth;
		//	////cStart = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);

		//	//double intraStepFactor = escapeVelocity / cme.BucketWidth;
		//	//result = Interpolate(cStart, cme.StartColor.ColorComps, cme.EndColor.ColorComps, intraStepFactor);

		//	var bucketWidth = _bucketWidths[colorMapIndex];
		//	var stepFactor = (countVal + escapeVelocity - botBucketVal) / bucketWidth;
		//	result = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);

		//	return result;

		//}

		#endregion

		private class ColorMapEntry : ICloneable
		{
			#region Constructor

			public ColorMapEntry(ColorBand cb) : this(cb.Cutoff, cb.StartColor, cb.BlendStyle, cb.ActualEndColor, cb.StartingCutoff, cb.BucketWidth)
			{ }

			public ColorMapEntry(int cutoff, ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor, 
                int startingCutoff, int bucketWidth)
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

			#endregion

			public override string? ToString()
			{
				return $"Starting Cutoff: {StartingCutoff}, Ending Cutoff: {Cutoff}, Start: {StartColor.GetCssColor()}, Blend: {BlendStyle}, End: {EndColor.GetCssColor()}.";
			}

			object ICloneable.Clone()
			{
				return Clone();
			}

			public ColorMapEntry Clone()
			{
				return new ColorMapEntry(Cutoff, StartColor, BlendStyle, EndColor, StartingCutoff, BucketWidth);
			}
		}
	
	}
}
