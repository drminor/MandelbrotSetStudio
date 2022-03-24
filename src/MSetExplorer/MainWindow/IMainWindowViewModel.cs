﻿using MSS.Types;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMainWindowViewModel
	{
		event PropertyChangedEventHandler PropertyChanged;

		IMapProjectViewModel MapProjectViewModel { get; }
		IMapDisplayViewModel MapDisplayViewModel { get; }
		IColorBandSetViewModel ColorBandSetViewModel { get; }

		int TargetIterations { get; set; }
		int Steps { get; set; }

		IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string initalName, DialogType dialogType);
	}
}