using System;

namespace MSS.Types
{
	public interface IPoolable : IDisposable
	{
		object CopyTo(object obj);


		void ResetObject();
	}
}
