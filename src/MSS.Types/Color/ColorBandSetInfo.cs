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

		public ColorBandSetInfo(ObjectId id, string? name, string? description, DateTime lastAccessed, Guid colorBandSerialNumber, int numberOfBands, int maxIterations)
		{
			Debug.WriteLine($"Constructing ColorBandSetInfo with Id: {id}.");
			Id = id;
			_name = name ?? throw new ArgumentNullException(nameof(name), "When creating a ColorBandSetInfo, the name cannot be null.");
			_description = description;
			LastAccessed = lastAccessed;
			NumberOfBands = numberOfBands;
			MaxIterations = maxIterations;
			ColorBandSerialNumber = colorBandSerialNumber;
		}

		#endregion

		#region Public Properties

		public ObjectId Id { get; init; }
		public DateTime DateCreated => Id.CreationTime;
		public DateTime LastAccessed { get; init; }
		public int NumberOfBands { get; init; }
		public int MaxIterations { get; init; }
		public Guid ColorBandSerialNumber { get; init; }

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
