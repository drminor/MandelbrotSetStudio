﻿using System.Collections.Generic;

namespace MSS.Common.MapSectionRepo
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
}
