#pragma once

#include <immintrin.h>
#include "VecHelper.h"

class fp31VecMath
{

public:
	void Square(__m256i* source, __m256i* result);


private:


	//private const int EFFECTIVE_BITS_PER_LIMB = 31;

	//private const uint LOW31_BITS_SET = 0x7FFFFFFF; // bits 0 - 30 are set.
	//private static readonly Vector256<uint> HIGH33_MASK_VEC = Vector256.Create(LOW31_BITS_SET);

	//private const ulong LOW31_BITS_SET_L = 0x000000007FFFFFFF; // bits 0 - 30 are set.
	//private static readonly Vector256<ulong> HIGH33_MASK_VEC_L = Vector256.Create(LOW31_BITS_SET_L);

	//private const uint SIGN_BIT_MASK = 0x3FFFFFFF;
	//private static readonly Vector256<uint> SIGN_BIT_MASK_VEC = Vector256.Create(SIGN_BIT_MASK);

	//private const uint RESERVED_BIT_MASK = 0x80000000;
	//private static readonly Vector256<uint> RESERVED_BIT_MASK_VEC = Vector256.Create(RESERVED_BIT_MASK);

	//private const int TEST_BIT_30 = 0x40000000; // bit 30 is set.
	//private static readonly Vector256<int> TEST_BIT_30_VEC = Vector256.Create(TEST_BIT_30);

	//private static readonly Vector256<int> ZERO_VEC = Vector256<int>.Zero;
	//private static readonly Vector256<uint> ALL_BITS_SET_VEC = Vector256<uint>.AllBitsSet;

	//private static readonly Vector256<uint> SHUFFLE_EXP_LOW_VEC = Vector256.Create(0u, 0u, 1u, 1u, 2u, 2u, 3u, 3u);
	//private static readonly Vector256<uint> SHUFFLE_EXP_HIGH_VEC = Vector256.Create(4u, 4u, 5u, 5u, 6u, 6u, 7u, 7u);

	//private static readonly Vector256<uint> SHUFFLE_PACK_LOW_VEC = Vector256.Create(0u, 2u, 4u, 6u, 0u, 0u, 0u, 0u);
	//private static readonly Vector256<uint> SHUFFLE_PACK_HIGH_VEC = Vector256.Create(0u, 0u, 0u, 0u, 0u, 2u, 4u, 6u);

	//private Vector256<uint>[] _squareResult0Lo;
	//private Vector256<uint>[] _squareResult0Hi;

	//private Vector256<ulong>[] _squareResult1Lo;
	//private Vector256<ulong>[] _squareResult1Hi;

	//private Vector256<ulong>[] _squareResult2Lo;
	//private Vector256<ulong>[] _squareResult2Hi;

	//private Vector256<uint>[] _negationResult;
	//private Vector256<uint>[] _additionResult;

	//private Vector256<uint> _ones;

	//private Vector256<uint> _carryVectors;
	//private Vector256<ulong> _carryVectorsLong;

	//private Vector256<int> _signBitVecs;

	//byte _shiftAmount;
	//byte _inverseShiftAmount;

	//private int _squareSourceStartIndex;
	//private bool _skipSquareResultLow;

	//private const bool USE_DET_DEBUG = false;

};

