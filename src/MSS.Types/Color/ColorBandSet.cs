﻿using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace MSS.Types
{
	public class ColorBandSet : ObservableCollection<ColorBand>, IEquatable<ColorBandSet>, IEqualityComparer<ColorBandSet?>, ICloneable, INotifyPropertyChanged
	{
		private static readonly ColorBand DEFAULT_HIGH_COLOR_BAND = new(1000, new ColorBandColor("#FFFFFF"), ColorBandBlendStyle.End, new ColorBandColor("#000000"));

		private string? _name;
		private string? _description;

		#region Constructor

		public ColorBandSet() : this(ObjectId.Empty, null)
		{ }

		public ColorBandSet(IList<ColorBand>? colorBands) : this(ObjectId.Empty, colorBands)
		{ }

		public ColorBandSet(ObjectId projectId, IList<ColorBand>? colorBands)
			: this(ObjectId.GenerateNewId(), null, projectId, null, null, colorBands)
		{ }

		public ColorBandSet(ObjectId id, ObjectId? parentId, ObjectId projectId, string? name, string? description, IList<ColorBand>? colorBands) : base(FixBands(colorBands))
		{
			Debug.WriteLine($"Constructing ColorBandSet with id: {id}.");
			Id = id;
			ParentId = parentId;
			ProjectId = projectId;
			_name = name;
			_description = description;

			//DateCreated = id == ObjectId.Empty ? DateTime.UtcNow : id.CreationTime;
		}

		#endregion

		#region Public Properties

		public ObjectId Id { get; set; }
		public ObjectId? ParentId { get; set; }
		public ObjectId ProjectId { get; set; }

		//public DateTime DateCreated { get; private set; }
		public DateTime DateCreated => Id.CreationTime; //{ get; private set; }


		public string? Name
		{
			get => _name;
			set
			{
				if (value != _name)
				{
					_name = value;
					OnPropertyChanged();
				}
			}
		}

		public string? Description
		{
			get => _description;
			set
			{
				if (value != _description)
				{
					_description = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Public Properties - Derived

		public ObservableCollection<ColorBand> ColorBands => this;

		public bool AssignedToProject => ProjectId != ObjectId.Empty;

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

		#region Public Methods

		public void Fix()
		{
			if (Items == null || Count == 0)
			{
				Insert(0, DEFAULT_HIGH_COLOR_BAND.Clone());
			}
			else
			{
				int? prevCutOff = null;

				for (var i = 0; i < Count - 1; i++)
				{
					var cb = Items[i];
					cb.PreviousCutOff = prevCutOff;
					cb.SuccessorStartColor = Items[i + 1].StartColor;
					prevCutOff = cb.CutOff;
				}

				var lastCb = Items[Count - 1];
				lastCb.PreviousCutOff = prevCutOff;
			}
		}

		//public static ColorBandColor GetActualEndColor(ColorBand colorBand, ColorBandColor? nextStartColor)
		//{
		//	if (colorBand.BlendStyle == ColorBandBlendStyle.Next)
		//	{
		//		if (nextStartColor == null)
		//		{
		//			throw new InvalidOperationException("The last ColorBand in the set has a BlendStyle of Next.");
		//		}
		//		return nextStartColor.Value;
		//	}
		//	else
		//	{
		//		return colorBand.BlendStyle == ColorBandBlendStyle.None ? colorBand.StartColor : colorBand.EndColor;
		//	}
		//}

		#endregion

		#region Collection Methods

		protected override void ClearItems()
		{
			base.ClearItems();
			Add(DEFAULT_HIGH_COLOR_BAND.Clone());
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

		protected override void SetItem(int index, ColorBand item)
		{
			base.SetItem(index, item);
			UpdateItemAndNeighbors(index, item);
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

		private static IList<ColorBand> FixBands(IList<ColorBand>? colorBands)
		{
			IList<ColorBand> result;

			if (colorBands == null || colorBands.Count == 0)
			{
				result = new List<ColorBand> { DEFAULT_HIGH_COLOR_BAND.Clone() };
			}
			else
			{
				result = new List<ColorBand>(colorBands);

				int? prevCutOff = null;
				for (var i = 0; i < colorBands.Count - 1; i++)
				{
					var cb = colorBands[i];
					cb.PreviousCutOff = prevCutOff;
					cb.SuccessorStartColor = colorBands[i + 1].StartColor;
					prevCutOff = cb.CutOff;
				}

				var lastCb = colorBands[colorBands.Count - 1];

				Debug.Assert(lastCb.BlendStyle != ColorBandBlendStyle.Next, "The last item in the list of ColorBands being used to construct a ColorBandSet has its BlendStyle set to 'Next.'");

				lastCb.PreviousCutOff = prevCutOff;
			}

			return result;
		}

		#endregion

		#region Clone Support

		/// <summary>
		/// Receives a new ObjectId and becomes a child of this ColorBandSet.
		/// </summary>
		/// <returns></returns>
		public ColorBandSet CreateNewCopy()
		{
			var result = new ColorBandSet(ObjectId.GenerateNewId(), Id, ProjectId, Name, Description, CreateCopy());
			return result;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		/// <summary>
		/// Preserves the value of SerialNumber
		/// </summary>
		/// <returns></returns>
		public ColorBandSet Clone()
		{
			Debug.WriteLine($"Cloning ColorBandSet with Id: {Id}.");

			var result = new ColorBandSet(Id, ParentId, ProjectId, Name, Description, CreateCopy());
			//result.DateCreated = DateCreated;
			return result;
		}

		private IList<ColorBand> CreateCopy()
		{
			var result = Items.Select(x => x.Clone()).ToList();
			return result;
		}

		#endregion

		#region ToString Support

		public override string ToString()
		{
			var result = $"ColorBandSet: {Id}\n{GetString(this)}";
			return result;
		}

		public static string GetString(ICollection<ColorBand> colorBands)
		{
			var sb = new StringBuilder();

			foreach (var cb in colorBands)
			{
				_ = sb.AppendLine(cb.ToString());
			}

			return sb.ToString();
		}

		#endregion

		#region IEquatable and IEqualityComparer Support

		public override bool Equals(object? obj)
		{
			return Equals(obj as ColorBandSet);
		}

		public bool Equals(ColorBandSet? other)
		{
			return other != null
				&& Id.Equals(other.Id);
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public bool Equals(ColorBandSet? x, ColorBandSet? y)
		{
			return x is null ? y is null : x.Equals(y);
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

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			base.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
		}
	}
}
