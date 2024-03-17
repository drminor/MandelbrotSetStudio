using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using System.Collections.Generic;
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
		private readonly IProjectAdapter _projectAdapter;

		private ColorBandSetInfo? _selectedColorBandSetInfo;
		private string? _selectedName;
		private string? _selectedDescription;

		private bool _userIsSettingTheName;

		#region Constructor

		//public ColorBandSetOpenSaveViewModel(IProjectAdapter projectAdapter, ObjectId projectId, int targetIterations, string? initialName, DialogType dialogType)
		//	:this(projectAdapter, targetIterations, initialName, dialogType, projectAdapter.GetAllColorBandSetInfosForProject(projectId))
		//{ }

		public ColorBandSetOpenSaveViewModel(IProjectAdapter projectAdapter, int targetIterations, string? initialName, DialogType dialogType, IEnumerable<ColorBandSetInfo> cbsInfos)
		{
			_projectAdapter = projectAdapter;
			TargetIterations = targetIterations;
			DialogType = dialogType;

			ColorBandSetInfos = new ObservableCollection<ColorBandSetInfo>(cbsInfos);
			_selectedColorBandSetInfo = ColorBandSetInfos.FirstOrDefault(x => x.Name == initialName && x.MaxIterations == targetIterations);

			var view = CollectionViewSource.GetDefaultView(ColorBandSetInfos);
			_ = view.MoveCurrentTo(SelectedColorBandSetInfo);
		}


		#endregion

		#region Public Methods 

		//public bool SaveColorBandSet(ColorBandSet colorBandSet)
		//{
		//	_projectAdapter.InsertColorBandSet(colorBandSet);
		//	return true;
		//}

		//public bool TryOpenColorBandSet(ObjectId colorBandSetId, [MaybeNullWhen(false)] out ColorBandSet colorBandSet)
		//{
		//	var result = _projectAdapter.TryGetColorBandSet(colorBandSetId, out colorBandSet);
		//	return result;
		//}

		#endregion

		#region Public Properties

		public DialogType DialogType { get; }

		public int TargetIterations { get; init; }

		public ObservableCollection<ColorBandSetInfo> ColorBandSetInfos { get; init; }

		public string? SelectedName
		{
			get => _selectedName;
			set
			{
				_selectedName = value;

				if (DialogType != DialogType.Save)
				{
					if (value != null && SelectedColorBandSetInfo != null && SelectedColorBandSetInfo.Name != value)
					{
						_projectAdapter.UpdateColorBandSetName(SelectedColorBandSetInfo.Id, SelectedName);
						SelectedColorBandSetInfo.Name = value;
					}
				}

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
					_projectAdapter. UpdateColorBandSetDescription(SelectedColorBandSetInfo.Id, SelectedDescription);
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
			var result = ColorBandSetInfos.Any(x => x.Name == name && x.MaxIterations == TargetIterations);
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
