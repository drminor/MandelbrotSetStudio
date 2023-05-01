using MSS.Types;
using MSS.Types.MSet;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace MSS.Common.MSet
{
	public class SubdivisonProvider
	{
		private const int TERMINAL_LIMB_COUNT = 2;
		private const int TERMINAL_SUBDIVISION_POW = TERMINAL_LIMB_COUNT * ApFixedPointFormat.EFFECTIVE_BITS_PER_LIMB;

		private static readonly BigVector TERMINAL_SUBDIV_SIZE = new BigVector(BigInteger.Pow(2, TERMINAL_SUBDIVISION_POW));

		private readonly IMapSectionAdapter _mapSectionAdapter;

		public SubdivisonProvider(IMapSectionAdapter mapSectionAdapter)
		{
			_mapSectionAdapter = mapSectionAdapter;
		}

		public bool TryGetSubdivision(RSize samplePointDelta, BigVector baseMapPosition, [NotNullWhen(true)] out Subdivision? subdivision)
		{
			var result = _mapSectionAdapter.TryGetSubdivision(samplePointDelta, baseMapPosition, out subdivision);
			return result;
		}

		// Find an existing subdivision record with the same SamplePointDelta
		// if not found, create a new record and persist to the repository.
		public Subdivision GetSubdivision(RSize samplePointDelta, BigVector mapBlockOffset, out BigVector localMapBlockOffset)
		{
			var estimatedBaseMapPosition = GetBaseMapPosition(mapBlockOffset, out localMapBlockOffset);

			if (!_mapSectionAdapter.TryGetSubdivision(samplePointDelta, estimatedBaseMapPosition, out var result))
			{
				var subdivision = new Subdivision(samplePointDelta, estimatedBaseMapPosition);
				result = _mapSectionAdapter.InsertSubdivision(subdivision);
			}

			return result;
		}

		public BigVector GetBaseMapPosition(BigVector mapBlockOffset, out BigVector localMapBlockOffset)
		{
			var quotient = mapBlockOffset.DivRem(TERMINAL_SUBDIV_SIZE, out localMapBlockOffset);

			var result = quotient.Scale(TERMINAL_SUBDIV_SIZE);

			return result;
		}

	}
}
