using MongoDB.Bson;
using MSS.Types;
using ProjectRepo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSetRepo
{
	public class SharedColorBandSetAdapter
	{
		private readonly DbProvider _dbProvider;
		private readonly MSetRecordMapper _mSetRecordMapper;

		#region Constructor

		public SharedColorBandSetAdapter(DbProvider dbProvider, MSetRecordMapper mSetRecordMapper)
		{
			_dbProvider = dbProvider;
			_mSetRecordMapper = mSetRecordMapper;
		}

		#endregion

		#region Collections

		public void CreateCollections()
		{
			var colorsReaderWriter = new SharedColorBandSetReaderWriter(_dbProvider);
			colorsReaderWriter.CreateCollection();
		}

		public void DropCollections()
		{
			var colorsReaderWriter = new SharedColorBandSetReaderWriter(_dbProvider);
			colorsReaderWriter.DropCollection();
		}

		#endregion

		#region SharedColorBandSet 

		public ColorBandSet? GetColorBandSet(string id)
		{
			Debug.WriteLine($"Retrieving ColorBandSet with Id: {id}.");

			var sharedColorsReaderWriter = new SharedColorBandSetReaderWriter(_dbProvider);
			var colorBandSetRecord = sharedColorsReaderWriter.Get(new ObjectId(id));

			var result = colorBandSetRecord == null ? null : _mSetRecordMapper.MapFrom(colorBandSetRecord);
			return result;
		}

		public bool TryGetColorBandSet(ObjectId colorBandSetId, [MaybeNullWhen(false)] out ColorBandSet colorBandSet)
		{
			Debug.WriteLine($"SharedColorBandSetAdapter. Retrieving ColorBandSet with Id: {colorBandSetId}.");

			var sharedColorsReaderWriter = new SharedColorBandSetReaderWriter(_dbProvider);

			if (sharedColorsReaderWriter.TryGet(colorBandSetId, out var colorBandSetRecord))
			{
				colorBandSet = _mSetRecordMapper.MapFrom(colorBandSetRecord);
				return true;
			}
			else
			{
				colorBandSet = null;
				return false;
			}
		}

		public ColorBandSet CreateColorBandSet(ColorBandSet colorBandSet)
		{
			var sharedColorsReaderWriter = new SharedColorBandSetReaderWriter(_dbProvider);
			var colorBandSetRecord = _mSetRecordMapper.MapTo(colorBandSet);
			var id = sharedColorsReaderWriter.Insert(colorBandSetRecord);
			colorBandSetRecord = sharedColorsReaderWriter.Get(id);

			if (colorBandSetRecord == null)
			{
				throw new InvalidOperationException("The SharedColorBandSetAdpater could not insert the ColorBandSet Record.");
			}

			var result = _mSetRecordMapper.MapFrom(colorBandSetRecord);
			Debug.Assert(id == result.Id, "The SharedColorBandSetAdpater is returning a different Id than the one just inserted.");

			return result;
		}

		public void UpdateColorBandSetName(ObjectId colorBandSetId, string? name)
		{
			var sharedColorsReaderWriter = new SharedColorBandSetReaderWriter(_dbProvider);
			sharedColorsReaderWriter.UpdateName(colorBandSetId, name);
		}

		public void UpdateColorBandSetDescription(ObjectId colorBandSetId, string? description)
		{
			var sharedColorsReaderWriter = new SharedColorBandSetReaderWriter(_dbProvider);
			sharedColorsReaderWriter.UpdateDescription(colorBandSetId, description);
		}

		public bool ColorBandSetExists(string name)
		{
			var sharedColorsReaderWriter = new SharedColorBandSetReaderWriter(_dbProvider);
			var result = sharedColorsReaderWriter.Exists(name);

			return result;
		}

		#endregion

		#region ColorBandSetInfo

		public IEnumerable<ColorBandSetInfo> GetAllColorBandSetInfos()
		{
			var colorsReaderWriter = new SharedColorBandSetReaderWriter(_dbProvider);

			var allColorBandSetRecords = colorsReaderWriter.GetAll();

			var result = allColorBandSetRecords.Select(x => new ColorBandSetInfo(x.Id, x.Name, x.Description, x.LastAccessed, x.ColorBandsSerialNumber, x.ColorBandRecords.Length, x.TargetIterations));

			return result;
		}

		public ColorBandSetInfo? GetColorBandSetInfo(ObjectId id)
		{
			var colorsReaderWriter = new SharedColorBandSetReaderWriter(_dbProvider);
			var cbsRecord = colorsReaderWriter.Get(id);

			if (cbsRecord != null)
			{
				var targetIterations = cbsRecord.TargetIterations == 0 ? cbsRecord.ColorBandRecords.Max(y => y.CutOff) : cbsRecord.TargetIterations;
				var result = new ColorBandSetInfo(cbsRecord.Id, cbsRecord.Name, cbsRecord.Description, cbsRecord.LastAccessed, cbsRecord.ColorBandsSerialNumber, cbsRecord.ColorBandRecords.Length, targetIterations);

				return result;
			}
			else
			{
				return null;
			}

		}

		#endregion
	}
}
