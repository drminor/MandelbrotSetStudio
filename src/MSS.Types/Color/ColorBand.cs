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

		//private bool _isCurrent;
		private bool _isCutoffSelected;
		private bool _isColorSelected;

		#endregion

		#region Constructor

		public ColorBand() 
			: this(0, ColorBandColor.White, ColorBandBlendStyle.None, ColorBandColor.Black)
		{ }

		public ColorBand(int cutoff, string startCssColor, ColorBandBlendStyle blendStyle, string endCssColor)
			: this(cutoff, new ColorBandColor(startCssColor), blendStyle, new ColorBandColor(endCssColor))
		{ }

		public ColorBand(int cutoff, ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor)
			: this(cutoff, startColor, blendStyle, endColor, null, blendStyle == ColorBandBlendStyle.Next ? endColor : null, 0)
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

			//_isCurrent = false;
			_isCutoffSelected = false;
			_isColorSelected = false;
		}

		private static ColorBand _emptySingleton = new ColorBand();

		/// <summary>
		/// Return the shared, single, empty instance.
		/// </summary>
		public static ColorBand Empty => _emptySingleton;

		/// <summary>
		/// Return a new empty intance
		/// </summary>
		public static ColorBand NewEmpty => new ColorBand();

		#endregion

		#region Events

		public event EventHandler? EditEnded;

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
					OnPropertyChanged(nameof(StartingCutoff));

					if (origVal.HasValue != value.HasValue)
					{
						OnPropertyChanged(nameof(IsFirst));
					}
				}
			}
		}

		//public int StartingCutoff => (_previousCutoff ?? -1) + 1;
		public int StartingCutoff => (_previousCutoff ?? 0) + 1;	// Updated on 12/26/2023

		public bool IsFirst => !_previousCutoff.HasValue;
		public bool IsLast => !_successorStartColor.HasValue;

		//public int BucketWidth => Cutoff - StartingCutoff;
		public int BucketWidth => Cutoff - (_previousCutoff ?? 0);  // Updated on 12/26/2023


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

					> 0 AND <= 5
					> 5 AND <= 10
					> 10 AND <= 15
					> 15 
		
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
					var origVal = _successorStartColor;
					_successorStartColor = value;
					OnPropertyChanged();

					if (origVal.HasValue != value.HasValue)
					{
						OnPropertyChanged(nameof(IsLast));
					}

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

		#region Public Properties - Selections

		//public bool IsCurrent
		//{
		//	get => _isCurrent;
		//	set
		//	{
		//		if (value != _isCurrent)
		//		{
		//			_isCurrent = value;
		//		}
		//	}
		//}

		public bool IsSelected
		{
			get => _isCutoffSelected || _isColorSelected;
			set
			{
				if (value != IsSelected)
				{
					IsCutoffSelected = value;
					IsColorSelected = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsCutoffSelected
		{
			get => _isCutoffSelected;
			set
			{
				if (value != _isCutoffSelected)
				{
					_isCutoffSelected = value;
					OnPropertyChanged();

				}
			}
		}

		public bool IsColorSelected
		{
			get => _isColorSelected;
			set
			{
				if (value != _isColorSelected)
				{
					_isColorSelected = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Private Methods

		private ColorBandColor GetActualEndColor()
		{
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
			SuccessorStartColor = successor?.StartColor;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public ColorBand Clone()
		{
			var result = new ColorBand(Cutoff, StartColor, BlendStyle, EndColor, _previousCutoff, _successorStartColor, Percentage);

			return result;
		}

		public override string? ToString()
		{
			return $"Starting Cutoff: {StartingCutoff}, Ending Cutoff: {Cutoff}, Start: {StartColor.GetCssColor()}, Blend: {BlendStyle}, End: {EndColor.GetCssColor()}, Actual End: {ActualEndColor}";
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

			Debug.WriteLine($"EndEdit is being called for ColorBand with StartColor: {StartColor}, Starting Offset: {StartingCutoff}, Ending Offset: {Cutoff}.");

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
