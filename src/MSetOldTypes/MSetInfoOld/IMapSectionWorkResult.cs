using MSS.Types.MSetOld;

namespace MSS.Common.MapSectionRepo
{
	public interface IMapSectionWorkResult : IPartsBin
	{
		int[] Counts { get; }

		bool[] DoneFlags { get; }
		bool IsHighRes { get; }
		int IterationCount { get; set; }
		//int PartCount { get; }
		//List<PartDetail> PartDetails { get; }
		//uint TotalBytesToWrite { get; }
		DDouble[] ZValues { get; }

		//byte[] GetPart(int partNumber);
		//void LoadPart(int partNumber, byte[] buf);
		//void SetPart(int partNumber, byte[] value);
	}


}