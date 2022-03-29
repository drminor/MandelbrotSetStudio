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

		public ColorBandSetInfo(ObjectId id, DateTime dateCreated, int numberOfBands, Guid serialNumber,
			string name, string? description, int versionNumber)
		{
			Debug.WriteLine($"Constructing ColorBandSetInfo with SerialNumber: {serialNumber}.");
			Id = id;
			DateCreated = dateCreated;
			NumberOfBands = numberOfBands;
			SerialNumber = serialNumber;

			_name = name;
			_description = description;
			_versionNumber = versionNumber;
		}

		#endregion

		#region Public Properties

		public ObjectId Id { get; init; }

		public DateTime DateCreated { get; init; }

		public int NumberOfBands { get; init; }

		public Guid SerialNumber { get; init; }

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
			var result = $"ColorBandSetInfo: {SerialNumber}";
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
