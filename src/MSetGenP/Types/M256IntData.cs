using System.Runtime.InteropServices;

namespace MSetGenP
{

	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct M256IntData
	{
		[FieldOffset(0)]
		public uint Int0;
		
		[FieldOffset(32)]
		public uint Int1;

		[FieldOffset(64)]
		public uint Int2;

		[FieldOffset(96)]
		public uint Int3;

		[FieldOffset(128)]
		public uint Int4;

		[FieldOffset(160)]
		public uint Int5;

		[FieldOffset(192)]
		public uint Int6;

		[FieldOffset(224)]
		public uint Int7;


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