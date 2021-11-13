using System;
using System.Collections.Generic;

namespace FSTypes
{
	public interface IPartsBin
	{
		int PartCount { get; }
		List<PartDetail> PartDetails { get; }
		uint TotalBytesToWrite { get; }
	
		byte[] GetPart(int partNumber);
		void LoadPart(int partNumber, byte[] buf);
		void SetPart(int partNumber, byte[] value);
	}

	public class PartDetail
	{
		public readonly int PartLength;
		public readonly bool IncludeOnRead;
		public byte[] Buf;

		public PartDetail(int partLength, bool includeOnRead)
		{
			PartLength = partLength;
			IncludeOnRead = includeOnRead;
			Buf = new byte[partLength];
		}
	}
}
