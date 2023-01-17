using MSS.Common.APValues;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	public ref struct SamplePointValues
	{

		#region Private Properties

		private readonly int[] _hasEscapedFlagsBa;
		private readonly int[] _countsBa;
		private readonly int[] _escapeVelocitiesBa;
		private readonly int[] _doneFlagsBa;
		private readonly int[] _unusedCalcsBa;

		private readonly Memory<int> _hasEscapedFlagsM;
		private readonly Memory<int> _countsM;
		private readonly  Memory<int> _escapeVelocitiesM;

		private readonly Memory<int> _doneFlagsM;
		private readonly Memory<int> _unusedCalcsM;


		private int _targetIterations;

		#endregion

		#region Constructor

		public SamplePointValues(int limbCount, int valueCount) 
		{
			//Crs = new FP31Deck(limbCount, valueCount);
			//Cis = new FP31Deck(limbCount, valueCount);
			//Zrs = new FP31Deck(limbCount, valueCount);
			//Zis = new FP31Deck(limbCount, valueCount);

			ZValuesAreZero = true;

			_targetIterations = 0;
			TargetIterationsVector = Vector256<int>.Zero;

			InPlayList = new int[0];

			_hasEscapedFlagsBa = new int[valueCount];
			_hasEscapedFlagsM = new Memory<int>(_hasEscapedFlagsBa);
			HasEscapedFlagsV = MemoryMarshal.Cast<int, Vector256<int>>(_hasEscapedFlagsM.Span);

			_countsBa = new int[valueCount];
			_countsM = new Memory<int>(_countsBa);
			CountsV = MemoryMarshal.Cast<int, Vector256<int>>(_countsM.Span);

			_escapeVelocitiesBa = new int[valueCount];
			_escapeVelocitiesM = new Memory<int>(_escapeVelocitiesBa);
			EscapeVelocitiesV = MemoryMarshal.Cast<int, Vector256<int>>(_escapeVelocitiesM.Span);

			_doneFlagsBa = new int[valueCount];
			_doneFlagsM = new Memory<int>(_doneFlagsBa);
			DoneFlagsV = MemoryMarshal.Cast<int, Vector256<int>>(_doneFlagsM.Span);

			_unusedCalcsBa = new int[valueCount];
			_unusedCalcsM = new Memory<int>(_unusedCalcsBa);
			UnusedCalcsV = MemoryMarshal.Cast<int, Vector256<int>>(_unusedCalcsM.Span);

		}

		public void LoadBlock(FP31Val[] samplePointsX, int targetIterations)
		{
			Crs = new FP31Deck(samplePointsX);

			//Crs.UpdateFrom(samplePointsX);
			TargetIterations = targetIterations;
		}

		public void LoadRow(FP31Val yPoint, FP31Deck zrs, FP31Deck zis, Span<int> hasEscapedFlags, Span<int> counts, Span<int> escapeVelocities)
		{
			//Crs = crs;
			Cis = new FP31Deck(yPoint, ValueCount);
			//Cis.UpdateFrom(yPoint);

			Zrs = zrs;
			Zis = zis;

			ZValuesAreZero = Zrs.IsZero || Zis.IsZero;

			if (ZValuesAreZero)
			{
				Debug.Assert(zrs.IsZero && zis.IsZero, "One of zRs or zIs is zero, but both zRs and zIs are not zero.");
			}


			hasEscapedFlags.CopyTo(_hasEscapedFlagsBa);
			counts.CopyTo(_countsBa);
			escapeVelocities.CopyTo(_escapeVelocitiesBa);

			// Initially, all vectors are 'In Play.'
			InPlayList = Enumerable.Range(0, VectorCount).ToArray();
			Array.Clear(_doneFlagsBa, 0, _doneFlagsBa.Length);

			//InPlayList = BuildTheInplayList(hasEscapedFlags, counts, targetIterations, out bool[] doneFlags);
			//DoneFlags = doneFlags;

			Array.Clear(_unusedCalcsBa, 0, _unusedCalcsBa.Length);
		}

		//public SamplePointValues(FP31Deck crs, FP31Deck cis, FP31Deck zrs, FP31Deck zis, int targetIterations, Span<int> hasEscapedFlags, Span<int> counts, Span<int> escapeVelocities)
		//{
		//	Crs = crs ?? throw new ArgumentNullException(nameof(crs));
		//	Cis = cis ?? throw new ArgumentNullException(nameof(cis));
		//	Zrs = zrs ?? throw new ArgumentNullException(nameof(zrs));
		//	Zis = zis ?? throw new ArgumentNullException(nameof(zis));

		//	_targetIterations = targetIterations;
		//	TargetIterationsVector = Vector256.Create(targetIterations);

		//	var valueCount = crs.ValueCount;

		//	// Initially, all vectors are 'In Play.'
		//	InPlayList = Enumerable.Range(0, crs.VectorCount).ToArray();

		//	_hasEscapedFlagsBa = new int[valueCount];
		//	_hasEscapedFlagsM = new Memory<int>(_hasEscapedFlagsBa);
		//	hasEscapedFlags.CopyTo(_hasEscapedFlagsBa);
		//	HasEscapedFlagsV = MemoryMarshal.Cast<int, Vector256<int>>(_hasEscapedFlagsM.Span);

		//	_countsBa = new int[valueCount];
		//	_countsM = new Memory<int>(_countsBa);
		//	counts.CopyTo(_countsBa);
		//	CountsV = MemoryMarshal.Cast<int, Vector256<int>>(_countsM.Span);

		//	_escapeVelocitiesBa = new int[valueCount];
		//	_escapeVelocitiesM = new Memory<int>(_escapeVelocitiesBa);
		//	escapeVelocities.CopyTo(_escapeVelocitiesBa);
		//	EscapeVelocitiesV = MemoryMarshal.Cast<int, Vector256<int>>(_escapeVelocitiesM.Span);

		//	//Counts = counts;
		//	//CountsV = MemoryMarshal.Cast<int, Vector256<int>>(counts.Span);

		//	//EscapeVelocities = escapeVelocities;
		//	//EscapeVelocitiesV = MemoryMarshal.Cast<int, Vector256<int>>(escapeVelocities.Span);

		//	//InPlayList = BuildTheInplayList(hasEscapedFlags, counts, targetIterations, out bool[] doneFlags);
		//	//DoneFlags = doneFlags;


		//	_doneFlagsBa = new int[valueCount];
		//	_doneFlagsM = new Memory<int>(_doneFlagsBa);
		//	DoneFlagsV = MemoryMarshal.Cast<int, Vector256<int>>(_doneFlagsM.Span);

		//	_unusedCalcsBa = new int[valueCount];
		//	_unusedCalcsM = new Memory<int>(_unusedCalcsBa);
		//	UnusedCalcsV = MemoryMarshal.Cast<int, Vector256<int>>(_unusedCalcsM.Span);

		//	//ZValuesAreZero = Zrs.IsZero || Zis.IsZero;

		//	//if (ZValuesAreZero)
		//	//{
		//	//	Debug.Assert(zrs.IsZero && zis.IsZero, "One of zRs or zIs is zero, but both zRs and zIs are not zero.");
		//	//}
		//}

		#endregion

		#region Public Properties

		public int ValueCount => Crs.ValueCount;
		public int VectorCount => Crs.VectorCount;

		public int[] InPlayList { get; private set; }

		public int TargetIterations
		{
			get => _targetIterations;
			set
			{
				if (value != _targetIterations)
				{
					_targetIterations = value;
					TargetIterationsVector = Vector256.Create(_targetIterations);

				}
			}
		}

		public Vector256<int> TargetIterationsVector { get; private set; }

		//public FP31Deck Crs { get; private set; }
		//public FP31Deck Cis { get; private set; }
		//public FP31Deck Zrs { get; private set; }
		//public FP31Deck Zis { get; private set; }

		//public bool ZValuesAreZero { get; set; }

		//public Span<Vector256<int>> HasEscapedFlagsV { get; init; }
		//public Span<Vector256<int>> CountsV { get; init; }
		//public Span<Vector256<int>> EscapeVelocitiesV { get; init; }

		//public Span<Vector256<int>> DoneFlagsV { get; init; }

		//public Span<Vector256<int>> UnusedCalcsV { get; init; }


		//public int[] HasEscapedFlags => _hasEscapedFlagsBa;
		//public int[] Counts => _countsBa;
		//public int[] EscapeVelocities => _escapeVelocitiesBa;

		////public int[] DoneFlags => _doneFlagsBa;
		//public int[] UnusedCalcs => _unusedCalcsBa;

		#endregion

		#region Public Methods

		public int[] UpdateTheInPlayList(List<int> vectorsNoLongerInPlay)
		{
			var lst = InPlayList.ToList();

			foreach (var vectorIndex in vectorsNoLongerInPlay)
			{
				lst.Remove(vectorIndex);
			}

			var updatedLst = lst.ToArray();

			InPlayList = updatedLst;

			return updatedLst;
		}

		//public void UpdateTheCounts()
		//{
		//	var lanes = Vector256<uint>.Count;

		//	var vPtr = 0;

		//	for (int i = 0; i < VectorCount; i++)
		//	{
		//		for (int lPtr = 0; lPtr < lanes; lPtr++)
		//		{
		//			Counts[vPtr++] = (ushort) CountsV[i].GetElement(lPtr);
		//		}
		//	}
		//}

		#endregion

		#region Private Methods

		private static int[] BuildTheInplayList(Span<bool> hasEscapedFlags, Span<ushort> counts, int targetIterations, out bool[] doneFlags)
		{
			var lanes = Vector256<uint>.Count;
			var vectorCount = hasEscapedFlags.Length / lanes;

			doneFlags = new bool[hasEscapedFlags.Length];

			for (int i = 0; i < hasEscapedFlags.Length; i++)
			{
				if (hasEscapedFlags[i] | counts[i] >= targetIterations) 
				{
					doneFlags[i] = true;
				}
			}

			var result = Enumerable.Range(0, vectorCount).ToList();

			for (int j = 0; j < vectorCount; j++)
			{
				var arrayPtr = j * lanes;

				var allDone = true;

				for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
				{
					if (!doneFlags[arrayPtr + lanePtr])
					{
						allDone = false;
						break;
					}
				}

				if (allDone)
				{
					result.Remove(j);
				}
			}

			return result.ToArray();
		}

		#endregion
	}
}

