using MSS.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace MSS.Common.DataTransferObjects
{
	public class CountsBlock
	{
		public Vector256<int>[] RowCountVectors;
		public Memory<Vector256<int>> Buffer;

		public CountsBlock(SizeInt blockSize)
		{
			var vectorCount = blockSize.NumberOfCells / Vector256<uint>.Count;
			RowCountVectors = new Vector256<int>[vectorCount];

			//nint x = new 

			Buffer = new Memory<Vector256<int>>(RowCountVectors);

		}
	}
}
