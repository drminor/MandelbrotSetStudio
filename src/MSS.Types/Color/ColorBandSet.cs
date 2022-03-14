﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	public class ColorBandSet : Collection<ColorBand>, IEquatable<ColorBandSet?>, IEqualityComparer<ColorBandSet>, ICloneable
	{
		public ColorBandSet() : this(Guid.NewGuid())
		{ }

		public ColorBandSet(Guid serialNumber) : base()
		{
			SerialNumber = serialNumber;
		}

		public ColorBandSet(IList<ColorBand> list) : this(Guid.NewGuid(), list)
		{ }

		public ColorBandSet(Guid serialNumber, IList<ColorBand> colorBands) : base(colorBands)
		{
			SerialNumber = serialNumber;
		}

		public Guid SerialNumber { get; set; }

		public bool IsEmpty => Count > 0;

		public ColorBand? HighColorBand => IsEmpty ? null : base[^1];

		public int? HighCutOff => HighColorBand?.CutOff;
		public ColorBandColor? HighColor => HighColorBand?.StartColor;

		public bool TrySetHighColor(ColorBandColor highColor)
		{
			if (Count > 0)
			{
				var currentBand = base[^1];
				base[^1] = new ColorBand(currentBand.CutOff, highColor, ColorBandBlendStyle.None, highColor);
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool TrySetHighCutOff(int cutOff)
		{
			if (Count > 0)
			{
				var currentBand = base[^1];
				base[^1] = new ColorBand(cutOff, currentBand.StartColor, currentBand.BlendStyle, currentBand.EndColor);
				return true;
			}
			else
			{
				return false;
			}
		}

		public ColorBandSet CreateNewCopy()
		{
			return new ColorBandSet(CreateCopy());
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public ColorBandSet Clone()
		{
			return new ColorBandSet(SerialNumber, CreateCopy());
		}

		private IList<ColorBand> CreateCopy()
		{
			var result = new List<ColorBand>();

			foreach (var cme in Items)
			{
				result.Add(cme.Clone());
			}

			return result;
		}

		public override string? ToString()
		{
			return $"{SerialNumber}:{base.ToString()}";
		}

		#region IEquatable Support

		public override bool Equals(object? obj)
		{
			return Equals(obj as ColorBandSet);
		}

		public bool Equals(ColorBandSet? other)
		{
			return other != null &&
				   //Count == other.Count &&
				   //EqualityComparer<IList<ColorBand>>.Default.Equals(Items, other.Items) &&
				   SerialNumber.Equals(other.SerialNumber);
		}

		public override int GetHashCode()
		{
			return SerialNumber.GetHashCode();
		}

		public bool Equals(ColorBandSet? x, ColorBandSet? y)
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

		public int GetHashCode([DisallowNull] ColorBandSet obj)
		{
			return GetHashCode(obj);
		}

		public static bool operator ==(ColorBandSet? left, ColorBandSet? right)
		{
			return EqualityComparer<ColorBandSet>.Default.Equals(left, right);
		}

		public static bool operator !=(ColorBandSet? left, ColorBandSet? right)
		{
			return !(left == right);
		}

		#endregion
	}
}
