using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Windows;

namespace MSetExplorer
{
	internal interface IMapJobViewModel
	{
		//bool InDesignMode { get; }

		SizeInt BlockSize { get; init; }
		bool CanGoBack { get; }
		Job CurrentJob { get; }
		Action<MapSection> OnMapSectionReady { get; set; }

		Point GetBlockPosition(Point posYInverted);
		void GoBack(SizeInt canvasControlSize);
		void LoadMap(string jobName, SizeInt canvasControlSize, MSetInfo mSetInfo, SizeInt newArea);
	}
}