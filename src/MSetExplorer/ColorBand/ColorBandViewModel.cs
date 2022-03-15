﻿using MSetRepo;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

		public new bool InDesignMode => base.InDesignMode;

		public ObservableCollection<ColorBand> ColorBands { get; private set; }

		public Job CurrentJob
		{
			get => _currentJob;
			set
			{
				if (value != _currentJob)
				{
					_currentJob = value;
					ColorBandSet = GetColorBands(value);
					OnPropertyChanged(nameof(IColorBandViewModel.CurrentJob));
				}
			}
		}

		public ColorBand SelectedColorBand
		{
			get => _selectedColorBand;

			set
			{
				_selectedColorBand = value;
				OnPropertyChanged(nameof(IColorBandViewModel.SelectedColorBand));
			}
		}

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;

			private set
			{
				Debug.WriteLine($"ColorBandViewModel is having is CBSet updated. Current = {_colorBandSet?.SerialNumber}, New = {value?.SerialNumber}");
				if (value == null)
				{
					if (_colorBandSet != null)
					{
						ColorBands.Clear();
						_colorBandSet = value;
						Debug.WriteLine("ColorBandViewModel is clearing its collection. (non-null => null.)");
						OnPropertyChanged(nameof(IColorBandViewModel.ColorBandSet));
					}
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


						if (_colorBandSet == null)
						{
							Debug.WriteLine("ColorBandViewModel is updating its collection. (null => non-null.)");
						}
						else
						{
							Debug.WriteLine("ColorBandViewModel is updating its collection. (non-null => non-null.)");
						}
						_colorBandSet = value;

						OnPropertyChanged(nameof(IColorBandViewModel.ColorBandSet));
					}
				}
			}
		}

		public int? HighCutOff
		{
			get => _colorBandSet?.HighCutOff;
			set
			{
				if (value.HasValue)
				{
					if (_colorBandSet != null)
					{
						_colorBandSet.HighCutOff = value.Value;
						OnPropertyChanged(nameof(IColorBandViewModel.HighCutOff));
					}
				}
			}
		}

		#endregion

		#region Public Methods

		public void Test()
		{
			var newColorBandSet = ColorBandSet.CreateNewCopy();
			var len = newColorBandSet.Count;

			var ocb = newColorBandSet[len - 3];
			var ocb1 = newColorBandSet[1];
			var ncb = new ColorBand(ocb.CutOff + 50, ocb1.StartColor, ocb1.BlendStyle, ocb1.EndColor);

			newColorBandSet.Insert(len - 2, ncb);

			ColorBandSet = newColorBandSet;
		}

		//public void RaiseColorBandSetChanged()
		//{
		//	OnPropertyChanged(nameof(IColorBandViewModel.ColorBandSet));
		//}

		#endregion

		#region Private Methods

		private ColorBandSet GetColorBands(Job job)
		{
			var result = job == null ? null : _projectAdapter.GetColorBands(job);
			return result;
		}

		#endregion
	}
}
