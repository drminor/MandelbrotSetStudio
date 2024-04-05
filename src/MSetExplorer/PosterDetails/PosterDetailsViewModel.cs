using MSS.Common;
using MSS.Common.MSet;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSetExplorer
{
	public class PosterDetailsViewModel : INotifyPropertyChanged
	{
		#region Private Fields

		private readonly IProjectAdapter _projectAdapter;
		private readonly Poster _poster;

		#endregion

		#region Constructor

		public PosterDetailsViewModel(IProjectAdapter projectAdapter, Poster poster)
		{
			_projectAdapter = projectAdapter;
			_poster = poster;
		}

		#endregion

		#region Public Properties

		public string Name
		{
			get => _poster.Name;
			set
			{
				_poster.Name = value;
				OnPropertyChanged();
			}
		}

		public string? Description
		{
			get => _poster.Description;
			set
			{
				_poster.Description = value;
				OnPropertyChanged();
			}
		}

		#endregion

		#region Public Methods

		public bool IsNameTaken(string? name)
		{
			var result = name != null && _projectAdapter.PosterExists(name, out _);
			return result;
		}

		#endregion

		#region INotifyPropertyChanged Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
