namespace FGenConsole
{
	internal class SubJobProcessorResultCache
	{
		private readonly SubJobResult[] _emptySubJobResults;
		private readonly SubJobResult[] _emptySubJobResultsWithZValues;
		private int _nextResultPtr;
		private int _nextResultWithZValuesPtr;

		public SubJobProcessorResultCache(int size, int instanceNum, int numberOfItems)
		{
			Size = size;
			InstanceNum = instanceNum;
			NumberOfItems = numberOfItems;

			_emptySubJobResults = new SubJobResult[numberOfItems];
			_emptySubJobResultsWithZValues = new SubJobResult[numberOfItems];
			_nextResultPtr = 0;
			_nextResultWithZValuesPtr = 0;
		}

		public readonly int Size;
		public readonly int InstanceNum;
		public readonly int NumberOfItems;

		public SubJobResult GetEmptySubJobResult(bool readZValues)
		{
			SubJobResult result;
			if (readZValues)
			{
				result = GetEmptySubJobResult(_emptySubJobResultsWithZValues, ref _nextResultWithZValuesPtr, readZValues);
			}
			else
			{
				result = GetEmptySubJobResult(_emptySubJobResults, ref _nextResultPtr, readZValues);
			}

			SubJobResult.ClearSubJobResult(result);
			return result;
		}

		private SubJobResult GetEmptySubJobResult(SubJobResult[] cache, ref int cachePtr, bool readZValues)
		{
			if (cachePtr > cache.Length - 1) cachePtr = 0;

			if (cache[cachePtr] == null)
			{
				string instanceName = $"Sub{InstanceNum}";
				cache[cachePtr] = SubJobResult.GetEmptySubJobResult(Size, instanceName, readZValues);
			}
			return cache[cachePtr++];
		}

	}
}
