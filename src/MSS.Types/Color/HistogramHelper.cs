using System.Text;

namespace MSS.Types
{
	public static class HistogramHelper
	{

		public static string HistogramToCSharp(IHistogram histogram)
		{
			var sb = new StringBuilder();

			sb.AppendLine("\t\t\tIDictionary<int, int> a = new Dictionary<int, int>()");
			sb.AppendLine("\t\t\t{");

			var kvps = histogram.GetKeyValuePairs();

			foreach (var kvpair in kvps)
			{
				sb.AppendLine($"\t\t\t\t{{{kvpair.Key}, {kvpair.Value}}},");
			}

			sb.AppendLine("\t\t\t};");

			sb.AppendLine();
			sb.AppendLine($"\t\t\tvar UpperCatchAllValue = {histogram.UpperCatchAllValue}");
			
			return sb.ToString();
		}

		/*

			IDictionary<int, int> a = new Dictionary<int, int>()
			{
				{ 0, 0 },
				{ 1, 1 }
			};



		*/

	}
}
