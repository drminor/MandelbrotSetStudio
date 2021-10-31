using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace FileDictionaryLib
{
	public class FileDictionary<K,T> where K: ISerializable where T : ISerializable
    {
		private readonly ValueRecords _keyRecords;
		private readonly ValueRecords _valueRecords;

		private readonly Dictionary<K, int> _dict;

		public FileDictionary(string filename)
		{
			_dict = new Dictionary<K, int>();

			string kDataFn = GetKeyFilenames(filename, out string kIdxFn);
			_keyRecords = new ValueRecords(kIdxFn, kDataFn);

			string vDataFn = GetKeyFilenames(filename, out string vIdxFn);
			_valueRecords = new ValueRecords(vIdxFn, vDataFn);

			//TODO: Load our _dict with all of the Keys from the _keyRecords.
		}

		public bool Add(K key, T value)
		{
			string k = Serialize(key);
			string v = Serialize(value);

			int kIdx = _keyRecords.Add(k);
			int vIdx = _valueRecords.Add(v);

			_dict.Add(key, kIdx);

			return true;
		}

		// TODO: Make the Serialize method of FileDictionary return real results. Currently hardcoded to return "key" in all cases.
		private string Serialize<J>(J key)
		{
			XmlSerializer xs = new(typeof(J));

			return "key";
		}

		private string GetKeyFilenames(string filename, out string indexFilename)
		{
			indexFilename = $"{filename}_key_idx.dat";
			return $"{filename}_key_data.dat";
		}

		private string GetValueFilenames(string filename, out string indexFilename)
		{
			indexFilename = $"{filename}_vals_idx.dat";
			return $"{filename}_vals_data.dat";
		}
	}
}
