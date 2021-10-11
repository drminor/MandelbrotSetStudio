using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CountsRepo
{
    // Record
    class IndexRecord
    {
		public IndexRecord(uint offset, uint length)
		{
			Offset = offset;
			Length = length;
		}

		public uint Offset { get; set; }
        public uint Length { get; set; }
    }

    class Indices
    {
		private readonly List<IndexRecord> _indexes;

        public readonly string IndexFilename;
        public bool IsDirty { get; set; }
        
        public Indices(string indexfilename)
        {
			IndexFilename = indexfilename;
			_indexes = new List<IndexRecord>();
			Load();
            IsDirty = false;
        }

        // Loads index from index file
        private void Load()
        {
            if (!File.Exists(IndexFilename))
            {
				throw new FileNotFoundException($"The file {IndexFilename} does not exist.");
            }

			using var fs = File.OpenRead(IndexFilename);
			using var br = new BinaryReader(fs);
			while (br.BaseStream.Position != br.BaseStream.Length)
			{
				var indexRec = new IndexRecord(br.ReadUInt32(), br.ReadUInt32());
				_indexes.Add(indexRec);
			}
		}

        public void Save()
        {
            if (!IsDirty) return;
			using var fs = File.OpenWrite(IndexFilename);
			using var bw = new BinaryWriter(fs);
			for (var i = 0; i < _indexes.Count; i++)
			{
				var indexRec = GetIndex(i);
				bw.Write(indexRec.Offset);
				bw.Write(indexRec.Length);
			}
		}

        // returns specified IndexRecord
        public IndexRecord GetIndex(int index)
        {
			if(index < 0 || index > _indexes.Count - 1)
			{
				throw new ArgumentException("The index is out of bounds.");
			}

            return _indexes[index];
        }

        public int Count
        {
            get { return _indexes.Count; }
        }

        public void AddIndex(uint offset, UInt32 length)
        {
			var indexRec = new IndexRecord(offset, length);
			_indexes.Add(indexRec);
            IsDirty = true;
        }
    }

    public sealed class TextRecords : IDisposable
    {
		public const string WORKING_DIR = @"c:\dice";
		private static readonly string TEMP_FILE_NAME = Path.Combine(WORKING_DIR, @"tempdata.dat");
		private static readonly string BAK_FILE_NAME = Path.Combine(WORKING_DIR, @"tempdata.bak");

		private readonly Indices _indices;
        private readonly FileStream _fs;

        public TextRecords(string indexFilename, string textFilename)
        {
			_indices = new Indices(indexFilename);
			_fs = new FileStream(textFilename, FileMode.OpenOrCreate);
			TextFilename = textFilename;
		}

		public readonly string TextFilename;
		public string IndexFilename => _indices.IndexFilename;
		public int Count => _indices.Count;

		// Adds a string, optionally saves index
		public int Add(string text, bool saveIndex = false) // return index
        {
            _fs.Seek(0, SeekOrigin.End);
            var offset = (uint) _fs.Position;

            using (var bw = new BinaryWriter(_fs, Encoding.UTF8, true))
            {
                bw.Write(text);
            }

            _indices.AddIndex(offset, (uint)text.Length);

            if (saveIndex)
            {
                _indices.Save();
            }

            return _indices.Count - 1;
        }

        // Change Record, update index
        public void Change(int index, string text)
        {
            var record = _indices.GetIndex(index);
            if (record == null)
            {
                return;
            }

            if (text.Length > record.Length)
            {
                _fs.Seek(0, SeekOrigin.End);            
                record.Offset = (uint)_fs.Position;
            }       
            else
            {
				// just change length
				_fs.Seek(record.Offset, SeekOrigin.Begin);
            }

            record.Length = (uint)text.Length;

            using (var bw = new BinaryWriter(_fs, Encoding.UTF8, true))
            {
                bw.Write(text);
            }

            _indices.IsDirty = true; // Makes sure Indices are rewritten
        }

        public void Update()
        {
            _indices.Save();
        }

        public string GetString(int index)
        {
            var record = _indices.GetIndex(index);

            if (record == null)
            {
				return null;
            }

			using var br = new BinaryReader(_fs, Encoding.UTF8, true);
			_fs.Seek(record.Offset, SeekOrigin.Begin);
			return br.ReadString();
		}

        // creates a temp file then renames the old one
        public TextRecords Compress()
        {
            File.Delete(BAK_FILE_NAME);
            File.Delete(TEMP_FILE_NAME);
   
            using (var newfs = File.OpenWrite(TEMP_FILE_NAME))
            {
				using var br = new BinaryReader(_fs);
				using var bw = new BinaryWriter(newfs);
				for (var i = 0; i < Count; i++)
				{
					var indexRec = _indices.GetIndex(i);
					_fs.Seek(indexRec.Offset, SeekOrigin.Begin);

					var str = br.ReadString();
					indexRec.Offset = (uint)newfs.Position;
					indexRec.Length = (uint)str.Length;
					bw.Write(str);
				}
			}

            _indices.IsDirty = true;

			Dispose();

            File.Move(TextFilename, BAK_FILE_NAME);
            File.Move(TEMP_FILE_NAME, TextFilename);

			return new TextRecords(IndexFilename, TextFilename);
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
