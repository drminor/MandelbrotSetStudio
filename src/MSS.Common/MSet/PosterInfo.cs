using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSS.Common.MSet
{
	public class PosterInfo : IPosterInfo
	{
		private string _name;
		private string? _description;
		private DateTime _lastSavedUtc;
		private DateTime _lastAccessedUtc;

		#region Constructor

		public PosterInfo(ObjectId posterId, string name, string? description, ObjectId currentJobId, 
			SizeDbl size, int bytes, 
			DateTime dateCreatedUtc, DateTime lastSavedUtc, DateTime lastAccessedUtc)
		{
			PosterId = posterId;
			_name = name;
			_description = description;
			CurrentJobId = currentJobId;
			
			Size = size;
			Bytes = bytes;

			DateCreatedUtc = dateCreatedUtc;
			_lastSavedUtc = lastSavedUtc;
			_lastAccessedUtc = lastAccessedUtc;

			SizeAsString = $"{size.Width} x {size.Height}";
		}

		#endregion

		#region Public Properties

		public OwnerType OwnerType => OwnerType.Project;
		public ObjectId OwnerId => PosterId;

		public ObjectId PosterId { get; init; }

		public string Name
		{
			get => _name;
			set { _name = value; OnPropertyChanged(); }
		}

		public string? Description
		{
			get => _description;
			set { _description = value; OnPropertyChanged(); }
		}

		public ObjectId CurrentJobId { get; init; }

		public SizeDbl Size { get; init; }

		public int Bytes { get; set; }

		public string SizeAsString { get; set; }

		public DateTime DateCreatedUtc { get; init; }

		public DateTime LastSavedUtc
		{
			get => _lastSavedUtc;
			set { _lastSavedUtc = value; OnPropertyChanged(); }
		}

		public DateTime LastAccessedUtc
		{
			get => _lastAccessedUtc;
			set { _lastAccessedUtc = value; OnPropertyChanged(); }
		}

		public int NumberOfJobs { get; init; }
		public int MinMapCoordsExponent { get; init; }
		public int MinSamplePointDeltaExponent { get; init; }

		#endregion

		#region NotifyPropertyChanged Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}
}
