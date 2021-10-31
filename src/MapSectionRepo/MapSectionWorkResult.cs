using FileDictionaryLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapSectionRepo
{
	/// <summary>
	/// Implements IPartsBin for either HighRes (Each Z has two doubles for X (real) and two doubles for Y (imaginary)) = 8 + 8 + 8 + 8 = 32
	/// or Standard (Each Z has a double for X (real) and a double for Y (imaginary)) = 8 + 8 = 16
	/// Currently only used for Standard
	/// </summary>
	public class MapSectionWorkResult : IPartsBin
	{
		public bool IsHighRes { get; private set; }
		public int[] Counts { get; private set; }
		public bool[] DoneFlags { get; private set; }
		public DDouble[] ZValues { get; private set; }
		public int IterationCount { get; set; }

		private readonly int _size;

		public MapSectionWorkResult(int[] counts) : this(counts, 0, null, null, counts.Length, false, false)
		{
		}

		public MapSectionWorkResult(int[] counts, int iterationCount, DDouble[] zValues, bool[] doneFlags) : this(counts, iterationCount, zValues, doneFlags, counts.Length, false, true)
		{
		}

		public MapSectionWorkResult(int size, bool highRes, bool includeZValuesOnRead) : this(null, 0, null, null, size, highRes, includeZValuesOnRead)
		{
		}

		private MapSectionWorkResult(int[] counts, int iterationCount, DDouble[] zValues, bool[] doneFlags, int size, bool highRes, bool includeZValuesOnRead)
		{
			_size = size;

			Counts = counts;
			DoneFlags = doneFlags;
			IterationCount = iterationCount;
			ZValues = zValues;

			PartDetails = BuildPartDetails(_size, highRes, includeZValuesOnRead, out uint totalBytes);
			TotalBytesToWrite = totalBytes;
		}

		private static List<PartDetail> BuildPartDetails(int size, bool hiRez, bool includeZValuesOnRead, out uint totalBytesToWrite)
		{
			int zValuesLength = hiRez ? 32 : 16;
			List<PartDetail> partDetails;

			partDetails = new List<PartDetail>
			{
				new PartDetail(size * 4, true), // Counts
				new PartDetail(4, true), // IterationCount
				new PartDetail(size * zValuesLength, includeZValuesOnRead), // ZValues
				new PartDetail(size, includeZValuesOnRead) // DoneFlags
			};

			totalBytesToWrite = (uint) (4 + size * (4 + zValuesLength + 1));

			return partDetails;
		}

		public int PartCount => PartDetails.Count;

		public List<PartDetail> PartDetails { get; }

		public uint TotalBytesToWrite { get; }

		public byte[] GetPart(int partNumber)
		{
			if(partNumber > PartCount - 1)
			{
				throw new ArgumentException($"This Parts Bin only has {PartCount} parts. Cannot get Part for PartNumber: {partNumber}.");
			}
			return partNumber switch
			{
				0 => GetBytesFromCounts(Counts),
				1 => BitConverter.GetBytes(IterationCount),
				2 => GetBytesFromZValues(ZValues),
				3 => GetBytesFromDoneFlags(DoneFlags),
				_ => throw new ArgumentException("The partnumber is out of bounds."),
			};
		}

		public void LoadPart(int partNumber, byte[] buf)
		{
			if (partNumber > PartCount - 1)
			{
				throw new ArgumentException($"This Parts Bin only has {PartCount} parts. Cannot get Part for PartNumber: {partNumber}.");
			}
			switch (partNumber)
			{
				case 0:
					LoadBytesFromCounts(Counts, buf);
					break;
				case 1:
					Array.Copy(BitConverter.GetBytes(IterationCount), buf, 4);
					break;
				case 2:
					LoadBytesFromZValues(ZValues, buf);
					break;
				case 3:
					LoadBytesFromDoneFlags(DoneFlags, buf);
					break;
				default:
					throw new ArgumentException("The partnumber is out of bounds.");
			}
		}

		public void SetPart(int partNumber, byte[] value)
		{
			if (partNumber > PartCount - 1)
			{
				throw new ArgumentException($"This Parts Bin only has {PartCount} parts. Cannot get Part for PartNumber: {partNumber}.");
			}
			switch (partNumber)
			{
				case 0:
					Counts = GetCounts(value, _size);
					break;
				case 1:
					IterationCount = BitConverter.ToInt32(value, 0);
					break;
				case 2:
					ZValues = GetZValues(value, _size);
					break;
				case 3:
					DoneFlags = GetDoneFlags(value, _size);
					break;
			}
		}

		private static int[] GetCounts(byte[] buf, int size)
		{
			int[] result = new int[size];
			for (int i = 0; i < size; i++)
			{
				result[i] = BitConverter.ToInt32(buf, i * 4);
			}

			return result;
		}

		private static byte[] GetBytesFromCounts(int[] values)
		{
			byte[] tempBuf = values.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
			return tempBuf;
		}

		private static void LoadBytesFromCounts(int[] values, byte[] buf)
		{
			for (int i = 0; i < values.Length; i++)
			{
				Array.Copy(BitConverter.GetBytes(values[i]), 0, buf, i * 4, 4);
			}
		}

		private static bool[] GetDoneFlags(byte[] buf, int size)
		{
			bool[] result = new bool[size];
			for (int i = 0; i < size; i++)
			{
				result[i] = BitConverter.ToBoolean(buf, i);
			}

			return result;
		}

		private static byte[] GetBytesFromDoneFlags(bool[] values)
		{
			byte[] tempBuf = values.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
			return tempBuf;
		}

		private static void LoadBytesFromDoneFlags(bool[] values, byte[] buf)
		{
			for (int i = 0; i < values.Length; i++)
			{
				Array.Copy(BitConverter.GetBytes(values[i]), 0, buf, i, 1);
			}
		}

		private static DDouble[] GetZValues(byte[] buf, int size)
		{
			DDouble[] result = new DDouble[size];

			for (int i = 0; i < size; i++)
			{
				int ptr = i * 16;
				double x = BitConverter.ToDouble(buf, ptr);
				double y = BitConverter.ToDouble(buf, ptr + 8);

				result[i] = new DDouble(x, y);
			}

			return result;
		}

		private static byte[] GetBytesFromZValues(DDouble[] zValues)
		{
			byte[] tempBuf = zValues.SelectMany(value => GetBytesFromDPoint(value)).ToArray();
			return tempBuf;
		}

		private static byte[] GetBytesFromDPoint(DDouble value)
		{
			byte[] dBufHi = BitConverter.GetBytes(value.Hi);
			byte[] dBufLo = BitConverter.GetBytes(value.Lo);

			byte[] result = new byte[16];

			Array.Copy(dBufHi, result, 8);
			Array.Copy(dBufLo, 0, result, 8, 8);

			return result;
		}

		private static void LoadBytesFromZValues(DDouble[] values, byte[] buf)
		{
			for (int i = 0; i < values.Length; i++)
			{
				Array.Copy(BitConverter.GetBytes(values[i].Hi), 0, buf, i * 16, 8);
				Array.Copy(BitConverter.GetBytes(values[i].Lo), 0, buf, 8 + (i * 16), 8);
			}
		}

	}
}
