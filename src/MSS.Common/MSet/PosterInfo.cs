using MongoDB.Bson;
using MSS.Types;
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

		public PosterInfo(ObjectId posterId, string name, string? description, ObjectId currentJobId, SizeInt size, DateTime dateCreatedUtc, DateTime lastSavedUtc, DateTime lastAccessedUtc)
		{
			PosterId = posterId;
			_name = name;
			_description = description;
			CurrentJobId = currentJobId;
			Size = size;
			DateCreatedUtc = dateCreatedUtc;
			_lastSavedUtc = lastSavedUtc;
			_lastAccessedUtc = lastAccessedUtc;

			SizeAsString = $"{size.Width} x {size.Height}";
		}

		#endregion

		#region Public Properties

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

		public SizeInt Size { get; init; }

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
