﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MSS.Types
{
	public class ColorBand : IColorBand, ICloneable, IEquatable<ColorBand?>
	{
		private int _cutOff;
		private ColorBandColor _startColor;
		private ColorBandBlendStyle _blendStyle;
		private ColorBandColor _endColor;

		private int _previousCutOff;
		private ColorBandColor _actualEndColor;

		private double _percentage;

		#region Constructor

		public ColorBand(int cutOff, string startCssColor, ColorBandBlendStyle blendStyle, string endCssColor)
			: this(cutOff, new ColorBandColor(startCssColor), blendStyle, new ColorBandColor(endCssColor))
		{
		}

		public ColorBand(int cutOff, ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor)
		{
			CutOff = cutOff;
			StartColor = startColor;
			BlendStyle = blendStyle;
			EndColor = endColor;
			ActualEndColor = BlendStyle == ColorBandBlendStyle.None ? StartColor : EndColor;

			Percentage = 0;
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
					_blendStyle = value;
					OnPropertyChanged();
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
				}
			}
		}

		//public string BlendStyleAsString => GetBlendStyleAsString(BlendStyle);

		public int PreviousCutOff
		{
			get => _previousCutOff;
			set
			{
				if (value != _previousCutOff)
				{
					_previousCutOff = value;
					OnPropertyChanged();
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

		public int BucketWidth => CutOff - PreviousCutOff;

		#endregion

		#region Public Methods

		public void UpdateWithNeighbors(IColorBand? predecessor, IColorBand? successor)
		{
			PreviousCutOff = predecessor == null ? 0 : predecessor.CutOff;

			if (BlendStyle == ColorBandBlendStyle.Next)
			{
				var followingStartColor = successor?.StartColor ?? throw new InvalidOperationException("Must have a successor if the blend style is set to Next.");
				ActualEndColor = followingStartColor;
			}
			else
			{
				ActualEndColor = BlendStyle == ColorBandBlendStyle.End ? EndColor : StartColor;
			}
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		IColorBand IColorBand.Clone()
		{
			return Clone();
		}

		public ColorBand Clone()
		{
			var result = new ColorBand(CutOff, StartColor, BlendStyle, EndColor);

			return result;
		}

		#endregion

		public override string? ToString()
		{
			return $"CutOff: {_cutOff}, Start: {_startColor.GetCssColor()}, End: {_endColor.GetCssColor()}, Blend: {_blendStyle}, Previous CutOff: {_previousCutOff}, Actual End: {_actualEndColor}";
		}

		#region IEquatable and IEqualityComparer Support


		public override bool Equals(object? obj)
		{
			return Equals(obj as ColorBand);
		}

		public bool Equals(ColorBand? other)
		{
			bool result = other != null
				&& _cutOff == other._cutOff
				&& _startColor.Equals(other._startColor)
				&& _blendStyle == other._blendStyle
				&& _endColor.Equals(other._endColor)
				&& _previousCutOff == other._previousCutOff
				&& _actualEndColor.Equals(other._actualEndColor)
				;//&& _percentage == other._percentage;

			return result;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(_cutOff, _startColor, _blendStyle, _endColor, _previousCutOff, _actualEndColor, _percentage);
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

		#region NotifyPropertyChanged Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
