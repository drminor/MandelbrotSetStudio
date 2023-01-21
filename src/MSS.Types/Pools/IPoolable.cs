using System;

namespace MSS.Types
{
	public interface IPoolable : IDisposable
	{
		void ResetObject();
	}
}
