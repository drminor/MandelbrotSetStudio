
namespace ProjectRepo
{
	public class MapSectionReaderWriters
	{
		private readonly DbProvider _dbProvider;

		private MapSectionReaderWriter _mapSectionReaderWriter;
		private JobMapSectionReaderWriter? _jobMapSectionReaderWriter;

		private MapSectionZValuesReaderWriter? _mapSectionZValuesReaderWriter;
		private SubdivisonReaderWriter? _subdivisionReaderWriter;


		public MapSectionReaderWriters(DbProvider dbProvider)
		{
			_dbProvider = dbProvider;

			_mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			_jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			//_mapSectionZValuesReaderWriter = new MapSectionZValuesReaderWriter(_dbProvider);
			_mapSectionZValuesReaderWriter = null;

			//_subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			_subdivisionReaderWriter = null;

		}

		public MapSectionReaderWriter MapSectionReaderWriter => _mapSectionReaderWriter;

		public JobMapSectionReaderWriter JobMapSectionReaderWriter
		{
			get
			{
				if (_jobMapSectionReaderWriter == null)
				{
					_jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);
				}

				return _jobMapSectionReaderWriter;
			}
		}


		public MapSectionZValuesReaderWriter MapSectionZValuesReaderWriter
		{
			get
			{
				if (_mapSectionZValuesReaderWriter == null)
				{
					_mapSectionZValuesReaderWriter = new MapSectionZValuesReaderWriter(_dbProvider);
				}

				return _mapSectionZValuesReaderWriter;
			}
		}

		public SubdivisonReaderWriter SubdivisionReaderWriter
		{
			get
			{
				if (_subdivisionReaderWriter == null)
				{
					_subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
				}

				return _subdivisionReaderWriter;
			}
		}



	}
}
