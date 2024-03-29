﻿using MSS.Common.MapSectionRepo;
using MSS.Types.MSetOld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MapSectionRepo
{
	/// <summary>
	/// Implements QuadPrecision IPartsBin (Each Z has two doubles for X (real) and two doubles for Y (imaginary)) = 32 bytes / sample.
	/// </summary>
	internal class SubJobResult : IPartsBin, IMapSectionWorkResult
	{
		private const uint EMPTY_SUB_JOB_RESULT_MARKER = uint.MaxValue;
		private readonly int _size;

		public SubJobResult(uint[] counts, uint iterationCount, double[] zValues, bool[] doneFlags, string processorInstanceName, int size, bool haveZValues, bool includeZValuesOnRead)
		{
			_size = size;

			Counts = counts ?? throw new ArgumentNullException(nameof(counts));
			IterationCount = iterationCount;
			DoneFlags = doneFlags ?? throw new ArgumentNullException(nameof(doneFlags));
			ZValues = zValues ?? throw new ArgumentNullException(nameof(zValues));
			ProcessorInstanceName = processorInstanceName;

			PartDetails = BuildPartDetails(_size, haveZValues, includeZValuesOnRead, out uint totalBytes);
			TotalBytesToWrite = totalBytes;
		}

		public uint[] Counts { get; set; }

		public uint IterationCount { get; set; }
		public bool[] DoneFlags { get; set; }
		public double[] ZValues { get; set; }

		public string ProcessorInstanceName { get; set; }

		public bool IsFree
		{
			get
			{
				return IterationCount == EMPTY_SUB_JOB_RESULT_MARKER;
			}

			set
			{
				if (value)
					IterationCount = EMPTY_SUB_JOB_RESULT_MARKER;
				else
					IterationCount = 0;
			}
		}

		public int PartCount => PartDetails.Count;

		public List<PartDetail> PartDetails { get; }

		public uint TotalBytesToWrite { get; }

		int[] IMapSectionWorkResult.Counts => throw new NotImplementedException();

		bool IMapSectionWorkResult.IsHighRes => throw new NotImplementedException();

		int IMapSectionWorkResult.IterationCount { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		DDouble[] IMapSectionWorkResult.ZValues => throw new NotImplementedException();

		public byte[] GetPart(int partNumber)
		{
			if (partNumber > PartCount - 1)
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
					IterationCount = BitConverter.ToUInt32(value, 0);
					break;
				case 2:
					ZValues = GetZValues(value, _size);
					break;
				case 3:
					DoneFlags = GetDoneFlags(value, _size);
					break;
			}
		}

		private static uint[] GetCounts(byte[] buf, int size)
		{
			uint[] result = new uint[size];
			for (int i = 0; i < size; i++)
			{
				result[i] = BitConverter.ToUInt32(buf, i * 4);
			}

			return result;
		}

		private static byte[] GetBytesFromCounts(uint[] values)
		{
			byte[] tempBuf = values.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
			return tempBuf;
		}

		private static void LoadBytesFromCounts(uint[] values, byte[] buf)
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

		private static double[] GetZValues(byte[] buf, int size)
		{
			double[] result = new double[size * 8];

			for (int i = 0; i < size; i++)
			{
				result[i] = BitConverter.ToDouble(buf, i * 8);
			}

			return result;
		}

		private static byte[] GetBytesFromZValues(double[] zValues)
		{
			byte[] tempBuf = zValues.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
			return tempBuf;
		}

		private static void LoadBytesFromZValues(double[] values, byte[] buf)
		{
			for (int i = 0; i < values.Length; i++)
			{
				Array.Copy(BitConverter.GetBytes(values[i]), 0, buf, i * 8, 8);
			}
		}

		public static SubJobResult GetEmptySubJobResult(int size, string instanceName, bool includeZValuesOnRead)
		{
			SubJobResult result = new(new uint[size], EMPTY_SUB_JOB_RESULT_MARKER, new double[size * 4], new bool[size], instanceName, size, true, includeZValuesOnRead);
			return result;
		}

		public static void ClearSubJobResult(SubJobResult subJobResult)
		{
			if(!subJobResult.IsFree)
			{
				Debug.WriteLine("Clearing a SubJobResult that is still in use.");
			}

			uint[] counts = subJobResult.Counts;
			for (int i = 0; i < counts.Length; i++)
			{
				counts[i] = 0;
			}

			subJobResult.IterationCount = 0;

			bool[] doneFlags = subJobResult.DoneFlags;
			for (int i = 0; i < doneFlags.Length; i++)
			{
				doneFlags[i] = false;
			}

			double[] zValues = subJobResult.ZValues;
			for (int i = 0; i < zValues.Length; i++)
			{
				zValues[i] = 0;
			}
		}

		private static List<PartDetail> BuildPartDetails(int size, bool haveZValues, bool includeZValuesOnRead, out uint totalBytesToWrite)
		{
			List<PartDetail> partDetails;

			if (haveZValues)
			{
				partDetails = new List<PartDetail>
				{
					new PartDetail(size * 4, true), // Counts
					new PartDetail(4, true), // IterationCount
					new PartDetail(size * 32, includeZValuesOnRead), // ZValues
					new PartDetail(size, includeZValuesOnRead) // DoneFlags
				};

				totalBytesToWrite = 4 + (uint)size * 37;
			}
			else
			{
				partDetails = new List<PartDetail>
				{
					new PartDetail(size * 4, true),
				};

				totalBytesToWrite = (uint)size * 4;
			}

			return partDetails;
		}

	}
}
