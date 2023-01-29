using System;

namespace MSS.Types
{
	public interface IPoolable : IDisposable
	{
		void CopyTo(object obj);

		void ResetObject();
	}
}
