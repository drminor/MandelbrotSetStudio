using MSetRepo;
using MSS.Types;
using MSS.Types.MSet;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;

namespace MSetExplorer
{
	public class ColorBandViewModel : ViewModelBase, IColorBandViewModel
	{
		private readonly ProjectAdapter _projectAdapter;

		private Job _currentJob;
		private ColorBandSet _colorBandSet;
		private ColorBand _selectedColorBand;

		#region Constructor

		public ColorBandViewModel(ProjectAdapter projectAdapter)
		{
			_projectAdapter = projectAdapter;
			CurrentJob = null;
			_colorBandSet = null;
			ColorBands = new ObservableCollection<ColorBand>();
			SelectedColorBand = null;
		}

		#endregion

		#region Public Properties

		public Job CurrentJob
		{
			get => _currentJob;
			set
			{
				if (value != _currentJob)
				{
					_currentJob = value;
					SetColorBandSet(GetColorBands(value));
					OnPropertyChanged();
				}
			}
		}

		public ObservableCollection<ColorBand> ColorBands { get; private set; }

		public ColorBand SelectedColorBand
		{
			get => _selectedColorBand;

			set
			{
				_selectedColorBand = value;
				OnPropertyChanged();
			}
		}

		#endregion

		#region Private Methods

		private void SetColorBandSet(ColorBandSet value)
		{
			if (value == null)
			{
				ColorBands.Clear();
			}
			else
			{
				if (_colorBandSet == null || _colorBandSet != value)
				{
					ColorBands.Clear();
					foreach (var c in value)
					{
						ColorBands.Add(c);
					}

					var view = CollectionViewSource.GetDefaultView(ColorBands);
					_ = view.MoveCurrentTo(ColorBands.FirstOrDefault());
				}
			}

			_colorBandSet = value;
		}

		private ColorBandSet GetColorBands(Job job)
		{
			var result = job == null ? null : _projectAdapter.GetColorBands(job);
			return result;
		}

		#endregion
	}
}
