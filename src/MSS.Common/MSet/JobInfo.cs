using MongoDB.Bson;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSS.Common.MSet
{
	public class JobInfo : IJobInfo
	{
		#region Private Fields

		private ObjectId _id;
		private DateTime _dateCreatedUtc;

		#endregion

		#region Constructor

		public JobInfo(ObjectId id, ObjectId? parentJobId, DateTime dateCreatedUtc, int transformType, ObjectId subdivisionId, int mapCoordExponent)
		{
			Id = id;
			ParentJobId = parentJobId;
			DateCreatedUtc = dateCreatedUtc;
			TransformType = transformType;
			SubdivisionId = subdivisionId;
			MapCoordExponent = mapCoordExponent;
		}


		#endregion

		#region Public Properties

		public ObjectId Id
		{
			get => _id;
			set { _id = value; OnPropertyChanged(); }
		}

		public ObjectId? ParentJobId { get; set; }

		public DateTime DateCreatedUtc
		{
			get => _dateCreatedUtc;
			set { _dateCreatedUtc = value; OnPropertyChanged(); }
		}

		public int TransformType { get; set; }

		public ObjectId SubdivisionId { get; set; }

		public int MapCoordExponent { get; set; }

		private int _stat1;
		private int _stat2;
		private int _stat3;

		public int Stat1
		{
			get => _stat1;
			set { _stat1 = value; OnPropertyChanged(); }
		}

		public int Stat2
		{
			get => _stat2;
			set { _stat2 = value; OnPropertyChanged(); }
		}

		public int Stat3
		{
			get => _stat3;
			set { _stat3 = value; OnPropertyChanged(); }
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
