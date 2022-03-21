﻿using MSS.Types;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MSS.Types
{
	public class ColorBand : IColorBand, ICloneable
	{
		#region Constructor

		private int _cutOff;
		private ColorBandColor _startColor;
		private ColorBandBlendStyle _blendStyle;
		private ColorBandColor _endColor;

		private int _previousCutOff;
		private ColorBandColor _actualEndColor;

		private double _percentage;

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
				_startColor = value;
				OnPropertyChanged();
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
				_endColor = value;
				OnPropertyChanged();
			}
		}

		public string BlendStyleAsString => GetBlendStyleAsString(BlendStyle);

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
				_actualEndColor = value;
				OnPropertyChanged();
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

		#region Static Methods

		private static string GetBlendStyleAsString(ColorBandBlendStyle blendStyle)
		{
			return blendStyle switch
			{
				ColorBandBlendStyle.Next => "Next",
				ColorBandBlendStyle.None => "None",
				ColorBandBlendStyle.End => "End",
				_ => "None",
			};
		}

		#endregion


		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

	}
}
