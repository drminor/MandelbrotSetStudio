using FSTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FileDictionaryLib
{
    // Record
    sealed class IndexEntry<K> where K: IEqualityComparer<K>
	{
		public IndexEntry(uint offset, uint valueLength, string serializedKey)
		{
			Offset = offset;
			ValueLength = valueLength;
			SerializedKey = serializedKey;
			//KeyLength = serializedKey.Length;

			SerializationHelper.Deserialize(serializedKey, out K temp);
			Key = temp;
		}

		public IndexEntry(uint offset, uint valueLength, K key)
		{
			Offset = offset;
			ValueLength = valueLength;
			Key = key;

			SerializedKey = GetKeyAsString(key);
			//KeyLength = SerializedKey.Length;
		}

		public uint Offset { get; set; }
        public uint ValueLength { get; set; }

		//public int KeyLength { get; }
		public string SerializedKey { get; }
		public K Key { get; }

		private static string GetKeyAsString(K key)
		{
			StringBuilder sb = new();
			SerializationHelper.Serialize(key, ref sb);

			return sb.ToString();
		}
	}

    sealed class IndexKeys<K> where K: IEqualityComparer<K>
    {
		private readonly Dictionary<K, IndexEntry<K>> _indexes;

        public readonly string IndexFilePath;
        public bool IsDirty { get; set; }
        
        public IndexKeys(string indexFilePath, bool createIfNotFound = true)
        {
			IndexFilePath = indexFilePath;
			_indexes = new Dictionary<K, IndexEntry<K>>();
			Load(createIfNotFound);
            IsDirty = false;
        }

        // Loads index from index file
        private void Load(bool createIfNotFound)
        {
            if (!File.Exists(IndexFilePath))
            {
				if(!createIfNotFound)
					throw new FileNotFoundException($"The file {IndexFilePath} does not exist.");
            }

			using var fs = File.Open(IndexFilePath, FileMode.OpenOrCreate, FileAccess.Read);
			using var br = new BinaryReader(fs);
			while (br.BaseStream.Position != br.BaseStream.Length)
			{
				uint offset = br.ReadUInt32();
				uint valLength = br.ReadUInt32();
				string serializedKey = br.ReadString();

				var indexRec = new IndexEntry<K>(offset, valLength, serializedKey);
				_indexes.Add(indexRec.Key, indexRec);
			}
		}

		public IReadOnlyCollection<IndexEntry<K>> IndexEntries => _indexes.Values;

        public void Save()
        {
            if (!IsDirty) return;
			using var fs = File.OpenWrite(IndexFilePath);
			using var bw = new BinaryWriter(fs);
			foreach (IndexEntry<K> indexRec in _indexes.Values)
			{
				bw.Write(indexRec.Offset);
				bw.Write(indexRec.ValueLength);
				bw.Write(indexRec.SerializedKey);
			}
		}

		public bool ContainsKey(K key)
		{
			bool result = _indexes.ContainsKey(key);
			return result;
		}

        // returns specified IndexRecord
        public IndexEntry<K> GetIndex(K key)
        {
			if(_indexes.TryGetValue(key, out IndexEntry<K> idxEntry))
			{
				return idxEntry;
			}
			else
			{
				return null;
			}
        }

        public void AddIndex(uint offset, uint length, K key)
        {
			var indexRec = new IndexEntry<K>(offset, length, key);
			_indexes.Add(key, indexRec);
            IsDirty = true;
        }
    }

    public sealed class ValueRecords<K,V> : IDisposable where K: IEqualityComparer<K> where V: IPartsBin
	{
		public const string WORKING_DIR = @"C:\_FractalFiles";
		public const string HI_REZ_WORKING_FOLDER = "HiRez";

		public const string DATA_FILE_EXT = "frd";
		public const string INDEX_FILE_EXT = "frx";

		//private static readonly string TEMP_FILE_NAME = Path.Combine(WORKING_DIR, @"tempdata.frd");
		//private static readonly string BAK_FILE_NAME = Path.Combine(WORKING_DIR, @"tempdata.bak");

		private readonly IndexKeys<K> _indices;
        private readonly FileStream _fs;
		public readonly bool UseHiRezFolder;

        public ValueRecords(string filename, bool useHiRezFolder)
        {
			UseHiRezFolder = useHiRezFolder;
			TextFilename = GetFilePaths(filename, UseHiRezFolder, out string indexFilePath);
			_indices = new IndexKeys<K>(indexFilePath);
			_fs = new FileStream(TextFilename, FileMode.OpenOrCreate);
		}

		public readonly string TextFilename;
		public string IndexFilename => _indices.IndexFilePath;

		public bool ContainsKey(K key)
		{
			return _indices.ContainsKey(key);
		}

		public IEnumerable<V> GetValues(Func<K, V> emptyValueProvider) 
		{
			IReadOnlyCollection<IndexEntry<K>> keys = _indices.IndexEntries;

			using var br = new BinaryReader(_fs, Encoding.UTF8, true);
			foreach (IndexEntry<K> key in keys)
			{
				V newV = emptyValueProvider(key.Key);
				_fs.Seek(key.Offset, SeekOrigin.Begin);
				bool _ = LoadParts(br, _fs, newV);

				yield return newV;
			}
		}

		// TODO: Why are we saving on write, unconditionally -- ValueRecords::Add
		// Adds a value by key, optionally saves index
		public void Add(K key, V value, bool saveOnWrite = false)
        {
			if(_indices.ContainsKey(key))
			{
				throw new ArgumentException($"The key: {key} already has been added.");
			}

            _fs.Seek(0, SeekOrigin.End);
            var offset = (uint) _fs.Position;

			using (var bw = new BinaryWriter(_fs, Encoding.UTF8, true))
            {
				uint valueLength = WriteParts(bw, value);
				_indices.AddIndex(offset, valueLength, key);
			}

			_fs.Flush();
			if (saveOnWrite)
			{
				_indices.Save();
			}
			else
			{
				_indices.Save();
			}
		}

        // Change Record, update index
        public void Change(K key, V value)
        {
            var record = _indices.GetIndex(key);
            if (record == null)
            {
                return;
            }

			if (value.TotalBytesToWrite > record.ValueLength)
            {
				// Write new value at the end of the file.
                _fs.Seek(0, SeekOrigin.End);            
                record.Offset = (uint)_fs.Position;
				record.ValueLength = value.TotalBytesToWrite;
			}
			else
            {
				// Write the new data at the original offset.
				_fs.Seek(record.Offset, SeekOrigin.Begin);
            }

            using (var bw = new BinaryWriter(_fs, Encoding.UTF8, true))
            {
				WriteParts(bw, value);
            }
			_fs.Flush();
			//_indices.Save();
			//_indices.IsDirty = true; // Makes sure Indices are rewritten
        }

        public void Update()
        {
            _indices.Save();
        }

		//     // creates a temp file then renames the old one
		//     public ValueRecords<K, V> Compress()
		//     {
		//         File.Delete(BAK_FILE_NAME);
		//         File.Delete(TEMP_FILE_NAME);

		//         using (var newfs = File.OpenWrite(TEMP_FILE_NAME))
		//         {
		//             using (var br = new BinaryReader(_fs))
		//             {
		//                 using (var bw = new BinaryWriter(newfs))
		//                 {
		//			foreach (IndexEntry<K> indexRec in _indices.IndexEntries)
		//			{
		//                         _fs.Seek(indexRec.Offset, SeekOrigin.Begin);

		//                         var str = br.ReadString();
		//                         indexRec.Offset = (uint)newfs.Position;
		//                         indexRec.ValueLength = (uint)str.Length;
		//                         bw.Write(str);
		//                     }
		//                 }
		//             }
		//         }

		//         _indices.IsDirty = true;

		//Dispose();

		//         File.Move(TextFilename, BAK_FILE_NAME);
		//         File.Move(TEMP_FILE_NAME, TextFilename);

		//return new ValueRecords<K,V>(IndexFilename, TextFilename);
		//     }

		public bool ReadParts(K key, V value)
		{
			var record = _indices.GetIndex(key);

			if (record == null)
			{
				return false;
			}

			using var br = new BinaryReader(_fs, Encoding.UTF8, true);
			_fs.Seek(record.Offset, SeekOrigin.Begin);
			bool success = LoadParts(br, _fs, value);

			return success;
		}

		private static uint WriteParts(BinaryWriter bw, V value)
		{
			uint totalBytes = 0;

			for(int partCntr = 0; partCntr < value.PartCount; partCntr++)
			{
				PartDetail pDetail = value.PartDetails[partCntr];
				int partLen = pDetail.PartLength;
				//byte[] buf = value.GetPart(partCntr);
				value.LoadPart(partCntr, pDetail.Buf);
				bw.Write(pDetail.Buf);
				totalBytes += (uint) partLen;
			}

			return totalBytes;
		}

		private static bool LoadParts(BinaryReader br, FileStream fs, V value)
		{
			for (int partCntr = 0; partCntr < value.PartCount; partCntr++)
			{
				PartDetail pDetail = value.PartDetails[partCntr];

				if (pDetail.IncludeOnRead)
				{
					//byte[] buf = br.ReadBytes(pDetail.PartLength);
					br.Read(pDetail.Buf, 0, pDetail.PartLength);
					value.SetPart(partCntr, pDetail.Buf);
				}
				else
				{
					fs.Seek(pDetail.PartLength, SeekOrigin.Current);
				}
			}

			return true;
		}

		public static string GetFilePaths(string fn, bool useHiRezFolder, out string indexPath)
		{
			string basePath = useHiRezFolder ? Path.Combine(WORKING_DIR, HI_REZ_WORKING_FOLDER) : WORKING_DIR;

			string dataPath = Path.ChangeExtension(Path.Combine(basePath, fn), DATA_FILE_EXT);
			indexPath = Path.ChangeExtension(Path.Combine(basePath, fn), INDEX_FILE_EXT);

			return dataPath;
		}

		public static bool RepoExists(string filename, bool useHiRezFolder = false)
		{
			string dataPath = GetFilePaths(filename, useHiRezFolder, out string _);

			bool result = File.Exists(dataPath);
			return result;
		}

		public static bool DeleteRepo(string filename, bool useHiRezFolder = false)
		{
			Debug.WriteLine($"Deleting the Repo: {filename}.");

			try
			{
				string dataPath = GetFilePaths(filename, useHiRezFolder, out string indexPath);

				string backupName = GetRepoBackupName(filename);
				string buDataPath = GetFilePaths(backupName, useHiRezFolder, out string buIndexPath);

				//File.Delete(dataPath);
				//File.Delete(indexPath);
				File.Move(dataPath, buDataPath);
				File.Move(indexPath, buIndexPath);
				return true;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Received error while deleting the Repo: {filename}. The error is {e.Message}.");
				return false;
			}
		}

		public static string GetRepoBackupName(string filename)
		{
			string timeStamp = DateTime.Now.ToString("yyyyMMddTHHmmss");

			return $"{filename}_{timeStamp}";
		}

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		private void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects).
					_indices.Save();
					try
					{
						_fs.Flush();
						_fs.Dispose();
					}
					catch
					{

					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~TextRecords() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}

		#endregion
	}
}
