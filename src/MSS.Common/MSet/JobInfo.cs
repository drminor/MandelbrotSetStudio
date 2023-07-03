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
