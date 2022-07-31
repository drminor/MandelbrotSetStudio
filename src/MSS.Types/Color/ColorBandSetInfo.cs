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

		public ColorBandSetInfo(ObjectId id, int maxIterations, int numberOfBands, string? name, string? description)
		{
			Debug.WriteLine($"Constructing ColorBandSetInfo with Id: {id}.");
			Id = id;
			MaxIterations = maxIterations;
			NumberOfBands = numberOfBands;
			_name = name ?? throw new ArgumentNullException("When creating a ColorBandSetInfo, the name cannot be null.");
			_description = description;
		}

		#endregion

		#region Public Properties

		public ObjectId Id { get; init; }
		public DateTime DateCreated => Id.CreationTime;
		public int MaxIterations { get; init; }
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
