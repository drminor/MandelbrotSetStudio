using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Common.MSet
{
	public class SubdivisonProvider
	{
		//private static readonly bool _useDetailedDebug = true;

		private const double BITS_PER_SIGNED_LONG = 63;

		private static readonly long LONG_FACTOR = (long)Math.Pow(2, BITS_PER_SIGNED_LONG);

		private static readonly VectorLong TERMINAL_SUBDIV_SIZE = new VectorLong(LONG_FACTOR);

		private readonly IMapSectionAdapter _mapSectionAdapter;

		public SubdivisonProvider(IMapSectionAdapter mapSectionAdapter)
		{
			_mapSectionAdapter = mapSectionAdapter;
		}

		// Find an existing subdivision record with the same SamplePointDelta.
		// If not found, create a new record and persist to the repository.
		public Subdivision GetSubdivision(RSize samplePointDelta, BigVector mapBlockOffset, out VectorLong localMapBlockOffset)
		{
			var baseMapPosition = GetBaseMapPosition(mapBlockOffset, out localMapBlockOffset);

			if (!_mapSectionAdapter.TryGetSubdivision(samplePointDelta, baseMapPosition, out var result))
			{
				var subdivision = new Subdivision(samplePointDelta, baseMapPosition);
				result = _mapSectionAdapter.InsertSubdivision(subdivision);
			}

			return result;
		}

		/// <summary>
		/// Break the MapBlockOffset into a Base and a local component.
		/// The Base is the MapBlockOffset rounded to the nearest integer multiple of 2^63
		/// and the local component is whatever is left over so that
		/// the local component when added to the Base is equal to the MapBlockOffset
		/// For example, given a MapBlockOffset of 515 and for this example using 2^3 instead of 2^63
		/// then the Base is 512 (64 x 2^3) and the local component is 3 (512 + 3 = 515)
		/// </summary>
		/// <param name="mapBlockOffset"></param>
		/// <param name="localMapBlockOffset"></param>
		/// <returns></returns>
		public static BigVector GetBaseMapPosition(BigVector mapBlockOffset, out VectorLong localMapBlockOffset)
		{
			var quotient = mapBlockOffset.DivRem(TERMINAL_SUBDIV_SIZE, out localMapBlockOffset);
			var result = quotient.Scale(TERMINAL_SUBDIV_SIZE);

			return result;
		}

		public bool TryGetSubdivision(RSize samplePointDelta, BigVector baseMapPosition, [NotNullWhen(true)] out Subdivision? subdivision)
		{
			var result = _mapSectionAdapter.TryGetSubdivision(samplePointDelta, baseMapPosition, out subdivision);
			return result;
		}

	}
}
