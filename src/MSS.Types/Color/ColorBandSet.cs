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
		#region Private Fields

		private ObjectId? _parentId;
		private ObjectId _projectId;
		private string? _name;
		private string? _description;
		private readonly Stack<ReservedColorBand> _reservedColorBands;

		private DateTime _lastSavedUtc;

		private int _hilightedColorBandIndex;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public ColorBandSet()
			: this(targetIterations: 1000)
		{ }

		public ColorBandSet(int targetIterations)
			: this(projectId: ObjectId.Empty, colorBands: null, targetIterations, Guid.NewGuid())
		{ }

		public ColorBandSet(IList<ColorBand>? colorBands, int targetIterations, Guid colorBandsSerialNumber)
			: this(projectId: ObjectId.Empty, colorBands, targetIterations, colorBandsSerialNumber)
		{ }

		private ColorBandSet(ObjectId projectId, IList<ColorBand>? colorBands, int targetIterations, Guid colorBandsSerialNumber)
			: this(ObjectId.GenerateNewId(), parentId: null, projectId, name: null, description: null, colorBands, targetIterations, null, colorBandsSerialNumber)
		{
			LastSavedUtc = DateTime.MinValue;
			OnFile = false;
		}

		public ColorBandSet(ObjectId id, ObjectId? parentId, ObjectId projectId, string? name, string? description, IList<ColorBand>? colorBands, int targetIterations, IEnumerable<ReservedColorBand>? reservedColorBands, Guid colorBandsSerialNumber) 
			: base(FixBands(targetIterations, colorBands))
		{
			Debug.WriteLineIf(_useDetailedDebug, $"Constructing ColorBandSet with id: {id}.");

			Id = id;
			_parentId = parentId;
			_projectId = projectId;
			_name = name;
			_description = description;

			_reservedColorBands = reservedColorBands == null ? new Stack<ReservedColorBand>() : new Stack<ReservedColorBand>(reservedColorBands);
			ColorBandsSerialNumber = colorBandsSerialNumber;

			_hilightedColorBandIndex = 0;

			_lastSavedUtc = DateTime.UtcNow;
			LastUpdatedUtc = DateTime.MinValue;

			TargetIterations = targetIterations;
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

		public int HilightedColorBandIndex
		{
			get => _hilightedColorBandIndex;
			set
			{
				if (value != _hilightedColorBandIndex)
				{
					if (value > Count - 1)
					{
						_hilightedColorBandIndex = Count - 1;
					}
					else if (value < 0)
					{
						_hilightedColorBandIndex = 0;
					}
					else
					{
						_hilightedColorBandIndex = value;
					}

					OnPropertyChanged();
				}
			}
		}

		//public ColorBand? CurrentColorBand
		//{
		//	get
		//	{
		//		if (CurrentColorBandIndex < 0 || CurrentColorBandIndex > Count - 1)
		//		{
		//			return null;
		//		}
		//		else
		//		{
		//			return this[CurrentColorBandIndex];
		//		}
		//	}
		//	set
		//	{
		//		var previousValue = CurrentColorBandIndex;
		//		if (value == null)
		//		{
		//			CurrentColorBandIndex = -1;
		//		}
		//		else
		//		{
		//			var index = IndexOf(value);

		//			if (index == -1)
		//			{
		//				var colorBandWithMatchingCutoff = this.FirstOrDefault(x => x.Cutoff == value.Cutoff);
		//				if (colorBandWithMatchingCutoff != null)
		//				{
		//					CurrentColorBandIndex = IndexOf(colorBandWithMatchingCutoff);
		//				}
		//				else
		//				{
		//					CurrentColorBandIndex = 0;
		//				}
		//			}
		//			else
		//			{
		//				CurrentColorBandIndex = IndexOf(value);
		//			}
		//		}

		//		if (CurrentColorBandIndex != previousValue)
		//		{
		//			OnPropertyChanged();
		//		}
		//	}
		//}

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

		public int TargetIterations { get; set; }

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
				throw new ArgumentException($"DeleteCutoff. Index must be between 0 and {Count - 1}, inclusive.");
			}

			PullCutoffsDown(index); // Last Band is popped from the list and added to the reserves.
		}

		public void InsertColor(int index, ColorBand colorBand)
		{
			InsertItem(index, colorBand);
			PullCutoffsDown(index); // Last Band is popped from the list and added to the reserves.
		}

		public void DeleteColor(int index)
		{
			if (index < 0 || index > Count - 2)
			{
				throw new ArgumentException($"DeleteColor. Index must be between 0 and {Count - 1}, inclusive.");
			}

			PullColorsDown(index); // A band is pulled from the reserves and placed at the end.
		}

		#endregion

		#region Collection Methods

		protected override void ClearItems()
		{
			base.ClearItems();

			var firstColorBand = CreateFirstColorBand(TargetIterations);
			var highColorBand = CreateHighColorBand(firstColorBand, TargetIterations);

			Add(firstColorBand);
			Add(highColorBand);
		}

		protected override void InsertItem(int index, ColorBand item)
		{
			base.InsertItem(index, item);
			UpdateItemAndNeighbors(index, item);
		}

		protected override void RemoveItem(int index)
		{
			if (Count <= 2)
			{
				// The collection must have at least two items.
				return;
			}

			base.RemoveItem(index);

			if (index > Count - 1)
			{
				index = Count - 1;
			}

			UpdateItemAndNeighbors(index, Items[index]);
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

		private static IList<ColorBand> FixBands(int targetIterations, IList<ColorBand>? colorBands)
		{
			if (targetIterations < 1)
			{
				throw new ArgumentException("The TargetIterations must be at least 1.");
			}

			IList<ColorBand> result;

			if (colorBands == null || colorBands.Count == 0)
			{
				var firstColorBand = CreateFirstColorBand(targetIterations);
				result = new List<ColorBand> { firstColorBand, CreateHighColorBand(firstColorBand, targetIterations) };
			}
			else if (colorBands.Count == 1)
			{
				var firstColorBand = colorBands[0].Clone();
				result = new List<ColorBand> { firstColorBand, CreateHighColorBand(firstColorBand, targetIterations) };
			}
			else
			{
				result = new List<ColorBand>(colorBands);
			}

			int? prevCutoff = null;
			int startingCutoff = 0;

			for (var i = 0; i < result.Count - 1; i++)
			{
				var cb = result[i];
				cb.PreviousCutoff = prevCutoff;
				cb.SuccessorStartColor = result[i + 1].StartColor;

				var bucketWidth = cb.Cutoff - startingCutoff;
				if (bucketWidth < 0)
				{
					throw new InvalidOperationException($"The bucket width for ColorBand: {i} is negative while creating the ColorBandSet.");
				}

				prevCutoff = cb.Cutoff;
				startingCutoff = cb.Cutoff + 1;

				FixBlendStyle(cb, cb.SuccessorStartColor.Value);
			}

			// Make sure that the next to last ColorBand's CutOff is < Target Iterations.
			if (prevCutoff >= targetIterations)
			{
				var cbBeforeLast = result[^2];

				cbBeforeLast.Cutoff = targetIterations - 1;
				if (cbBeforeLast.BucketWidth < 0)
				{
					throw new InvalidOperationException("Cannot fix the ColorBandSet. The last starting cutoff is too large.");
				}
				else
				{
					Debug.WriteLine($"WARNING: Setting the next to last ColorBand's Cutoff to {targetIterations - 1}, it was {prevCutoff}. ");
				}
				
				prevCutoff = cbBeforeLast.Cutoff;
			}

			var lastCb = result[^1];

			lastCb.PreviousCutoff = prevCutoff;

			// Make sure that the last ColorBand's Cutoff == Target Iterations
			if (lastCb.Cutoff < targetIterations)
			{
				Debug.WriteLine($"WARNING: The last ColorBand's Cutoff is less than the TargetIterations. Creating a new ColorBand to fill the gap.");

				// Create a new ColorBand to fill the gap.
				var newLastCb = CreateHighColorBand(lastCb, targetIterations);
				result.Add(newLastCb);

				lastCb.SuccessorStartColor = newLastCb.StartColor;
				FixBlendStyle(lastCb, lastCb.SuccessorStartColor.Value);
			}
			else
			{
				if (lastCb.Cutoff > targetIterations)
				{
					// Use the targetIterations to set the Cutoff.
					var valueBeforeUpdate = lastCb.Cutoff;
					lastCb.Cutoff = targetIterations;

					if (lastCb.BucketWidth < 0)
					{
						throw new InvalidOperationException("Cannot fix the ColorBandSet. The last ColorBand's Cutoff > TargetIterations and the last ColorBand's StartingCutoff is too large.");
					}
					else
					{
						Debug.WriteLine($"WARNING: Setting last ColorBand's Cutoff to {targetIterations}, it was {valueBeforeUpdate}. ");
					}
				}
				else
				{
					//Debug.Assert(lastCb.BucketWidth >= 0, "The bucket width is negative while creating the ColorBandSet.");
					if (lastCb.BucketWidth < 0)
					{
						throw new InvalidOperationException($"The bucket width for the last ColorBand is negative while creating the ColorBandSet.");
					}
				}

				// Make sure that the BlendStyle is not equal to Next.
				if (lastCb.BlendStyle == ColorBandBlendStyle.Next)
				{
					Debug.WriteLine($"WARNING: Setting the last ColorBand's BlendStyle to 'End', it was 'Next'.");
					lastCb.BlendStyle = ColorBandBlendStyle.End;
				}
				else
				{
					if (lastCb.BlendStyle == ColorBandBlendStyle.None)
					{
						lastCb.EndColor = lastCb.StartColor;
					}
				}

				lastCb.SuccessorStartColor = null;
			}

			//lastCb.Cutoff = lastCb.Cutoff + 2; // Force the inclusion of the counts above the target iterations as a 'real' color band.

			ReportBucketWidthsAndCutoffs(result);

			return result;
		}

		private static void FixBlendStyle(ColorBand cb, ColorBandColor sucessorStartColor)
		{
			if (cb.BlendStyle == ColorBandBlendStyle.None)
			{
				cb.EndColor = cb.StartColor;
			}
			else
			{
				if (cb.BlendStyle == ColorBandBlendStyle.Next)
				{
					cb.EndColor = sucessorStartColor;
				}
			}
		}

		private static ColorBand CreateHighColorBand(ColorBand previousColorBand, int targetIterations)
		{
			var startColor = previousColorBand.ActualEndColor;
			var	result = new ColorBand(targetIterations, startColor, ColorBandBlendStyle.End, startColor);

			return result;
		}

		private static ColorBand CreateFirstColorBand(int targetIterations)
		{
			var result = new ColorBand(targetIterations - 1, new ColorBandColor("#FFFFFF"), ColorBandBlendStyle.Next, new ColorBandColor("#000000"));
			return result;
		}

		[Conditional("DEBUG2")]
		public static void ReportBucketWidthsAndCutoffs(IList<ColorBand> colorBands)
		{
			var totalWidth = colorBands.Sum(x => x.BucketWidth);
			var minCutoff = colorBands[0].StartingCutoff;
			var maxCutoff = colorBands[^1].Cutoff;
			var totalRange = 1 + maxCutoff - minCutoff;

			Debug.WriteLine($"Total Width: {totalWidth}, Total Range: {totalRange}, Min Cutoff: {minCutoff}, Max Cutoff: {maxCutoff}.");

			var bucketWidths = string.Join("; ", colorBands.Select(x => x.BucketWidth.ToString()).ToArray());
			Debug.WriteLine($"Bucket Widths: {bucketWidths}.");

			var cutoffs = string.Join("; ", colorBands.Select(x => x.Cutoff.ToString()).ToArray());
			Debug.WriteLine($"Cutoffs: {cutoffs}.");

			var startingCutoffs = string.Join("; ", colorBands.Select(x => x.StartingCutoff.ToString()).ToArray());
			Debug.WriteLine($"Starting Cutoffs: {startingCutoffs}.");
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

		private void PullCutoffsDown(int index)
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
		/// Creates a copy with a new Id using the existing serial number.
		/// </summary>
		/// <returns></returns>
		public ColorBandSet CreateNewCopy()
		{
			//Debug.WriteLine($"About to CreateNewCopy: {this}");

			var idx = HilightedColorBandIndex;

			var result = new ColorBandSet(ObjectId.GenerateNewId(), Id, ProjectId, Name, Description, CreateBandsCopy(), TargetIterations, CreateReservedBandsCopy(), ColorBandsSerialNumber)
			{
				LastSavedUtc = DateTime.MinValue,
				LastUpdatedUtc = LastUpdatedUtc,
				OnFile = false,
				HilightedColorBandIndex = idx
			};

			return result;
		}

		/// <summary>
		/// Creates a copy with a new Id, the specified targetIterations and keeps the existing "Serial Number."
		/// </summary>
		/// <returns></returns>
		public ColorBandSet CreateNewCopy(int targetIterations)
		{
			//Debug.WriteLine($"About to CreateNewCopy with update iterations: {targetIterations}: {this}");

			var idx = HilightedColorBandIndex;

			var bandsCopy = CreateBandsCopy();
			bandsCopy[^1].Cutoff = targetIterations;
			var result = new ColorBandSet(ObjectId.GenerateNewId(), ParentId, ProjectId, Name, Description, bandsCopy, targetIterations, CreateReservedBandsCopy(), ColorBandsSerialNumber)
			{
				LastSavedUtc = DateTime.MinValue,
				LastUpdatedUtc = LastUpdatedUtc,
				OnFile = false,
				HilightedColorBandIndex = idx
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
			Debug.WriteLineIf(_useDetailedDebug, $"Cloning ColorBandSet with Id: {Id}.");

			var idx = HilightedColorBandIndex;
			var result = new ColorBandSet(Id, ParentId, ProjectId, Name, Description, CreateBandsCopy(), TargetIterations, CreateReservedBandsCopy(), ColorBandsSerialNumber)
			{
				LastSavedUtc = LastSavedUtc,
				LastUpdatedUtc = LastUpdatedUtc,
				OnFile = OnFile,
				HilightedColorBandIndex = idx
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
			var sb = new StringBuilder();

			sb.AppendLine($"Id: {Id}, Serial: {ColorBandsSerialNumber}, Number of Color Bands: {Count}, HighCutoff: {HighCutoff}");

			for(var i = 0; i < Count; i++)
			{
				_ = sb.AppendLine($"{i,2} {this[i]}");

			}

			return sb.ToString();
		}

		public string ToString(int style)
		{
			if (style == 1)
			{
				var sb = new StringBuilder();

				sb.AppendLine($"Id: {Id}, Serial: {ColorBandsSerialNumber}, Number of Color Bands: {Count}, HighCutoff: {HighCutoff}");

				_ = sb.AppendLine($"Prev\t\tCutoff\t\tWidth");
				for (var i = 0; i < Count; i++)
				{
					var cb = this[i];

					_ = sb.AppendLine($"{cb.PreviousCutoff ?? 0}\t\t{cb.Cutoff}\t\t{cb.BucketWidth}");

				}

				return sb.ToString();

			}
			else
			{
				throw new NotImplementedException("ColorBandSet.ToString(int style) only supports style = 1.");
			}
		}

		//public static string GetString(ICollection<ColorBand> colorBands)
		//{
		//	var sb = new StringBuilder();

		//	foreach (var cb in colorBands)
		//	{
		//		_ = sb.AppendLine(cb.ToString());
		//	}

		//	return sb.ToString();
		//}

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

	}
}
