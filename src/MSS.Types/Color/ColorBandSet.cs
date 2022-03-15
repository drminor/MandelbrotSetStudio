using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Types
{
	public class ColorBandSet : Collection<ColorBand>, IEquatable<ColorBandSet?>, IEqualityComparer<ColorBandSet>, ICloneable
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
			SerialNumber = serialNumber;
		}

		public static IList<ColorBand> FixBands(IList<ColorBand> colorBands)
		{
			if (colorBands == null || colorBands.Count == 0)
			{
				return new List<ColorBand> { DEFAULT_HIGH_COLOR_BAND };
			}

			var result = new List<ColorBand>(colorBands);

			if (colorBands.Count > 1)
			{
				var prevCutOff = 0;
				for(var i = 0; i < colorBands.Count - 1; i++)
				{
					var cb = colorBands[i];
					cb.PreviousCutOff = prevCutOff;
					cb.ActualEndColor = cb.BlendStyle == ColorBandBlendStyle.Next ? colorBands[i + 1].StartColor : cb.BlendStyle == ColorBandBlendStyle.None ? cb.StartColor : cb.EndColor;

					prevCutOff = cb.CutOff;
				}

				var lastCb = colorBands[colorBands.Count - 1];

				Debug.Assert(lastCb.BlendStyle != ColorBandBlendStyle.Next, "The last item in the list of ColorBands being used to construct a ColorBandSet has its BlendStyle set to 'Next.'");

				lastCb.PreviousCutOff = prevCutOff;
				lastCb.ActualEndColor = lastCb.BlendStyle == ColorBandBlendStyle.None ? lastCb.StartColor : lastCb.EndColor;
			}

			return result;
		}

		#endregion

		#region Public Properties

		public Guid SerialNumber { get; set; }

		public ColorBand HighColorBand
		{
			get => base[^1];
			set { base[^1] = value; }
		}

		public int HighCutOff
		{
			get => HighColorBand.CutOff;
			set
			{
				var currentBand = base[^1];
				base[^1] = new ColorBand(value, currentBand.StartColor, currentBand.BlendStyle, currentBand.EndColor);
			}
		}

		public ColorBandColor HighStartColor
		{
			get => HighColorBand.StartColor;
			set
			{
				var currentBand = base[^1];
				base[^1] = new ColorBand(currentBand.CutOff, value, currentBand.BlendStyle, currentBand.EndColor);
			}
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
				var currentBand = base[^1];
				base[^1] = new ColorBand(currentBand.CutOff, currentBand.StartColor, value, currentBand.EndColor);
			}
		}

		public ColorBandColor HighEndColor
		{
			get => HighColorBand.EndColor;
			set
			{
				var currentBand = base[^1];
				base[^1] = new ColorBand(currentBand.CutOff, currentBand.StartColor, currentBand.BlendStyle, value);
			}
		}

		#endregion

		#region Collection Methods

		protected override void ClearItems()
		{
			base.ClearItems();
			Add(DEFAULT_HIGH_COLOR_BAND);
		}

		protected override void InsertItem(int index, ColorBand item)
		{
			base.InsertItem(index, item);
			UpdateItemAndNeighbors(index, item);
		}

		protected override void RemoveItem(int index)
		{
			base.RemoveItem(index);
			if (Count == 0)
			{
				Add(DEFAULT_HIGH_COLOR_BAND);
			}
			else
			{
				if (index > Count - 1)
				{
					index = Count - 1;
				}

				UpdateItemAndNeighbors(index, Items[index]);
			}
		}

		protected override void SetItem(int index, ColorBand item)
		{
			base.SetItem(index, item);
			UpdateItemAndNeighbors(index, item);
		}

		private void UpdateItemAndNeighbors(int index, ColorBand item)
		{
			IList<ColorBand> colorBands = GetItemAndNeighbors(index, item);

			for(var i = 0; i < colorBands.Count; i++)
			{
				var cb = colorBands[i];
				cb.UpdateWithNeighbors(GetPreviousItem(i - 1), GetNextItem(i + 1));
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

		#endregion

		#region Clone Support

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
			var result = Items.Select(x => x.Clone()).ToList();
			return result;
		}

		#endregion

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
