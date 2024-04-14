using System;

namespace MSS.Types
{
	public interface IBlendVals 
	{
		int BlendAndPlace(double factor, Span<byte> destination);
	}
}