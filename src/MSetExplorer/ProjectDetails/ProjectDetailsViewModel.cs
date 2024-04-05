using MSS.Common;
using MSS.Common.MSet;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSetExplorer
{
	public class ProjectDetailsViewModel : INotifyPropertyChanged
	{
		private readonly IProjectAdapter _projectAdapter;
		private readonly Project _project;

		#region Constructor

		public ProjectDetailsViewModel(IProjectAdapter projectAdapter, Project project)
		{
			_projectAdapter = projectAdapter;
			_project = project;
		}

		#endregion

		#region Public Properties

		public string Name
		{
			get => _project.Name;
			set
			{
				_project.Name = value;
				OnPropertyChanged();
			}
		}

		public string? Description
		{
			get => _project.Description;
			set
			{
				_project.Description = value;
				OnPropertyChanged();
			}
		}

		#endregion

		#region Public Methods

		public bool IsNameTaken(string? name)
		{
			var result = name != null && _projectAdapter.ProjectExists(name, out _);
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
