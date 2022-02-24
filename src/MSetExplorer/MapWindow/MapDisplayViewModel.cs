﻿using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections.ObjectModel;

namespace MSetExplorer
{
	internal class MapDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		public MapDisplayViewModel(SizeInt blockSize)
		{
			BlockSize = blockSize;
			MapSections = new ObservableCollection<MapSection>();
		}

		public new bool InDesignMode => base.InDesignMode;

		public SizeInt BlockSize { get; }
		public SizeInt CanvasSize { get; set; }

		private VectorInt _canvasControlOffset;

		public VectorInt CanvasControlOffset
		{ 
			get => _canvasControlOffset;
			set { _canvasControlOffset = value; OnPropertyChanged(); }
		}

		public ObservableCollection<MapSection> MapSections { get; }

		//public Action<MapSection> MapSectionReadyHandler => HandleMapSectionReady;
		//public Action<VectorInt> MapNavHandler => HandleMapNav;

		//private void HandleMapSectionReady(MapSection mapSection)
		//{
		//	MapSections.Add(mapSection);
		//}

		//private void HandleMapNav(VectorInt canvasControOffset)
		//{
		//	CanvasControlOffset = canvasControOffset;
		//	MapSections.Clear();
		//}
	}
}
