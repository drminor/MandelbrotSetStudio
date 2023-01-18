using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	public class IterationState
	{

		#region Private Properties

		private readonly int[] _hasEscapedFlagsBa;
		private readonly int[] _countsBa;
		private readonly int[] _escapeVelocitiesBa;
		private readonly int[] _doneFlagsBa;
		private readonly int[] _unusedCalcsBa;

		private readonly Memory<int> _hasEscapedFlagsM;
		private readonly Memory<int> _countsM;
		private readonly Memory<int> _escapeVelocitiesM;

		private readonly Memory<int> _doneFlagsM;
		private readonly Memory<int> _unusedCalcsM;


		private int _targetIterations;

		#endregion

		#region Constructor

		public IterationState(int valueCount, int targetIteration) 
		{
			ValueCount = valueCount;
			VectorCount = valueCount / Vector256<int>.Count;

			_targetIterations = targetIteration;

			TargetIterationsVector = Vector256.Create(_targetIterations);

			InPlayList = new int[0];

			_hasEscapedFlagsBa = new int[valueCount];
			_hasEscapedFlagsM = new Memory<int>(_hasEscapedFlagsBa);

			_countsBa = new int[valueCount];
			_countsM = new Memory<int>(_countsBa);

			_escapeVelocitiesBa = new int[valueCount];
			_escapeVelocitiesM = new Memory<int>(_escapeVelocitiesBa);

			_doneFlagsBa = new int[valueCount];
			_doneFlagsM = new Memory<int>(_doneFlagsBa);

			_unusedCalcsBa = new int[valueCount];
			_unusedCalcsM = new Memory<int>(_unusedCalcsBa);

		}

		public void LoadRow(Span<int> hasEscapedFlags, Span<int> counts, Span<int> escapeVelocities)
		{
			Array.Copy(hasEscapedFlags.ToArray(), _hasEscapedFlagsBa, hasEscapedFlags.Length);
			Array.Copy(counts.ToArray(), _countsBa, counts.Length);
			Array.Copy(escapeVelocities.ToArray(), _escapeVelocitiesBa, escapeVelocities.Length);

			//hasEscapedFlags.CopyTo(_hasEscapedFlagsM);
			//counts.CopyTo(_countsM);
			//escapeVelocities.CopyTo(_escapeVelocitiesM);

			//InPlayList = BuildTheInplayList(hasEscapedFlags, counts, targetIterations, out bool[] doneFlags);
			//DoneFlags = doneFlags;

			// Initially, all vectors are 'In Play.'
			InPlayList = Enumerable.Range(0, VectorCount).ToArray();

			Array.Clear(_doneFlagsBa, 0, _doneFlagsBa.Length);
			Array.Clear(_unusedCalcsBa, 0, _unusedCalcsBa.Length);
		}

		#endregion

		#region Public Properties

		public int ValueCount { get; init; }
		public int VectorCount { get; init; }

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


		public int[] HasEscapedFlags => _hasEscapedFlagsBa;
		public int[] Counts => _countsBa;
		public int[] EscapeVelocities => _escapeVelocitiesBa;

		//public int[] DoneFlags => _doneFlagsBa;
		public int[] UnusedCalcs => _unusedCalcsBa;

		#endregion

		#region Public Methods

		public void GetVectors(out Span<Vector256<int>> hasEscapedFlagsV, out Span<Vector256<int>> countsV, out Span<Vector256<int>> escapeVelocitiesV, out Span<Vector256<int>> doneFlagsV, out Span<Vector256<int>> unusedCalcsV)
		{
			hasEscapedFlagsV = MemoryMarshal.Cast<int, Vector256<int>>(_hasEscapedFlagsM.Span);

			countsV = MemoryMarshal.Cast<int, Vector256<int>>(_countsM.Span);

			escapeVelocitiesV = MemoryMarshal.Cast<int, Vector256<int>>(_escapeVelocitiesM.Span);

			doneFlagsV = MemoryMarshal.Cast<int, Vector256<int>>(_doneFlagsM.Span);

			unusedCalcsV = MemoryMarshal.Cast<int, Vector256<int>>(_unusedCalcsM.Span);
		}

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

