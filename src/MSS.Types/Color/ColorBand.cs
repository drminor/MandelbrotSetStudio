using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace MSS.Types
{
	public class ColorBand : INotifyPropertyChanged, IEditableObject, ICloneable
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

		#endregion

		#region Constructor

		public ColorBand() 
			: this(0, ColorBandColor.White, ColorBandBlendStyle.None, ColorBandColor.Black)
		{ }

		public ColorBand(int cutoff, string startCssColor, ColorBandBlendStyle blendStyle, string endCssColor)
			: this(cutoff, new ColorBandColor(startCssColor), blendStyle, new ColorBandColor(endCssColor))
		{ }

		public ColorBand(int cutoff, ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor)
			: this(cutoff, startColor, blendStyle, endColor, null, null, 0)
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
		}

		// Create a single instance for all to use.
		//private static ColorBand _emptySingleton = new ColorBand();

		// Create a new intance on each reference
		private static ColorBand _emptySingleton => new ColorBand();

		public static ColorBand Empty => _emptySingleton;

		#endregion

		#region Events

		public event EventHandler? EditEnded;

		#endregion

		#region Public Properties

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
					var origVal = _blendStyle;
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


		private bool CheckPreviousCutoff(int prevCutoff, int cutoff)
		{
			var thisColorBandsStartingCutoff = prevCutoff + 1;
			var diff = cutoff - thisColorBandsStartingCutoff;

			var result = diff >= 0;

			return result;
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

		public int StartingCutoff => (_previousCutoff ?? -1) + 1;

		public bool IsFirst => !_previousCutoff.HasValue;
		public bool IsLast => !_successorStartColor.HasValue;
		public int BucketWidth => Cutoff - StartingCutoff;

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
	}
}
