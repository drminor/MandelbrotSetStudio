using ImageBuilder;
using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MSetExplorer
{
	public class PosterOpenSaveViewModel : IPosterOpenSaveViewModel, INotifyPropertyChanged
	{
		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly ProjectAdapter _projectAdapter;
		private Poster? _selectedPoster;

		private string? _selectedName;
		private string? _selectedDescription;

		private bool _userIsSettingTheName;

		#region Constructor

		public PosterOpenSaveViewModel(IMapLoaderManager mapLoaderManager, ProjectAdapter projectAdapter, string? initialName, DialogType dialogType)
		{
			_mapLoaderManager = mapLoaderManager;
			_projectAdapter = projectAdapter;
			DialogType = dialogType;

			Posters = new ObservableCollection<Poster>(_projectAdapter.GetAllPosters());
			SelectedPoster = Posters.FirstOrDefault(x => x.Name == initialName);

			if (SelectedPoster == null)
			{
				SelectedName = initialName;
				_userIsSettingTheName = true;
			}

			var view = CollectionViewSource.GetDefaultView(Posters);
			_ = view.MoveCurrentTo(SelectedPoster);
		}

		#endregion

		#region Public Properties

		public DialogType DialogType { get; }

		public ObservableCollection<Poster> Posters { get; init; }

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
			set
			{
				_userIsSettingTheName = value;
				OnPropertyChanged();
			}
		}


		public string? SelectedDescription
		{
			get => _selectedDescription;
			set
			{
				_selectedDescription = value;

				if (SelectedPoster != null && SelectedPoster.Id != ObjectId.Empty && SelectedPoster.Description != value)
				{
					_projectAdapter.UpdateProjectDescription(SelectedPoster.Id, SelectedDescription);
					SelectedPoster.Description = value;
				}

				OnPropertyChanged();
			}
		}

		public Poster? SelectedPoster
		{
			get => _selectedPoster;

			set
			{
				_selectedPoster = value;
				if (value != null)
				{
					if (!_userIsSettingTheName)
					{
						SelectedName = _selectedPoster?.Name;
					}

					SelectedDescription = _selectedPoster?.Description;
				}
				else
				{
					SelectedName = null;
					SelectedDescription = null;
				}

				OnPropertyChanged();
			}
		}

		#endregion

		#region Public Methods

		public bool IsNameTaken(string? name)
		{
			var result = name != null && _projectAdapter.PosterExists(name);
			return result;
		}

		public void DeleteSelected()
		{
			var poster = SelectedPoster;

			if (poster != null)
			{
				_projectAdapter.DeleteProject(poster.Id);
				_ = Posters.Remove(poster);
			}
		}

		public byte[]? GetPreviewImageData(SizeInt imageSize)
		{
			var poster = SelectedPoster;

			if (poster == null)
			{
				throw new InvalidOperationException("No Selected Poster.");
			}

			var bitmapBuilder = new BitmapBuilder(_mapLoaderManager);

			//Task.Run(async bitmapBuilder.BuildAsync(poster.))

			var cts = new CancellationTokenSource();
			var posterAreaInfo = poster.MapAreaInfo;
			var previewMapArea = new JobAreaInfo(posterAreaInfo.Coords, imageSize, posterAreaInfo.Subdivision, posterAreaInfo.MapBlockOffset, posterAreaInfo.CanvasControlOffset);

			//byte[]? result = null;

			var task = Task.Run(async () => await bitmapBuilder.BuildAsync(previewMapArea, poster.ColorBandSet, poster.MapCalcSettings, cts.Token, StatusCallBack));

			var result = task.Result;

			//task.GetAwaiter().GetResult();

			//Task<LogEntity> task = Task.Run<LogEntity>(async () => await GetLogAsync());
			//return task.Result;

			//var task = bitmapBuilder.BuildAsync(previewMapArea, poster.ColorBandSet, poster.MapCalcSettings, StatusCallBack, cts.Token); 
			//var result = task.GetAwaiter()
			return result;
		}

		private Progress<double> _previewImageDataBuilderProgress = new Progress<double>();

		private void StatusCallBack(double value)
		{
			((IProgress<double>)_previewImageDataBuilderProgress).Report(value);
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
