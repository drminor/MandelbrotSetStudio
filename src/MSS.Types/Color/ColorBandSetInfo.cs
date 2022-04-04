using MongoDB.Bson;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MSS.Types
{
	public class ColorBandSetInfo : INotifyPropertyChanged
	{
		#region Constructor

		public ColorBandSetInfo(ObjectId id, ObjectId? parentId, DateTime dateCreated, int numberOfBands, string name, string? description)
		{
			Debug.WriteLine($"Constructing ColorBandSetInfo with Id: {id}.");
			Id = id;
			ParentId = parentId;
			DateCreated = dateCreated;
			NumberOfBands = numberOfBands;

			_name = name;
			_description = description;
		}

		#endregion

		#region Public Properties

		public ObjectId Id { get; init; }

		public ObjectId? ParentId { get; init; }

		public DateTime DateCreated { get; init; }

		public int NumberOfBands { get; init; }

		private string _name;
		public string Name
		{
			get => _name;
			set
			{
				if (value != _name)
				{
					_name = value;
					OnPropertyChanged();
				}
			}
		}

		private string? _description;
		public string? Description
		{
			get => _description;
			set
			{
				if (value != _description)
				{
					_description = value;
					OnPropertyChanged();
				}
			}
		}

		private int _versionNumber;
		public int VersionNumber
		{
			get => _versionNumber;
			set
			{
				if (value != _versionNumber)
				{
					_versionNumber = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region ToString Support

		public override string ToString()
		{
			var result = $"ColorBandSetInfo: {Id}";
			return result;
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
