using ProtoBuf;

namespace MSS.Types.DataTransferObjects
{
	[ProtoContract(SkipConstructor = true)]
	public class SizeIntDto
	{
		[ProtoMember(1)]
		public int Width { get; init; }

		[ProtoMember(2)]
		public int Height { get; init; }

		public SizeIntDto() : this(0, 0)
		{ }

		public SizeIntDto(int width, int height)
		{
			Width = width;
			Height = height;
		}
	}
}
