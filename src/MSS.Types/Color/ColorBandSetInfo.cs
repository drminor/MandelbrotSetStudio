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

		public ColorBandSetInfo(ObjectId id, string name, int version, int targetIterations, string? description, DateTime dateLastUsed, Guid colorBandSerialNumber, int numberOfBands, int numberOfJobs)
		{
			Debug.WriteLine($"Constructing ColorBandSetInfo with Id: {id}.");
			Id = id;
			_name = name;
			TargetIterations = targetIterations;
			Version = version;
			_description = description;
			DateLastUsed = dateLastUsed;
			NumberOfBands = numberOfBands;
			ColorBandSerialNumber = colorBandSerialNumber;
			NumberOfJobs = numberOfJobs;
		}

		#endregion

		#region Public Properties

		public ObjectId Id { get; init; }

		public int Version { get; init; }
		public int TargetIterations { get; init; }
		public DateTime DateCreated => Id.CreationTime;
		public DateTime DateLastUsed { get; init; }
		public int NumberOfBands { get; init; }
		public Guid ColorBandSerialNumber { get; init; }
		public int NumberOfJobs { get; set; }
		public bool? IsLatestVersion { get; set; }

		public string VersionFontWeight => IsLatestVersion == true ? "Bold" : "Normal";

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
