using System;
using System.Collections.Generic;
using System.Linq;

namespace MqMessages
{
	[Serializable]
	public class FHistorgram
	{
		public int JobId { get; set; }
		public int Count { get; set; }
		public string StrValues { get; set; }
		public string StrOccurances { get; set; }

		public FHistorgram() : this(-1, (string)null, (string)null, 0) { }

		public FHistorgram(int jobId, string values, string occurances, int count)
		{
			JobId = jobId;
			StrValues = values;
			StrOccurances = occurances;
			Count = count;
		}

		public FHistorgram(int jobId, int[] values, int[] occurances)
		{
			JobId = jobId;
			Count = values.Length;
			StrValues = GetStringFromInts(values);
			StrOccurances = GetStringFromInts(occurances);
		}

		public FHistorgram(int jobId, IDictionary<int, int> dictionary)
		{
			JobId = jobId;
			Count = dictionary.Count;
			StrValues = GetStringFromInts(dictionary.Keys);
			StrOccurances = GetStringFromInts(dictionary.Values);
		}

		public int[] GetValues()
		{
			return GetIntsFromString(StrValues);
		}

		//public void SetValues(int[] values)
		//{
		//	StrValues = GetStringFromInts(values);
		//}

		public int[] GetOccurances()
		{
			return GetIntsFromString(StrOccurances);
		}

		//public void SetOccurances(int[] occurances)
		//{
		//	StrOccurances = GetStringFromInts(occurances);
		//}

		private string GetStringFromInts(ICollection<int> vals)
		{
			byte[] tempBuf = vals.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
			string result = Convert.ToBase64String(tempBuf);
			return result;
		}

		//private string GetStringFromInts(int[] vals)
		//{
		//	byte[] tempBuf = vals.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
		//	string result = Convert.ToBase64String(tempBuf);
		//	return result;
		//}

		private int[] GetIntsFromString(string str)
		{
			int len = Count;

			byte[] bytes = Convert.FromBase64String(str);
			if (bytes.Length / 4 != len)
			{
				throw new InvalidOperationException("Our Counts string has the wrong length.");
			}

			int[] result = new int[len];
			for (int i = 0; i < len; i++)
			{
				result[i] = BitConverter.ToInt32(bytes, i * 4);
			}

			return result;
		}
	}
}
