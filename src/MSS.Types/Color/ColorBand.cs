using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSS.Types
{
	public class ColorBand : INotifyPropertyChanged/*, IEquatable<ColorBand?>*/ ,IEditableObject, ICloneable
	{
		private int _cutOff;
		private ColorBandColor _startColor;
		private ColorBandBlendStyle _blendStyle;
		private ColorBandColor _endColor;

		private int? _previousCutOff;
		private ColorBandColor? _successorStartColor;

		private double _percentage;

		private ColorBand? _copy;
		private bool _isInEditMode;

		#region Constructor

		public ColorBand() : this(0, ColorBandColor.White, ColorBandBlendStyle.None, ColorBandColor.Black)
		{ }

		public ColorBand(int cutOff, string startCssColor, ColorBandBlendStyle blendStyle, string endCssColor)
			: this(cutOff, new ColorBandColor(startCssColor), blendStyle, new ColorBandColor(endCssColor))
		{ }

		public ColorBand(int cutOff, ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor) : this(cutOff, startColor, blendStyle, endColor, null, null, 0)
		{ }

		public ColorBand(int cutOff, ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor, int? previousCutoff, ColorBandColor? successorStartColor, double percentage)
		{
			_cutOff = cutOff;
			_startColor = startColor;
			_blendStyle = blendStyle;
			_endColor = endColor;
			_previousCutOff = previousCutoff;
			_successorStartColor = successorStartColor;
			_percentage = percentage;
		}


		#endregion

		#region Public Properties

		public int CutOff
		{
			get => _cutOff;
			set
			{
				if (value != _cutOff)
				{
					_cutOff = value;
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

					if (origVal == ColorBandBlendStyle.Next || value == ColorBandBlendStyle.Next)
					{
						OnPropertyChanged(nameof(ActualEndColor));
					}
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

					if (BlendStyle == ColorBandBlendStyle.End)
					{
						OnPropertyChanged(nameof(ActualEndColor));
					}
				}

			}
		}

		public int? PreviousCutOff
		{
			get => _previousCutOff;
			set
			{
				if (value != _previousCutOff)
				{
					var origVal = _previousCutOff;
					_previousCutOff = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(StartingCutOff));

					if (origVal.HasValue != value.HasValue)
					{
						OnPropertyChanged(nameof(IsFirst));
					}

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

					if (BlendStyle == ColorBandBlendStyle.Next)
					{
						OnPropertyChanged(nameof(ActualEndColor));
					}

					if (origVal.HasValue != value.HasValue)
					{
						OnPropertyChanged(nameof(IsLast));
					}

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

		public int StartingCutOff => (_previousCutOff ?? -1) + 1;

		public bool IsFirst => !_previousCutOff.HasValue;
		public bool IsLast => !_successorStartColor.HasValue;
		public int BucketWidth => CutOff - (PreviousCutOff ?? 0);

		public ColorBandColor ActualEndColor
		{
			get
			{
				if (_successorStartColor != null)
				{
					return BlendStyle == ColorBandBlendStyle.Next
					   ?					_successorStartColor.Value
					   : BlendStyle == ColorBandBlendStyle.None
						   ? StartColor
						   : EndColor;
				}
				else
				{
					return BlendStyle == ColorBandBlendStyle.Next
                        ?                     throw new InvalidProgramException("BlendStyle is Next, but SuccessorStartColor is null.")
						: BlendStyle == ColorBandBlendStyle.None
                        ? StartColor
                        : EndColor;
				}
			}
			set
			{
				if (BlendStyle == ColorBandBlendStyle.End)
				{
					// Must use backing to avoid loops.
					_endColor = value;
				}
			}
		}


		#endregion

		#region Public Methods

		public void UpdateWithNeighbors(ColorBand? predecessor, ColorBand? successor)
		{
			PreviousCutOff = predecessor?.CutOff;
			SuccessorStartColor = successor?.StartColor;

			//StartingCutOff = predecessor == null ? 0 : predecessor.CutOff + 1;
			//PreviousCutOff = predecessor == null ? 0 : predecessor.CutOff;

			//if (BlendStyle == ColorBandBlendStyle.Next)
			//{
			//	var followingStartColor = successor?.StartColor ?? throw new InvalidOperationException("Must have a successor if the blend style is set to Next.");
			//	ActualEndColor = followingStartColor;
			//}
			//else
			//{
			//	ActualEndColor = BlendStyle == ColorBandBlendStyle.End ? EndColor : StartColor;
			//}

			//if (predecessor != null && predecessor.BlendStyle == ColorBandBlendStyle.Next)
			//{
			//	predecessor.ActualEndColor = StartColor;
			//}
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public ColorBand Clone()
		{
			var result = new ColorBand(CutOff, StartColor, BlendStyle, EndColor, _previousCutOff, _successorStartColor, Percentage);

			return result;
		}

		#endregion

		#region IEditable Object Support

		public void BeginEdit()
		{
			_copy = Clone();
			IsInEditMode = true;
		}

		public void CancelEdit()
		{
			if (_copy != null)
			{
				CutOff = _copy.CutOff;
				StartColor = _copy.StartColor;
				BlendStyle = _copy.BlendStyle;
				EndColor = _copy.EndColor;
				PreviousCutOff = _copy.PreviousCutOff;
				_successorStartColor = _copy._successorStartColor;
				Percentage = _copy.Percentage;
			}
			else
			{
				throw new InvalidOperationException("_copy is null on Cancel Edit.");
			}

			IsInEditMode = false;
		}

		public void EndEdit()
		{
			IsInEditMode = false;
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

		public override string? ToString()
		{
			return $"Starting CutOff: {StartingCutOff}, Ending CutOff: {CutOff}, Start: {StartColor.GetCssColor()}, End: {EndColor.GetCssColor()}, Blend: {BlendStyle}, Actual End: {ActualEndColor}";
		}

		#region IEquatable and IEqualityComparer Support

		//public override bool Equals(object? obj)
		//{
		//	return Equals(obj as ColorBand);
		//}

		//public bool Equals(ColorBand? other)
		//{
		//	bool result = other != null
		//		&& _cutOff == other._cutOff
		//		&& _startColor.Equals(other._startColor)
		//		&& _blendStyle == other._blendStyle
		//		&& _endColor.Equals(other._endColor)
		//		&& _previousCutOff == other._previousCutOff
		//		&& _actualEndColor.Equals(other._actualEndColor)
		//		;//&& _percentage == other._percentage;

		//	return result;
		//}

		//public override int GetHashCode()
		//{
		//	return HashCode.Combine(_cutOff, _startColor, _blendStyle, _endColor, _previousCutOff, _actualEndColor, _percentage);
		//}

		//public static bool operator ==(ColorBand? left, ColorBand? right)
		//{
		//	return EqualityComparer<ColorBand>.Default.Equals(left, right);
		//}

		//public static bool operator !=(ColorBand? left, ColorBand? right)
		//{
		//	return !(left == right);
		//}

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
