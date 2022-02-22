using ProtoBuf;

namespace MSS.Types.DataTransferObjects
{
	[ProtoContract(SkipConstructor = true)]
	public class SizeDblDto
	{
		[ProtoMember(1)]
		public double Width { get; init; }

		[ProtoMember(2)]
		public double Height { get; init; }

		public SizeDblDto() : this(0, 0)
		{ }

		public SizeDblDto(double width, double height)
		{
			Width = width;
			Height = height;
		}
	}
}
