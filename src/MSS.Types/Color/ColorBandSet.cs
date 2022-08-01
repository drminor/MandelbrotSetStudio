using MongoDB.Bson;
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
	public class ColorBandSet : ObservableCollection<ColorBand>, IEquatable<ColorBandSet>, IEqualityComparer<ColorBandSet?>, INotifyPropertyChanged, ICloneable
	{
		#region Static Members

		private static readonly ColorBand DEFAULT_HIGH_COLOR_BAND = new(1000, new ColorBandColor("#FFFFFF"), ColorBandBlendStyle.End, new ColorBandColor("#000000"));

		#endregion

		private ObjectId? _parentId;
		private ObjectId _projectId;
		private string? _name;
		private string? _description;
		private readonly Stack<ReservedColorBand> _reservedColorBands;

		private DateTime _lastSavedUtc;

		#region Constructor

		public ColorBandSet() 
			: this(colorBands: null)
		{ }

		public ColorBandSet(IList<ColorBand>? colorBands) 
			: this(projectId: ObjectId.Empty, colorBands, Guid.NewGuid())
		{ }

		public ColorBandSet(IList<ColorBand>? colorBands, Guid colorBandsSerialNumber)
			: this(projectId: ObjectId.Empty, colorBands, colorBandsSerialNumber)
		{ }

		private ColorBandSet(ObjectId projectId, IList<ColorBand>? colorBands, Guid colorBandsSerialNumber)
			: this(ObjectId.GenerateNewId(), parentId: null, projectId, name: null, description: null, colorBands, null, colorBandsSerialNumber)
		{
			LastSavedUtc = DateTime.MinValue;
			OnFile = false;
		}

		public ColorBandSet(ObjectId id, ObjectId? parentId, ObjectId projectId, string? name, string? description, IList<ColorBand>? colorBands, IEnumerable<ReservedColorBand>? reservedColorBands, Guid colorBandsSerialNumber) : base(FixBands(colorBands))
		{
			//Debug.WriteLine($"Constructing ColorBandSet with id: {id}.");

			Id = id;
			_parentId = parentId;
			_projectId = projectId;
			_name = name;
			_description = description;

			_reservedColorBands = reservedColorBands == null ? new Stack<ReservedColorBand>() : new Stack<ReservedColorBand>(reservedColorBands);
			ColorBandsSerialNumber = colorBandsSerialNumber;

			SelectedColorBandIndex = 0;

			_lastSavedUtc = DateTime.UtcNow;
			LastUpdatedUtc = DateTime.MinValue;
			OnFile = true;
		}

		#endregion

		#region Public Properties - Derived

		public bool IsReadOnly => false;
		public bool IsAssignedToProject => ProjectId != ObjectId.Empty;
		public bool IsDirty => LastSavedUtc.Equals(DateTime.MinValue) || LastUpdatedUtc > LastSavedUtc;

		public int HighCutoff => this[^1].Cutoff;

		#endregion

		#region Public Properties

		public ObjectId Id { get; init; }

		public Guid ColorBandsSerialNumber { get; private set; }

		public bool OnFile { get; private set; }

		public DateTime DateCreated => Id.CreationTime;

		public ObjectId? ParentId
		{
			get => _parentId;
			set
			{
				_parentId = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public ObjectId ProjectId
		{
			get => _projectId;
			set
			{
				if (value != _projectId)
				{
					_projectId = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public string? Name
		{
			get => _name;
			set
			{
				if (value != _name)
				{
					_name = value;
					LastUpdatedUtc = DateTime.UtcNow;
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
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public int SelectedColorBandIndex { get; set; }

		public ColorBand SelectedColorBand
		{
			get
			{
				if (SelectedColorBandIndex < 0 || SelectedColorBandIndex > Count - 1)
				{
					return this[0];
				}
				else
				{
					return this[SelectedColorBandIndex];
				}
			} 
		}

		public DateTime LastSavedUtc
		{
			get => _lastSavedUtc;
			set
			{
				_lastSavedUtc = value;
				LastUpdatedUtc = value;
				OnFile = true;
			}
		}

		public DateTime LastUpdatedUtc { get; private set; }

		#endregion

		#region Public Methods

		public bool UpdatePercentages(PercentageBand[] newPercentages)
		{
			var len = Math.Min(newPercentages.Length, Count);

			var allMatched = true;
			for (var i = 0; i < len; i++)
			{
				if (Items[i].Cutoff != newPercentages[i].Cutoff)
				{
					allMatched = false;
					break;
				}
			}

			if (!allMatched)
			{
				return false;
			}

			for (var i = 0; i < len; i++)
			{
				var cb = Items[i];
				cb.Percentage = newPercentages[i].Percentage;
			}

			return true;
		}

		public IList<ReservedColorBand> GetReservedColorBands()
		{
			return _reservedColorBands.ToList();
		}

		public void MoveItemsToReserveWithCutoffGtrThan(int cutoff)
		{
			var selectedItems = Items.Take(Count - 1).Where(x => x.Cutoff > cutoff).Reverse();

			foreach(var colorBand in selectedItems)
			{
				var reservedColorBand = new ReservedColorBand(colorBand.StartColor, colorBand.BlendStyle, colorBand.EndColor);
				Remove(colorBand);

				_reservedColorBands.Push(reservedColorBand);
			}
		}

		public void InsertCutoff(int index, int cutoff)
		{
			InsertItem(index, new ColorBand(cutoff, ColorBandColor.White, ColorBandBlendStyle.None, ColorBandColor.White));
			PullColorsDown(index); // A band is pulled from the reserves and placed at the end.
		}

		public void DeleteCutoff(int index)
		{
			if (index < 0 || index > Count - 2)
			{
				throw new ArgumentException("Index must be between 0 and Count - 2, inclusive when deleting a cutoff.");
			}

			PullOffsetsDown(index); // Last Band is popped from the list and added to the reserves.
		}

		public void InsertColor(int index, ColorBand colorBand)
		{
			InsertItem(index, colorBand);
			PullOffsetsDown(index); // Last Band is popped from the list and added to the reserves.
		}

		public void DeleteColor(int index)
		{
			if (index < 0 || index > Count - 2)
			{
				throw new ArgumentException("Index must be between 0 and Count - 2, inclusive when deleting a cutoff.");
			}

			PullColorsDown(index); // A band is pulled from the reserves and placed at the end.
		}

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

				int? prevCutoff = null;

				for (var i = 0; i < colorBands.Count - 1; i++)
				{
					var cb = colorBands[i];
					cb.PreviousCutoff = prevCutoff;
					cb.SuccessorStartColor = colorBands[i + 1].StartColor;
					prevCutoff = cb.Cutoff;

					if (cb.BlendStyle == ColorBandBlendStyle.None)
					{
						cb.EndColor = cb.StartColor;
					}
					else if (cb.BlendStyle == ColorBandBlendStyle.Next)
					{
						cb.EndColor = colorBands[i + 1].StartColor;
					}
				}

				var lastCb = colorBands[colorBands.Count - 1];

				Debug.Assert(lastCb.BlendStyle != ColorBandBlendStyle.Next, "The last item in the list of ColorBands being used to construct a ColorBandSet has its BlendStyle set to 'Next.'");

				lastCb.PreviousCutoff = prevCutoff;

				if (lastCb.BlendStyle == ColorBandBlendStyle.None)
				{
					lastCb.EndColor = lastCb.StartColor;
				}

				var totalWidth = result.Sum(x => x.BucketWidth);
				var maxCutoff = lastCb.Cutoff;
				var minCutoff = colorBands[0].StartingCutoff;

				var totalRange = 1 + maxCutoff - minCutoff;
			}

			return result;
		}

		private void PullColorsDown(int index)
		{
			for (var ptr = index; ptr < Count - 3; ptr++)
			{
				var targetCb = Items[ptr];
				var sourceCb = Items[ptr + 1];

				targetCb.StartColor = sourceCb.StartColor;
				targetCb.BlendStyle = sourceCb.BlendStyle;
				targetCb.EndColor = sourceCb.EndColor;
				targetCb.SuccessorStartColor = Items[ptr + 2].StartColor;
			}

			var targetCbE = Items[Count - 2];
			var sourceCbE = GetNextReservedColorBand();

			targetCbE.StartColor = sourceCbE.StartColor;
			targetCbE.BlendStyle = sourceCbE.BlendStyle;
			targetCbE.EndColor = sourceCbE.EndColor;
			targetCbE.SuccessorStartColor = Items[Count - 1].StartColor;
		}

		private void PullOffsetsDown(int index)
		{
			for (var ptr = index; ptr < Count - 2; ptr++)
			{
				var targetCb = Items[ptr];
				var sourceCb = Items[ptr + 1];

				targetCb.Cutoff = sourceCb.Cutoff;
				targetCb.PreviousCutoff = ptr == 0 ? null : Items[ptr - 1].Cutoff;
			}

			var lastCb = Items[Count - 2];
			RemoveItem(Count - 2);
			var newReserved = new ReservedColorBand(lastCb.StartColor, lastCb.BlendStyle, lastCb.EndColor);
			_reservedColorBands.Push(newReserved);
		}

		private ReservedColorBand GetNextReservedColorBand()
		{
			if (_reservedColorBands.Count == 0)
			{
				return new ReservedColorBand();
			}
			else
			{
				return _reservedColorBands.Pop();
			}
		}

		#endregion

		#region Clone Support

		/// <summary>
		/// Identical clone, but with a new "Serial Number" and record id.
		/// </summary>
		/// <returns></returns>
		public ColorBandSet CreateNewCopy()
		{
			var result = new ColorBandSet(ObjectId.GenerateNewId(), Id, ProjectId, Name, Description, CreateBandsCopy(), CreateReservedBandsCopy(), ColorBandsSerialNumber)
			{
				LastSavedUtc = DateTime.MinValue,
				LastUpdatedUtc = LastUpdatedUtc,
				OnFile = false
			};

			return result;
		}

		/// <summary>
		/// Receives a new targetIterations, but keeps the same "Serial Number."
		/// </summary>
		/// <returns></returns>
		public ColorBandSet CreateNewCopy(int targetIterations)
		{
			var bandsCopy = CreateBandsCopy();
			bandsCopy[^1].Cutoff = targetIterations;
			var result = new ColorBandSet(ObjectId.GenerateNewId(), ParentId, ProjectId, Name, Description, bandsCopy, CreateReservedBandsCopy(), ColorBandsSerialNumber)
			{
				LastSavedUtc = DateTime.MinValue,
				LastUpdatedUtc = LastUpdatedUtc,
				OnFile = false
			};

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

			var result = new ColorBandSet(Id, ParentId, ProjectId, Name, Description, CreateBandsCopy(), CreateReservedBandsCopy(), ColorBandsSerialNumber)
			{
				LastSavedUtc = LastSavedUtc,
				LastUpdatedUtc = LastUpdatedUtc,
				OnFile = OnFile
			};

			return result;
		}

		private IList<ColorBand> CreateBandsCopy()
		{
			var result = Items.Select(x => x.Clone()).ToList();
			return result;
		}

		private IEnumerable<ReservedColorBand> CreateReservedBandsCopy()
		{
			var result = _reservedColorBands.Select(x => x.Clone());
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

		//public bool Equals(ColorBandSet? other)
		//{
		//	return other != null
		//		&& ColorBandsSerialNumber == other.ColorBandsSerialNumber
		//		&& HighCutoff == other.HighCutoff;
		//}

		public bool Equals(ColorBandSet? other)
		{
			return other != null
				&& Id.Equals(other.Id);
		}

		public bool Equals(ColorBandSet? x, ColorBandSet? y)
		{
			return x is null ? y is null : x.Equals(y);
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public int GetHashCode([DisallowNull] ColorBandSet obj)
		{
			return GetHashCode(obj);
		}

		//public override int GetHashCode()
		//{
		//	return HashCode.Combine(ColorBandsSerialNumber, HighCutoff);
		//}

		//public int GetHashCode([DisallowNull] ColorBandSet obj)
		//{
		//	return HashCode.Combine(obj.ColorBandsSerialNumber, obj.HighCutoff);
		//}

		public static bool operator ==(ColorBandSet? left, ColorBandSet? right)
		{
			return EqualityComparer<ColorBandSet>.Default.Equals(left, right);
		}

		public static bool operator !=(ColorBandSet? left, ColorBandSet? right)
		{
			return !(left == right);
		}

		#endregion

		#region Property Changed Support

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			base.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		#region Public Methods Not Used

		public void Fix()
		{
			if (Items == null || Count == 0)
			{
				Insert(0, DEFAULT_HIGH_COLOR_BAND.Clone());
			}
			else
			{
				int? prevCutoff = null;

				for (var i = 0; i < Count - 1; i++)
				{
					var cb = Items[i];
					cb.PreviousCutoff = prevCutoff;
					cb.SuccessorStartColor = Items[i + 1].StartColor;
					prevCutoff = cb.Cutoff;

					if (cb.BlendStyle == ColorBandBlendStyle.None)
					{
						cb.EndColor = cb.StartColor;
					}
					else if (cb.BlendStyle == ColorBandBlendStyle.Next)
					{
						cb.EndColor = Items[i + 1].StartColor;
					}

				}

				var lastCb = Items[Count - 1];
				lastCb.PreviousCutoff = prevCutoff;
				if (lastCb.BlendStyle == ColorBandBlendStyle.None)
				{
					lastCb.EndColor = lastCb.StartColor;
				}
			}
		}

		#endregion
	}
}
