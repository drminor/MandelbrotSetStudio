using MongoDB.Bson;
using MSetRepo;
using MSS.Types;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace MSetExplorer
{
	public class ColorBandSetOpenSaveViewModel : IColorBandSetOpenSaveViewModel, INotifyPropertyChanged
	{
		private readonly SharedColorBandSetAdapter _sharedColorBandSetAdapter;
		private ColorBandSetInfo? _selectedColorBandSetInfo;

		private string? _selectedName;
		private string? _selectedDescription;

		private bool _userIsSettingTheName;

		#region Constructor

		public ColorBandSetOpenSaveViewModel(SharedColorBandSetAdapter sharedColorBandSetAdapter, string? initialName, DialogType dialogType)
		{
			_sharedColorBandSetAdapter = sharedColorBandSetAdapter;
			DialogType = dialogType;

			ColorBandSetInfos = new ObservableCollection<ColorBandSetInfo>(_sharedColorBandSetAdapter.GetAllColorBandSetInfos());
			_selectedColorBandSetInfo = ColorBandSetInfos.FirstOrDefault(x => x.Name == initialName);

			var view = CollectionViewSource.GetDefaultView(ColorBandSetInfos);
			_ = view.MoveCurrentTo(SelectedColorBandSetInfo);
		}

		#endregion

		#region Public Methods 

		public bool ExportColorBandSet(ColorBandSet colorBandSet)
		{
			_sharedColorBandSetAdapter.CreateColorBandSet(colorBandSet);
			return true;
		}

		public bool TryImportColorBandSet(ObjectId colorBandSetId, [MaybeNullWhen(false)] out ColorBandSet colorBandSet)
		{
			var result = _sharedColorBandSetAdapter.TryGetColorBandSet(colorBandSetId, out colorBandSet);
			return result;
		}

		#endregion

		#region Public Properties

		public DialogType DialogType { get; }

		public ObservableCollection<ColorBandSetInfo> ColorBandSetInfos { get; init; }

		public string? SelectedName
		{
			get => _selectedName;
			set
			{
				_selectedName = value;
				OnPropertyChanged();
			}
		}

		public bool UserIsSettingTheName
		{
			get => _userIsSettingTheName;
			set { _userIsSettingTheName = value; OnPropertyChanged(); }
		}


		public string? SelectedDescription
		{
			get => _selectedDescription;
			set
			{
				_selectedDescription = value;

				if (SelectedColorBandSetInfo != null && SelectedColorBandSetInfo.Description != value)
				{
					_sharedColorBandSetAdapter. UpdateColorBandSetDescription(SelectedColorBandSetInfo.Id, SelectedDescription);
					SelectedColorBandSetInfo.Description = value;
				}

				OnPropertyChanged();
			}
		}

		public ColorBandSetInfo? SelectedColorBandSetInfo
		{
			get => _selectedColorBandSetInfo;

			set
			{
				_selectedColorBandSetInfo = value;

				if (value != null)
				{
					if (!_userIsSettingTheName)
					{
						SelectedName = _selectedColorBandSetInfo?.Name;
					}

					SelectedDescription = _selectedColorBandSetInfo?.Description;
				}
				else
				{
					SelectedName = null;
					SelectedDescription = null;
				}

				OnPropertyChanged();
			}
		}

		public bool IsNameTaken(string name)
		{
			var result = _sharedColorBandSetAdapter.ColorBandSetExists(name);
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
