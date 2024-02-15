using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSS.Types
{
	public class ColorBand : INotifyPropertyChanged, IEditableObject, ICloneable, IEquatable<ColorBand?>
	{
		#region Private Fields

		private int _cutoff;
		private ColorBandColor _startColor;
		private ColorBandBlendStyle _blendStyle;
		private ColorBandColor _endColor;

		private int? _previousCutoff;
		private ColorBandColor? _successorStartColor;

		private ColorBandColor _actualEndColor;

		private double _percentage;

		//private ColorBand? _copy;
		private bool _isInEditMode;

		private bool _isSelected;
		private bool _isLast;

		private static ColorBand _emptySingleton = new ColorBand();

		#endregion

		#region Constructor

		public ColorBand() 
			: this(0, ColorBandColor.White, ColorBandBlendStyle.End, ColorBandColor.Black, double.NaN)
		{ }

		public ColorBand(int cutoff, string startCssColor, ColorBandBlendStyle blendStyle, string endCssColor)
			: this(cutoff, new ColorBandColor(startCssColor), blendStyle, new ColorBandColor(endCssColor), percentage: double.NaN)
		{ }

		public ColorBand(int cutoff, string startCssColor, ColorBandBlendStyle blendStyle, string endCssColor, double percentage)
			: this(cutoff, new ColorBandColor(startCssColor), blendStyle, new ColorBandColor(endCssColor), percentage)
		{ }

		public ColorBand(int cutoff, ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor)
			: this(cutoff, startColor, blendStyle, endColor, previousCutoff: null, successorStartColor: null, percentage: double.NaN)
		{ }

		public ColorBand(int cutoff, ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor, double percentage)
			: this(cutoff, startColor, blendStyle, endColor, previousCutoff: null, successorStartColor: null, percentage)
		{ }

		public ColorBand(int cutoff, ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor, int? previousCutoff, ColorBandColor? successorStartColor, double percentage)
		{
			_cutoff = cutoff;
			_startColor = startColor;
			_blendStyle = blendStyle;
			_endColor = endColor;
			_previousCutoff = previousCutoff;
			_successorStartColor = successorStartColor;
			_percentage = percentage;

			_actualEndColor = GetActualEndColor();

			_isSelected = false;
			_isLast = false;
		}




		#endregion

		#region Events

		public event EventHandler? EditEnded;

		#endregion

		#region Public Properties

		/// <summary>
		/// Return the shared, single, empty instance.
		/// </summary>
		public static ColorBand Empty => _emptySingleton;

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (value != _isSelected)
				{
					_isSelected = value;
					OnPropertyChanged();
				}
			}
		}

		public double Percentage
		{
			get => _percentage;
			set
			{
				if (value != _percentage)
				{
					_percentage = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Public Properties - Cutoff

		public int Cutoff
		{
			get => _cutoff;
			set
			{
				if (value != _cutoff)
				{
					_cutoff = value;
					OnPropertyChanged();
				}
			}
		}

		public int? PreviousCutoff
		{
			get => _previousCutoff;
			set
			{
				if (value != _previousCutoff)
				{
					var origVal = _previousCutoff;
					_previousCutoff = value;
					OnPropertyChanged();

					if (origVal.HasValue != value.HasValue)
					{
						OnPropertyChanged(nameof(IsFirst));
					}
				}
			}
		}

		public bool IsLast
		{
			get => _isLast;

			set
			{
				if (value != _isLast)
				{
					_isLast = value;
					EndColor = GetActualEndColor();
				}
			}
		}

		//public int StartingCutoff => (_previousCutoff ?? -1) + 1;
		//public int StartingCutoff => (_previousCutoff ?? 0) + 1;	// Updated on 12/26/2023

		public bool IsFirst => !_previousCutoff.HasValue;


		//public int BucketWidth => Cutoff - StartingCutoff;
		public int BucketWidth => Cutoff - (_previousCutoff ?? 0);  // Updated on 12/26/2023

		// cutoff = (_previousCutoff ?? 0) + BucketWidth


		/* Relationship between Cutoff and StartingCutoff

		Width = StartingCutoff (the minimum count value) subtracted from Cutoff (the maximum count value), using double precision math.
		
		Count value matching the Cutoff belongs to that bucket.
		Count values > Target belong to the last bucket.

		NOTE: If the result of the first iteration > threshold then for that sample point the count is zero.

		Example:

		Target = 20

		Cutoffs		5		10		15		max count value
		Start		0		5		10		15
		Width		5		5		5		max count value - 15


		> Previous and <= Cutoff
					> 0 AND <= 5
					> 5 AND <= 10
					> 10 AND <= 15
					> 15 
		
		>= Starting Cutoffand <= Ending Cutoff
					>= 1 and <= 5
					>= 6 and <= 10
					>= 10 and <= 15
					>= 16

		The previous item's cutoff is this item's Previous Cutoff.
		This items' Starting Cutoff is = to this item's Previous Cutoff + 1

		For example Cutoffs = 1,  10, 11
		Previous Cutoffs =    0,  1,  10, 
		Starting Cutoffs =    1,  2,  11

		Using Starting Cutoffs
		0 - 0
		1 - 10
		11 - 11
		Bucket Widths = 0, 9, 0
		The Bucket Width must be >= 0

		Using Previous Cutoffs
		-1 -> 0	
		0 -> 10
		10 -> 11

		Bucket Widths = 1, 10, 1
		The Bucket Width must be >= 1



		| ------------------||--------------------|
		0	1	2	3	4	5	6	7	8	9	10
		*/

		#endregion

		#region Public Properties - Color

		public ColorBandColor StartColor
		{
			get => _startColor;
			set
			{
				if (value != _startColor)
				{
					//if (value == ColorBandColor.Black)
					//{
					//	Debug.WriteLine("Setting the Start Color to Black.");
					//}

					_startColor = value;
					OnPropertyChanged();

					ActualEndColor = GetActualEndColor();
				}
			}
		}

		public ColorBandBlendStyle BlendStyle
		{
			get => _blendStyle;
			set
			{
				if (value != _blendStyle)
				{
					_blendStyle = value;
					OnPropertyChanged();

					ActualEndColor = GetActualEndColor();
				}
			}
		}

		public ColorBandColor EndColor
		{
			get => _endColor;
			set
			{
				if (value != _endColor)
				{
					_endColor = value;
					OnPropertyChanged();

					ActualEndColor = GetActualEndColor();
				}
			}
		}

		public ColorBandColor? SuccessorStartColor
		{
			get => _successorStartColor;
			set
			{
				if (value != _successorStartColor)
				{
					_successorStartColor = value;
					ActualEndColor = GetActualEndColor();
				}
			}
		}

		public ColorBandColor ActualEndColor
		{
			get => _actualEndColor;
			set
			{
				if (value != _actualEndColor)
				{
					_actualEndColor = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Private Methods

		private ColorBandColor GetActualEndColor()
		{
			if (IsLast)
			{
				return ColorBandColor.Black;
			}

			var result = BlendStyle == ColorBandBlendStyle.Next
				? GetSuccessorStartColor()
				: BlendStyle == ColorBandBlendStyle.End
					? EndColor
					: StartColor;

			return result;
		}

		private ColorBandColor GetSuccessorStartColor()
		{
			var result = SuccessorStartColor ?? EndColor; // throw new InvalidOperationException("BlendStyle is Next, but SuccessorStartColor is null.");
			return result;
		}

		#endregion

		#region Public Methods

		public void UpdateWithNeighbors(ColorBand? predecessor, ColorBand? successor)
		{
			PreviousCutoff = predecessor?.Cutoff;

			if (successor != null)
			{
				SuccessorStartColor = successor?.StartColor;
			}
			else
			{
				SuccessorStartColor = null;
				IsLast = true;
			}
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public ColorBand Clone()
		{
			var result = new ColorBand(Cutoff, StartColor, BlendStyle, EndColor, _previousCutoff, _successorStartColor, Percentage)
			{
				IsLast = IsLast
			};

			return result;
		}

		public override string? ToString()
		{
			return $"Previous Cutoff: {PreviousCutoff}, Ending Cutoff: {Cutoff}, Start: {StartColor.GetCssColor()}, Blend: {BlendStyle}, End: {EndColor.GetCssColor()}, Actual End: {ActualEndColor}";
		}

		#endregion

		#region IEditable Object Support

		public void BeginEdit()
		{
			//_copy = Clone();

			if (IsInEditMode)
			{
				Debug.WriteLine("WARNING: BeginEdit is being called, but IsInEditMode is already true.");
			}

			IsInEditMode = true;
		}

		public void CancelEdit()
		{
			//if (_copy != null)
			//{
			//	Cutoff = _copy.Cutoff;
			//	StartColor = _copy.StartColor;
			//	BlendStyle = _copy.BlendStyle;
			//	EndColor = _copy.EndColor;
			//	PreviousCutoff = _copy.PreviousCutoff;
			//	_successorStartColor = _copy._successorStartColor;
			//	Percentage = _copy.Percentage;
			//}
			//else
			//{
			//	throw new InvalidOperationException("_copy is null on Cancel Edit.");
			//}

			//_copy = null;

			if (!IsInEditMode)
			{
				Debug.WriteLine("WARNING: CancelEdit is being called, but IsInEditMode = false.");
			}

			IsInEditMode = false;
		}

		public void EndEdit()
		{
			//_copy = null;

			if (!IsInEditMode)
			{
				Debug.WriteLine("WARNING: EndEdit is being called, but IsInEditMode = false.");
			}

			Debug.WriteLine($"EndEdit is being called for ColorBand with StartColor: {StartColor}, Previous Offset: {PreviousCutoff}, Ending Offset: {Cutoff}.");

			IsInEditMode = false;
			EditEnded?.Invoke(this, EventArgs.Empty);
		}

		public bool IsInEditMode
		{
			get => _isInEditMode;
			private set
			{
				if (_isInEditMode != value)
				{
					_isInEditMode = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region NotifyPropertyChanged Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		#region IEquatable Support

		public override bool Equals(object? obj)
		{
			return Equals(obj as ColorBand);
		}

		public bool Equals(ColorBand? other)
		{
			return other is not null &&
				   _cutoff == other._cutoff;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(_cutoff);
		}

		public static bool operator ==(ColorBand? left, ColorBand? right)
		{
			return EqualityComparer<ColorBand>.Default.Equals(left, right);
		}

		public static bool operator !=(ColorBand? left, ColorBand? right)
		{
			return !(left == right);
		}

		#endregion
	}
}
