using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Types
{
	public class ColorBandSetW : ObservableCollection<ColorBandW>, IEquatable<ColorBandSetW>, IEqualityComparer<ColorBandSetW>, IColorBandSet<ColorBandW>, ICloneable
	{
		private static readonly ColorBandW DEFAULT_HIGH_COLOR_BAND = new ColorBandW(1000, new ColorBandColor("#FFFFFF"), ColorBandBlendStyle.End, new ColorBandColor("#000000"));

		#region Constructor

		public ColorBandSetW() : this(Guid.NewGuid(), new List<ColorBandW>())
		{ }

		public ColorBandSetW(Guid serialNumber) : this(serialNumber, new List<ColorBandW>())
		{ }

		public ColorBandSetW(IList<ColorBandW> list) : this(Guid.NewGuid(), list)
		{ }

		public ColorBandSetW(Guid serialNumber, IList<ColorBandW> colorBands) : base(FixBands(colorBands))
		{
			Debug.WriteLine($"Constructing ColorBandSet with SerialNumber: {serialNumber}.");
			SerialNumber = serialNumber;
		}

		public static IList<ColorBandW> FixBands(IList<ColorBandW> colorBands)
		{
			if (colorBands == null || colorBands.Count == 0)
			{
				return new List<ColorBandW> { DEFAULT_HIGH_COLOR_BAND.Clone() };
			}

			var result = new List<ColorBandW>(colorBands);

			if (colorBands.Count > 1)
			{
				var prevCutOff = 0;
				for (var i = 0; i < colorBands.Count - 1; i++)
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

		public ObservableCollection<ColorBandW> ColorBands => this;

		public bool IsReadOnly => false;

		public ColorBandW HighColorBand
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

		protected override void ClearItems()
		{
			base.ClearItems();
			Add(DEFAULT_HIGH_COLOR_BAND.Clone());
		}

		protected override void InsertItem(int index, ColorBandW item)
		{
			base.InsertItem(index, item);
			UpdateItemAndNeighbors(index, item);
		}

		protected override void RemoveItem(int index)
		{
			base.RemoveItem(index);
			if (Count == 0)
			{
				Add(DEFAULT_HIGH_COLOR_BAND.Clone());
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

		protected override void SetItem(int index, ColorBandW item)
		{
			base.SetItem(index, item);
			UpdateItemAndNeighbors(index, item);
		}

		private void UpdateItemAndNeighbors(int index, ColorBandW item)
		{
			var colorBands = GetItemAndNeighbors(index, item);

			for (var i = 0; i < colorBands.Count; i++)
			{
				var cb = colorBands[i];
				cb.UpdateWithNeighbors(GetPreviousItem(i), GetNextItem(i));
			}
		}

		private IList<ColorBandW> GetItemAndNeighbors(int index, ColorBandW item)
		{
			var result = new List<ColorBandW>();

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

		private ColorBandW? GetPreviousItem(int index)
		{
			return index <= 0 ? null : Items[index - 1];
		}

		private ColorBandW? GetNextItem(int index)
		{
			return index >= Count - 1 ? null : Items[index + 1];
		}

		#endregion

		#region Clone Support

		public IColorBandSet<ColorBandW> CreateNewCopy()
		{
			return new ColorBandSetW(CreateCopy());
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public IColorBandSet<ColorBandW> Clone()
		{
			Debug.WriteLine($"Cloning ColorBandSet with SerialNumber: {SerialNumber}.");

			return new ColorBandSetW(SerialNumber, CreateCopy());
		}

		private IList<ColorBandW> CreateCopy()
		{
			var result = Items.Select(x => x.Clone()).ToList();
			return result;
		}

		#endregion

		public override string ToString()
		{
			return $"{SerialNumber}:{base.ToString()}";
		}

		#region IEquatable Support

		public override bool Equals(object? obj)
		{
			return Equals(obj as ColorBandSetW);
		}

		public bool Equals(ColorBandSetW? other)
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

		public bool Equals(ColorBandSetW? x, ColorBandSetW? y)
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

		public int GetHashCode([DisallowNull] ColorBandSetW obj)
		{
			return GetHashCode(obj);
		}

		public static bool operator ==(ColorBandSetW? left, ColorBandSetW? right)
		{
			return EqualityComparer<ColorBandSetW>.Default.Equals(left, right);
		}

		public static bool operator !=(ColorBandSetW? left, ColorBandSetW? right)
		{
			return !(left == right);
		}

		#endregion
	}
}
