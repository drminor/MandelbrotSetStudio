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

			//NumberOfMapSections = 3;

			//PercentageMapSectionsShared = 1.21;
			//PercentageMapSectionsSharedWithSameOwner = 8.12;
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

		private int _numberOfMapSections;

		private double _percentageMapSectionsShared;
		private double _percentageMapSectionsSharedWithSameOwner;

		public int NumberOfMapSections
		{
			get => _numberOfMapSections;
			set
			{
				if (value != _numberOfMapSections)
				{
					_numberOfMapSections = value;
					OnPropertyChanged();
				}
			}
		}

		public double PercentageMapSectionsShared
		{
			get => _percentageMapSectionsShared;
			set
			{
				if (value != _percentageMapSectionsShared)
				{
					_percentageMapSectionsShared = value;
					OnPropertyChanged();
				}
			}
		}

		public double PercentageMapSectionsSharedWithSameOwner
		{
			get => _percentageMapSectionsSharedWithSameOwner;
			set
			{
				if (value != _percentageMapSectionsSharedWithSameOwner)
				{
					_percentageMapSectionsSharedWithSameOwner = value;
					OnPropertyChanged();
				}
			}
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
