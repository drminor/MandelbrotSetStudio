﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace MSS.Types
{
	public class ColorBandSet : ObservableCollection<ColorBand>, IColorBandSet<ColorBand>, IEquatable<ColorBandSet>, IEqualityComparer<ColorBandSet>, ICloneable, INotifyPropertyChanged
	{
		private static readonly ColorBand DEFAULT_HIGH_COLOR_BAND = new ColorBand(1000, new ColorBandColor("#FFFFFF"), ColorBandBlendStyle.End, new ColorBandColor("#000000"));

		#region Constructor

		public ColorBandSet() : this(Guid.NewGuid(), new List<ColorBand>())
		{ }

		public ColorBandSet(Guid serialNumber) : this(serialNumber, new List<ColorBand>())
		{ }

		public ColorBandSet(IList<ColorBand> list) : this(Guid.NewGuid(), list)
		{ }

		public ColorBandSet(Guid serialNumber, IList<ColorBand> colorBands) : base(FixBands(colorBands))
		{
			Debug.WriteLine($"Constructing ColorBandSet with SerialNumber: {serialNumber}.");
			SerialNumber = serialNumber;
		}

		#endregion

		#region Public Properties

		public Guid SerialNumber { get; set; }

		public ObservableCollection<ColorBand> ColorBands => this;

		public bool IsReadOnly => false;

		public ColorBand HighColorBand
		{
			get => base[^1];
			set => base[^1] = value;
		}

		public int HighCutOff
		{
			get => HighColorBand.CutOff;
			set => base[^1].CutOff = value;
		}

		public ColorBandColor HighStartColor
		{
			get => HighColorBand.StartColor;
			set => base[^1].StartColor = value;
		}

		public ColorBandBlendStyle HighColorBlendStyle
		{
			get => HighColorBand.BlendStyle;
			set
			{
				if (value == ColorBandBlendStyle.Next)
				{
					throw new InvalidOperationException("The HighColorBand cannot have a BlendStyle of Next.");
				}
				base[^1].BlendStyle = value;

			}
		}

		public ColorBandColor HighEndColor
		{
			get => HighColorBand.EndColor;
			set => base[^1].EndColor = value;
		}

		#endregion

		#region Collection Methods

		public void Fix()
		{
			if (Items == null || Count == 0)
			{
				Insert(0, DEFAULT_HIGH_COLOR_BAND.Clone());
			}
			else
			{
				var prevCutOff = 0;

				for (var i = 0; i < Count - 1; i++)
				{
					var cb = Items[i];
					cb.PreviousCutOff = prevCutOff;
					cb.ActualEndColor = GetActualEndColor(cb, Items[i + 1].StartColor);
					prevCutOff = cb.CutOff;
				}

				var lastCb = Items[Count - 1];

				lastCb.PreviousCutOff = prevCutOff;
				lastCb.ActualEndColor = GetActualEndColor(lastCb, null);
			}
		}

		protected override void ClearItems()
		{
			base.ClearItems();
			Add(DEFAULT_HIGH_COLOR_BAND.Clone());
		}

		protected override void InsertItem(int index, ColorBand item)
		{
			base.InsertItem(index, item);
			//UpdateItemAndNeighbors(index, item);
		}

		protected override void RemoveItem(int index)
		{
			base.RemoveItem(index);

			//if (Count == 0)
			//{
			//	Add(DEFAULT_HIGH_COLOR_BAND.Clone());
			//}
			//else
			//{
			//	if (index > Count - 1)
			//	{
			//		index = Count - 1;
			//	}

			//	UpdateItemAndNeighbors(index, Items[index]);
			//}
		}

		protected override void SetItem(int index, ColorBand item)
		{
			base.SetItem(index, item);
			//UpdateItemAndNeighbors(index, item);
		}

		#endregion

		#region Private Methods

		private void UpdateItemAndNeighbors(int index, ColorBand item)
		{
			var colorBands = GetItemAndNeighbors(index, item);
			var prev = GetPreviousItem(index);
			var idx = prev == null ? index : index - 1;

			for (var ptr = 0; ptr < colorBands.Count; ptr++)
			{
				var cb = colorBands[ptr];
				cb.UpdateWithNeighbors(GetPreviousItem(idx), GetNextItem(idx));

				idx++;
			}
		}

		private IList<ColorBand> GetItemAndNeighbors(int index, ColorBand item)
		{
			var result = new List<ColorBand>();

			var prev = GetPreviousItem(index);

			if (prev != null)
			{
				result.Add(prev);
			}

			result.Add(item);

			var next = GetNextItem(index);

			if (next != null)
			{
				result.Add(next);
			}

			return result;
		}

		private ColorBand? GetPreviousItem(int index)
		{
			return index <= 0 ? null : Items[index - 1];
		}

		private ColorBand? GetNextItem(int index)
		{
			return index >= Count - 1 ? null : Items[index + 1];
		}

		private static IList<ColorBand> FixBands(IList<ColorBand> colorBands)
		{
			if (colorBands == null || colorBands.Count == 0)
			{
				return new List<ColorBand> { DEFAULT_HIGH_COLOR_BAND.Clone() };
			}

			var result = new List<ColorBand>(colorBands);

			if (colorBands.Count > 1)
			{
				var prevCutOff = 0;
				for (var i = 0; i < colorBands.Count - 1; i++)
				{
					var cb = colorBands[i];
					cb.PreviousCutOff = prevCutOff;
					cb.ActualEndColor = GetActualEndColor(cb, colorBands[i + 1].StartColor);

					prevCutOff = cb.CutOff;
				}

				var lastCb = colorBands[colorBands.Count - 1];

				Debug.Assert(lastCb.BlendStyle != ColorBandBlendStyle.Next, "The last item in the list of ColorBands being used to construct a ColorBandSet has its BlendStyle set to 'Next.'");

				lastCb.PreviousCutOff = prevCutOff;
				lastCb.ActualEndColor = GetActualEndColor(lastCb, null);
			}

			return result;
		}

		#endregion

		private static ColorBandColor GetActualEndColor(ColorBand colorBand, ColorBandColor? nextStartColor)
		{
			if (colorBand.BlendStyle == ColorBandBlendStyle.Next)
			{
				if (nextStartColor == null)
				{
					throw new InvalidOperationException("The last ColorBand in the set has a BlendStyle of Next.");
				}
				return nextStartColor.Value;
			}
			else
			{
				return colorBand.BlendStyle == ColorBandBlendStyle.None ? colorBand.StartColor : colorBand.EndColor;
			}
		}

		#region Clone Support

		IColorBandSet<ColorBand> IColorBandSet<ColorBand>.CreateNewCopy()
		{
			return CreateNewCopy();
		}

		/// <summary>
		/// Receives a new SerialNumber
		/// </summary>
		/// <returns></returns>
		public ColorBandSet CreateNewCopy()
		{
			return new ColorBandSet(CreateCopy());
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		IColorBandSet<ColorBand> IColorBandSet<ColorBand>.Clone()
		{
			return Clone();
		}

		/// <summary>
		/// Preserves the value of SerialNumber
		/// </summary>
		/// <returns></returns>
		public ColorBandSet Clone()
		{
			Debug.WriteLine($"Cloning ColorBandSet with SerialNumber: {SerialNumber}.");

			return new ColorBandSet(SerialNumber, CreateCopy());
		}

		private IList<ColorBand> CreateCopy()
		{
			var result = Items.Select(x => x.Clone()).ToList();
			return result;
		}

		#endregion

		public override string ToString()
		{
			var result = $"ColorBandSet: {SerialNumber}\n{GetString(this)}";
			return result;
		}

		public static string GetString(ICollection<ColorBand> colorBands)
		{
			var sb = new StringBuilder();

			foreach (var cb in colorBands)
			{
				sb.AppendLine(cb.ToString());
			}

			return sb.ToString();
		}

		#region IEquatable and IEqualityComparer Support

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
