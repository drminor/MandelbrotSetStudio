using MongoDB.Bson;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSS.Types.MSet
{
	public class JobInfo : IJobInfo
	{
		#region Private Fields

		private ObjectId _id;
		private DateTime _dateCreatedUtc;

		private int _numberOfMapSections;

		//private int _numberOfFullScale;
		//private int _numberOfReducedScale;
		//private int _numberOfImage;
		//private int _numberOfSizeEditorPreview;

		private int _numberOfCrtical;
		private int _numberOfNonCritical;

		private double _percentageMapSectionsShared;
		private double _percentageMapSectionsSharedWithSameOwner;

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

		public bool IsCurrentOnOwner { get; set; }

		public DateTime DateCreatedUtc
		{
			get => _dateCreatedUtc;
			set { _dateCreatedUtc = value; OnPropertyChanged(); }
		}

		public int TransformType { get; set; }

		public ObjectId SubdivisionId { get; set; }

		public int MapCoordExponent { get; set; }

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

		public int NumberOfCritical
		{
			get => _numberOfCrtical;
			set
			{
				if (value != _numberOfCrtical)
				{
					_numberOfCrtical = value;
					OnPropertyChanged();
				}
			}
		}

		public int NumberOfNonCritical
		{
			get => _numberOfNonCritical;
			set
			{
				if (value != _numberOfNonCritical)
				{
					_numberOfNonCritical = value;
					OnPropertyChanged();
				}
			}
		}

		//public int NumberOfFullScale
		//{
		//	get => _numberOfFullScale;
		//	set
		//	{
		//		if (value != _numberOfFullScale)
		//		{
		//			_numberOfFullScale = value;
		//			OnPropertyChanged();
		//		}
		//	}
		//}

		//public int NumberOfReducedScale
		//{
		//	get => _numberOfReducedScale;
		//	set
		//	{
		//		if (value != _numberOfReducedScale)
		//		{
		//			_numberOfReducedScale = value;
		//			OnPropertyChanged();
		//		}
		//	}
		//}

		//public int NumberOfImage
		//{
		//	get => _numberOfImage;
		//	set
		//	{
		//		if (value != _numberOfImage)
		//		{
		//			_numberOfImage = value;
		//			OnPropertyChanged();
		//		}
		//	}
		//}

		//public int NumberOfSizeEditorPreview
		//{
		//	get => _numberOfSizeEditorPreview;
		//	set
		//	{
		//		if (value != _numberOfSizeEditorPreview)
		//		{
		//			_numberOfSizeEditorPreview = value;
		//			OnPropertyChanged();
		//		}
		//	}
		//}

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
