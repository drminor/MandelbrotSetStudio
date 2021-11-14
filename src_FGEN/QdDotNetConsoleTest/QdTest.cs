using qdDotNet;

namespace QdDotNetConsoleTest
{
	class QdTest
	{
		public double[] Test1(out string testString)
		{
			var dd = new Dd(1d, 12132d);
			var result = new double[] { dd.hi, dd.lo };
			testString = dd.GetStringVal();

			return result;
		}
	}
}
