using System.Runtime.InteropServices;

namespace MSetGenP
{

	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct M256IntData
	{
		[FieldOffset(0)]
		public byte Int0;
		
		[FieldOffset(32)]
		public byte Int1;

		[FieldOffset(64)]
		public byte Int2;

		[FieldOffset(96)]
		public byte Int3;

		[FieldOffset(128)]
		public byte Int4;

		[FieldOffset(160)]
		public byte Int5;

		[FieldOffset(192)]
		public byte Int6;

		[FieldOffset(224)]
		public byte Int7;


		[FieldOffset(0)]
		public ulong Long0;

		[FieldOffset(64)]
		public ulong Long1;

		[FieldOffset(128)]
		public ulong Long2;

		[FieldOffset(192)]
		public ulong Long3;

		[FieldOffset(0)] 
		public fixed uint Ints[8];

		[FieldOffset(0)]
		public fixed ulong Longs[4];


	}

}