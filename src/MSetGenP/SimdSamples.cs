using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Numerics;

namespace MSetGenP
{
	public class SimdSamples
	{

		public unsafe int SumVectorized(ReadOnlySpan<int> source)
		{
			if (Avx2.IsSupported)
			{
				return SumVectorizedAvx2(source);
			}
			else
			{
				return SumVectorizedSse2(source);	
			}
		}

		public unsafe int SumVectorizedAvx2(ReadOnlySpan<int> source)
		{
			int result = 0;

			fixed (int* pSource = source)
			{
				Vector256<int> vresult = Vector256<int>.Zero;

				int i = 0;
				int lastBlockIndex = source.Length - (source.Length % 8);

				while (i < lastBlockIndex)
				{
					vresult = Avx2.Add(vresult, Avx.LoadVector256(pSource + i));
					i += 4;

					vresult = Avx2.HorizontalAdd(vresult, vresult);

					result += vresult.ToScalar();
				}

				while (i < source.Length)
				{
					result += pSource[i];
					i += 1;
				}
			}

			Debug.WriteLine($"The result is {result}.");
			return result;
		}

		public unsafe int SumVectorizedSse2(ReadOnlySpan<int> source)
		{
			int result = 0;

			fixed (int* pSource = source)
			{
				Vector128<int> vresult = Vector128<int>.Zero;

				int i = 0;
				int lastBlockIndex = source.Length - (source.Length % 4);

				while (i < lastBlockIndex)
				{
					vresult = Sse2.Add(vresult, Sse2.LoadVector128(pSource + i));
					i += 4;

					if (Ssse3.IsSupported)
					{
						vresult = Ssse3.HorizontalAdd(vresult, vresult);
						vresult = Ssse3.HorizontalAdd(vresult, vresult);
					}
					else
					{
						vresult = Sse2.Add(vresult, Sse2.Shuffle(vresult, 0x4E));
						vresult = Sse2.Add(vresult, Sse2.Shuffle(vresult, 0xB1));
					}

					result += vresult.ToScalar();
				}

				while (i < source.Length)
				{
					result += pSource[i];
					i += 1;
				}
			}

			Debug.WriteLine($"The result is {result}.");
			return result;
		}



		public void Sample()
		{
			var lanes = Vector<ulong>.Count;

			ulong[] a = { 1, 2, 3, 4, 5, 6, 7, 8 };
			ulong[] b = { 1, 1, 1, 1, 1, 1, 1, 1 };
			ulong[] c = new ulong[a.Length];

			for (int i = 0; i < a.Length; i += lanes)
			{
				var a8 = new Vector<ulong>(a, i);
				var b8 = new Vector<ulong>(b, i);
				(a8 + b8).CopyTo(c, i);
			}

			for (int i = 0; i < a.Length; i++)
			{
				Debug.WriteLine($"item: {i}: {c[i]}.");
			}
		}


	}
}
