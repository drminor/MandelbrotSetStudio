using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MSS.Common
{
	public class Poster : INotifyPropertyChanged, ICloneable 
	{
		private string _name;
		private string? _description;
		private string _size;

		private MapAreaInfo _mapAreaInfo;
		private ColorBandSet _colorBandSet;
		private MapCalcSettings _mapCalcSettings;

		private VectorInt _dispPosition;
		private double _displayZoom;

		private DateTime _lastUpdatedUtc;
		private DateTime _lastSavedUtc;

		#region Constructor

		public Poster(string name, string? description, ObjectId sourceJobId, MapAreaInfo mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings)
			: this(ObjectId.GenerateNewId(), name, description, sourceJobId, mapAreaInfo, colorBandSet, mapCalcSettings, new VectorInt(), 1.0d,
				  DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow)
		{
			OnFile = false;
		}

		public Poster(ObjectId id, string name, string? description, ObjectId sourceJobId, MapAreaInfo mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings,
			VectorInt displayPosition, double displayZoom,
			DateTime dateCreatedUtc, DateTime lastSavedUtc, DateTime lastAccessedUtc)
		{
			Id = id;

			_name = name ?? throw new ArgumentNullException(nameof(name));
			_description = description;

			SourceJobId = sourceJobId;

			_mapAreaInfo = mapAreaInfo;
			_colorBandSet = colorBandSet;
			_mapCalcSettings = mapCalcSettings;

			_dispPosition = displayPosition;
			_displayZoom = displayZoom;

			DateCreatedUtc = dateCreatedUtc;
			LastUpdatedUtc = DateTime.MinValue;
			LastSavedUtc = lastSavedUtc;
			LastAccessedUtc = lastAccessedUtc;

			_size = string.Empty;
			Size = GetFormattedPosterSize(mapAreaInfo.CanvasSize);

			//Debug.WriteLine($"Poster is loaded.");
		}

		#endregion

		#region Public Properties

		public ObjectId Id { get; init; }

		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public string? Description
		{
			get => _description;
			set
			{
				if (_description != value)
				{
					_description = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public string Size
		{
			get => _size;
			private set
			{
				if (value != _size)
				{
					_size = value;
					OnPropertyChanged();
				}
			}
		}

		public ObjectId SourceJobId { get; init; }

		public MapAreaInfo MapAreaInfo
		{
			get => _mapAreaInfo;
			set
			{
				if (value != _mapAreaInfo)
				{
					_mapAreaInfo = value;
					Size = GetFormattedPosterSize(value.CanvasSize);
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;
			set
			{
				if (value != _colorBandSet)
				{
					_colorBandSet = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public MapCalcSettings MapCalcSettings
		{
			get => _mapCalcSettings;
			set
			{
				if (value != _mapCalcSettings)
				{
					_mapCalcSettings = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public VectorInt DisplayPosition
		{
			get => _dispPosition;
			set
			{
				if (value != _dispPosition)
				{
					_dispPosition = value;
					//LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>
		/// Value between 1.0 and a maximum, where the maximum is posterSize / displaySize
		/// 1.0 presents 1 map "pixel" to 1 screen pixel
		/// 2.0 presents 2 map "pixels" to 1 screen pixel
		/// </summary>
		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{
				if (Math.Abs(value - _displayZoom) > 0.1)
				{
					_displayZoom = value;
					//LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public DateTime DateCreatedUtc { get; init; }

		public DateTime LastSavedUtc
		{
			get => _lastSavedUtc;
			private set
			{
				_lastSavedUtc = value;
				LastUpdatedUtc = value;
				OnFile = true;
			}
		}

		public DateTime LastUpdatedUtc
		{
			get => _lastUpdatedUtc;

			private set
			{
				var isDirtyBefore = IsDirty;
				_lastUpdatedUtc = value;

				if (IsDirty != isDirtyBefore)
				{
					OnPropertyChanged(nameof(IsDirty));
				}
			}
		}

		public DateTime LastAccessedUtc { get; init; }

		public bool IsDirty => LastUpdatedUtc > LastSavedUtc;
		public bool OnFile { get; private set; }

		#endregion

		#region Public Methods 

		public bool Save(IProjectAdapter projectAdapter)
		{
			projectAdapter.UpdatePoster(this);
			LastSavedUtc = DateTime.UtcNow;
			return true;
		}

		#endregion

		#region Private Methods

		private string GetFormattedPosterSize(SizeInt size)
		{
			var result = $"{size.Width} x {size.Height}";
			return result;
		}

		#endregion

		#region ICloneable Support

		object ICloneable.Clone()
		{
			return Clone();
		}

		Poster Clone()
		{
			return new Poster(Name, Description, SourceJobId, MapAreaInfo.Clone(), ColorBandSet, MapCalcSettings);
		}

		#endregion

		#region Property Changed Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
