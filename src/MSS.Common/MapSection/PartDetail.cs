namespace MSS.Common.MapSectionRepo
{
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
