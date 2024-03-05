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
		private string _name;
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
			: this(Guid.NewGuid().ToString(), projectId: ObjectId.Empty, colorBands: null, targetIterations, Guid.NewGuid())
		{ }

		public ColorBandSet(string name, IList<ColorBand>? colorBands, int targetIterations, Guid colorBandsSerialNumber)
			: this(name, projectId: ObjectId.Empty, colorBands, targetIterations, colorBandsSerialNumber)
		{ }

		private ColorBandSet(string name, ObjectId projectId, IEnumerable<ColorBand>? colorBands, int targetIterations, Guid colorBandsSerialNumber)
			: this(ObjectId.GenerateNewId(), parentId: null, projectId, name, description: null, colorBands, targetIterations, null, colorBandsSerialNumber)
		{
			LastSavedUtc = DateTime.MinValue;
			OnFile = false;
		}

		public ColorBandSet(ObjectId id, ObjectId? parentId, ObjectId projectId, string name, string? description, IEnumerable<ColorBand>? colorBands, int targetIterations, IEnumerable<ReservedColorBand>? reservedColorBands, Guid colorBandsSerialNumber) 
			: base(FixBands(targetIterations, colorBands))
		{
			Debug.WriteLineIf(_useDetailedDebug, $"Constructing ColorBandSet with id: {id} and Serial#: {colorBandsSerialNumber}.");

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

		private bool _noneAreNaN => Items.All(x => !double.IsNaN(x.Percentage));

		// True if none are NaN and at least one is non zero.
		public bool HavePercentages => (_noneAreNaN) && Items.Any(x => x.Percentage != 0);

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

		public string Name
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

		public int HighlightedColorBandIndex
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

		public void UpdateItemAndNeighbors(int index, ColorBand item)
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

		public bool DeleteStartingCutoff(ColorBand colorBand, out ReservedColorBand? reservedColorBand)
		{
			reservedColorBand = null;

			var index = IndexOf(colorBand);

			if (index < 0 || index > Count - 2)
			{
				return false;
			}

			if (Count < 2)
			{
				// The collection must have at least two items. 
				return false;
			}

			reservedColorBand = PushColorsUp(index); // The Color Values assigned to the last ColorBand are used to create a ReserveColorBand and it's saved to the Reserves.

			var wasRemoved = Remove(colorBand);

			return wasRemoved;
		}

		public ReservedColorBand InsertColor(int index, ColorBand colorBand)
		{
			var reservedColorBand = PushColorsUp(index); // The Color Values assigned to the last ColorBand are used to create a ReserveColorBand and it's saved to the Reserves.
			var cb = Items[index];

			cb.StartColor = colorBand.StartColor;
			cb.EndColor = colorBand.EndColor;
			cb.BlendStyle = colorBand.BlendStyle;

			if (index < Count - 1)
			{
				cb.SuccessorStartColor = Items[index + 1].StartColor;
			}

			if (index > 0)
			{
				Items[index - 1].SuccessorStartColor = cb.StartColor;
			}

			return reservedColorBand;
		}

		public void DeleteColor(int index, ReservedColorBand reservedColorBand)
		{
			if (index < 0 || index > Count - 2)
			{
				throw new ArgumentException($"DeleteColor. Index must be between 0 and {Count - 1}, inclusive.");
			}

			PullColorsDown(index, reservedColorBand); // The first reserved band is popped from stack and its colors are used. If no reserve band available, white and black are used.

			if (index > 0)
			{
				var predecessorCb = Items[index - 1];
				predecessorCb.SuccessorStartColor = Items[index].StartColor;
			}
		}

		//public bool UpdatePercentagesCheckOffsets(PercentageBand[] newPercentages)
		//{
		//	var len = Math.Min(newPercentages.Length - 1, Count); // The last PercentageBand holds the UpperCatchAll

		//	var allMatched = true;
		//	for (var i = 0; i < len; i++)
		//	{
		//		if (Items[i].Cutoff != newPercentages[i].Cutoff)
		//		{
		//			allMatched = false;
		//			break;
		//		}
		//	}

		//	if (!allMatched)
		//	{
		//		Debug.WriteLine($"WARNING: ColorBandSet No percentages are not receiving an update. The offsets don't match.");
		//		return false;
		//	}

		//	if (len != Count)
		//	{
		//		Debug.WriteLine($"WARNING: ColorBandSet {Count - len} Percentages are not receiving an update.");
		//	}

		//	for (var i = 0; i < len; i++)
		//	{
		//		var cb = Items[i];
		//		cb.Percentage = newPercentages[i].Percentage;
		//	}

		//	return true;
		//}

		public bool UpdatePercentagesNoCheck(PercentageBand[] newPercentages)
		{
			var len = Math.Min(newPercentages.Length, Count);

			if (len != Count)
			{
				Debug.WriteLine($"WARNING: ColorBandSet {Count - len} Percentages are not receiving an update.");
			}

			for (var i = 0; i < len; i++)
			{
				var cb = Items[i];
				cb.Percentage = newPercentages[i].Percentage;
			}

			return true;
		}

		public bool UpdateCutoffs(CutoffBand[] newCutoffs)
		{
			var len = Math.Min(newCutoffs.Length - 1, Count);  // The last CutoffBand holds the UpperCatchAll

			if (Count > len)
			{
				Debug.WriteLine($"WARNING: ColorBandSet UpdateCutoffs not updating all values. The length of newCutoffs {newCutoffs.Length - 1} is < Count {Count}.");
			}

			int? prevCutoff = null;

			for (var i = 0; i < len; i++)
			{
				var cb = Items[i];
				cb.UpdateStartAndEndCutoffs(prevCutoff, newCutoffs[i].Cutoff);

				if (cb.BucketWidth < 1)
				{
					throw new InvalidOperationException($"The bucket width for ColorBand: {i} is < 1 while updating the Cutoffs.");
				}

				prevCutoff = cb.Cutoff;
			}

			return true;
		}

		public void ClearPercentages()
		{
			for (var i = 0; i < Count; i++)
			{
				Items[i].Percentage = double.NaN;
			}
		}

		public IEnumerable<ReservedColorBand> GetReservedColorBands()
		{
			return _reservedColorBands.ToList();
		}
		
		public void AssignNewSerialNumber()
		{
			ColorBandsSerialNumber = Guid.NewGuid();
		}

		public ReservedColorBand PopReservedColorBand()
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

		public void PushReservedColorBand(ReservedColorBand reservedColorBand)
		{
			_reservedColorBands.Push(reservedColorBand);
		}

		#endregion

		#region Collection Methods

		protected override void ClearItems()
		{
			base.ClearItems();

			var singleColorBand = CreateSingleColorBand(TargetIterations);
			Add(singleColorBand);
		}

		protected override void InsertItem(int index, ColorBand item)
		{
			base.InsertItem(index, item);
			//UpdateItemAndNeighbors(index, item);
		}

		protected override void RemoveItem(int index)
		{
			if (Count < 2)
			{
				// The collection must have at least one bands.
				return;
			}

			base.RemoveItem(index);
		}

		protected override void SetItem(int index, ColorBand item)
		{
			base.SetItem(index, item);
			//UpdateItemAndNeighbors(index, item);
		}

		#endregion

		#region Private Methods

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

		private ReservedColorBand PushColorsUp(int index)
		{
			var sourceCbE = Items[^2];
			var targetCbE = Items[^1];

			var newReserved = new ReservedColorBand(targetCbE.StartColor, targetCbE.BlendStyle, targetCbE.EndColor);
			//_reservedColorBands.Push(newReserved);

			targetCbE.StartColor = sourceCbE.StartColor;
			targetCbE.BlendStyle = sourceCbE.BlendStyle;
			targetCbE.EndColor = sourceCbE.EndColor;

			var successorStartColor = targetCbE.StartColor;

			for (var ptr = Count - 2; ptr > index; ptr--)
			{
				var sourceCb = Items[ptr - 1];
				var targetCb = Items[ptr];

				targetCb.SuccessorStartColor = successorStartColor;
				targetCb.StartColor = sourceCb.StartColor;
				targetCb.BlendStyle = sourceCb.BlendStyle;
				targetCb.EndColor = sourceCb.EndColor;

				successorStartColor = targetCb.StartColor;
			}

			return newReserved;
		}

		private void PullColorsDown(int index, ReservedColorBand sourceCbE)
		{
			Debug.Assert(Items.Count > 0);
			Debug.Assert(Items[^1].IsLast == true, "Items[^1].IsLast != true.");

			Items[^1].SuccessorStartColor = sourceCbE.StartColor;


			for (var ptr = index; ptr < Count - 1; ptr++)
			{
				var sourceCb = Items[ptr + 1];
				var targetCb = Items[ptr];

				targetCb.StartColor = sourceCb.StartColor;
				targetCb.BlendStyle = sourceCb.BlendStyle;
				targetCb.EndColor = sourceCb.EndColor;
				targetCb.SuccessorStartColor = sourceCb.SuccessorStartColor;
			}

			var targetCbE = Items[^1];

			targetCbE.StartColor = sourceCbE.StartColor;
			targetCbE.BlendStyle = sourceCbE.BlendStyle;
			targetCbE.EndColor = sourceCbE.EndColor;
		}

		#endregion

		#region Fix Bands

		private static IEnumerable<ColorBand> FixBands(int targetIterations, IEnumerable<ColorBand>? colorBands)
		{
			if (targetIterations < 1)
			{
				throw new ArgumentException("The TargetIterations must be at least 1.");
			}

			IList<ColorBand> result;

			if (colorBands == null || colorBands.Count() == 0)
			{
				var singleColorBand = CreateSingleColorBand(targetIterations);
				result = new List<ColorBand> { singleColorBand };
			}
			else
			{
				result = new List<ColorBand>(colorBands);
			}

			var firstCb = result[0];

			if (firstCb.PreviousCutoff != null)
			{
				Debug.WriteLine($"WARNING: ColorBandSet The first color band's PreviousCutoff is not null, setting it to null.");
				firstCb.PreviousCutoff = null;
			}

			var result2 =  FixBandsPart2(targetIterations, result);
			return result2;
		}

		private static IList<ColorBand> FixBandsPart2(int targetIterations, IList<ColorBand> result)
		{
			int? prevCutoff = null;

			double runningPercentage = 0d;

			for (var i = 0; i < result.Count - 1; i++)
			{
				var cb = result[i];
				cb.IsLast = false;
				cb.PreviousCutoff = prevCutoff;
				cb.SuccessorStartColor = result[i + 1].StartColor;

				if (cb.BucketWidth < 1)
				{
					throw new InvalidOperationException($"The bucket width for ColorBand: {i} is < 1 while creating the ColorBandSet.");
				}

				prevCutoff = cb.Cutoff;

				if (!double.IsNaN(cb.Percentage))
				{
					runningPercentage += cb.Percentage;
				}
				else
				{
					Debug.WriteLine($"WARNING: ColorBandSet The last ColorBand's Cutoff is less than the TargetIterations. Creating a new ColorBand to fill the gap.");

				}
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
					Debug.WriteLine($"WARNING: ColorBandSet Setting the next to last ColorBand's Cutoff to {targetIterations - 1}, it was {prevCutoff}. LEAVING THE PERCENTAGE VALUE AS IS.");
				}

				prevCutoff = cbBeforeLast.Cutoff;
			}

			var lastCb = result[^1];

			lastCb.PreviousCutoff = prevCutoff;

			// Make sure that the last ColorBand's Cutoff == Target Iterations
			if (lastCb.Cutoff < targetIterations)
			{
				Debug.WriteLine($"WARNING: ColorBandSet The last ColorBand's Cutoff is less than the TargetIterations. Creating a new ColorBand to fill the gap.");
				lastCb.IsLast = false;

				// Create a new ColorBand to fill the gap.
				var percentage = Math.Max(0, 100 - runningPercentage);
				var newLastCb = CreateHighColorBand(lastCb, targetIterations, percentage);
				result.Add(newLastCb);

				lastCb.SuccessorStartColor = newLastCb.StartColor;
				runningPercentage = 100;
			}
			else
			{
				lastCb.IsLast = true;
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
						Debug.WriteLine($"WARNING: ColorBandSet Setting last ColorBand's Cutoff to {targetIterations}, it was {valueBeforeUpdate}. LEAVING THE PERCENTAGE VALUE AS IS.");
					}
				}
				else
				{
					//Debug.Assert(lastCb.BucketWidth >= 0, "The bucket width is negative while creating the ColorBandSet.");
					if (lastCb.BucketWidth < 1)
					{
						throw new InvalidOperationException($"The bucket width for the last ColorBand is < 1 while creating the ColorBandSet.");
					}
				}

				lastCb.SuccessorStartColor = null;
			}

			//lastCb.Cutoff = lastCb.Cutoff + 2; // Force the inclusion of the counts above the target iterations as a 'real' color band.

			ReportBucketWidthsAndCutoffs(result, runningPercentage);

			return result;
		}

		private static ColorBand CreateHighColorBand(ColorBand previousColorBand, int targetIterations, double percentage)
		{
			var startColor = previousColorBand.ActualEndColor;
			var endColor = ColorBandColor.White;
			var result = new ColorBand(targetIterations, startColor, ColorBandBlendStyle.Next, endColor, previousCutoff: previousColorBand.Cutoff, successorStartColor: null, percentage);
			result.IsLast = true;
			return result;
		}

		private static ColorBand CreateSingleColorBand(int targetIterations)
		{
			var result = new ColorBand(targetIterations, new ColorBandColor("#FFFFFF"), ColorBandBlendStyle.Next, new ColorBandColor("#000000"), 100);
			result.IsLast = true;

			return result;
		}

		#endregion

		#region Clone Support

		/// <summary>
		/// Creates a copy with a new Id using the existing serial number.
		/// </summary>
		/// <returns></returns>
		public ColorBandSet CreateNewCopy()
		{
			var idx = HighlightedColorBandIndex;

			var result = new ColorBandSet(ObjectId.GenerateNewId(), Id, ProjectId, Name, Description, CreateBandsCopy(), TargetIterations, CreateReservedBandsCopy(), ColorBandsSerialNumber)
			{
				LastSavedUtc = DateTime.MinValue,
				LastUpdatedUtc = LastUpdatedUtc,
				OnFile = false,
				HighlightedColorBandIndex = idx
			};

			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSet. Created a new copy from ColorBandSet (Id: {Id}) with new ID: {result.Id}");
			//Debug.WriteLine($"About to CreateNewCopy: {this}");

			return result;
		}

		/// <summary>
		/// Creates a copy with a new Id, the specified targetIterations and keeps the existing "Serial Number."
		/// </summary>
		/// <returns></returns>
		public ColorBandSet CreateNewCopy(int targetIterations)
		{
			//Debug.WriteLine($"ColorBandSet. About to CreateNewCopy with target iterations: {targetIterations}: {this}");
			var idx = HighlightedColorBandIndex;

			var bandsCopy = CreateBandsCopy();
			bandsCopy[^1].Cutoff = targetIterations;
			var result = new ColorBandSet(ObjectId.GenerateNewId(), ParentId, ProjectId, Name, Description, bandsCopy, targetIterations, CreateReservedBandsCopy(), ColorBandsSerialNumber)
			{
				LastSavedUtc = DateTime.MinValue,
				LastUpdatedUtc = LastUpdatedUtc,
				OnFile = false,
				HighlightedColorBandIndex = idx
			};

			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSet. Created a new copy with TargetIterations: {targetIterations} from ColorBandSet (Id: {Id}) with new ID: {result.Id}");

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
			Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSet. Cloning ColorBandSet with Id: {Id}.");

			var idx = HighlightedColorBandIndex;
			var result = new ColorBandSet(Id, ParentId, ProjectId, Name, Description, CreateBandsCopy(), TargetIterations, CreateReservedBandsCopy(), ColorBandsSerialNumber)
			{
				LastSavedUtc = LastSavedUtc,
				LastUpdatedUtc = LastUpdatedUtc,
				OnFile = OnFile,
				HighlightedColorBandIndex = idx
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

			sb.AppendLine($"WARNING: ColorBandSet Using Depreciated version of ToString. Id: {Id}, Serial: {ColorBandsSerialNumber}, Number of Color Bands: {Count}, HighCutoff: {HighCutoff}");

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

		#region Diagnostics

		[Conditional("DEBUG2")]
		public static void ReportBucketWidthsAndCutoffs_Old(IList<ColorBand> colorBands)
		{
			var totalWidth = colorBands.Sum(x => x.BucketWidth);
			var minCutoff = colorBands[0].PreviousCutoff ?? 0;
			var maxCutoff = colorBands[^1].Cutoff;
			var totalRange = 1 + maxCutoff - minCutoff;

			Debug.WriteLine($"Total Width: {totalWidth}, Total Range: {totalRange}, Min Cutoff: {minCutoff}, Max Cutoff: {maxCutoff}.");

			var bucketWidths = string.Join("; ", colorBands.Select(x => x.BucketWidth.ToString()).ToArray());
			Debug.WriteLine($"Bucket Widths: {bucketWidths}.");

			var cutoffs = string.Join("; ", colorBands.Select(x => x.Cutoff.ToString()).ToArray());
			Debug.WriteLine($"Cutoffs: {cutoffs}.");

			var previousCutoffs = string.Join("; ", colorBands.Select(x => (x.PreviousCutoff ?? 0).ToString()).ToArray());
			Debug.WriteLine($"Previous Cutoffs: {previousCutoffs}.");
		}


		[Conditional("DEBUG")]
		public static void ReportBucketWidthsAndCutoffs(IList<ColorBand> colorBands, double runningPercentage)
		{
			var sb = new StringBuilder();

			var totalWidth = colorBands.Sum(x => x.BucketWidth);
			var minCutoff = colorBands[0].PreviousCutoff ?? 0;
			var maxCutoff = colorBands[^1].Cutoff;
			var totalRange = 1 + maxCutoff - minCutoff;

			sb.AppendLine($"Total Width: {totalWidth}, Total Percentage: {runningPercentage}, Total Range: {totalRange}, Min Cutoff: {minCutoff}, Max Cutoff: {maxCutoff}.");

			sb.AppendLine("Start	End		Width");

			for (var i = 0; i < colorBands.Count; i++)
			{
				var cb = colorBands[i];
				var prevCutoff = cb.PreviousCutoff ?? 0;
				sb.AppendLine($"{prevCutoff,8}\t{cb.Cutoff,8}\t{cb.BucketWidth,8}");
			}

			Debug.Write(sb.ToString());
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
