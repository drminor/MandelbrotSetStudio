﻿using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections.ObjectModel;

namespace MSetExplorer
{
	internal interface IMapDisplayViewModel
	{
		//bool InDesignMode { get; }
		SizeInt BlockSize { get; }
		ObservableCollection<MapSection> MapSections { get; }

		Action<MapSection> HandleMapSectionReady { get; }
		Action HandleMapNav { get; }
	}
}