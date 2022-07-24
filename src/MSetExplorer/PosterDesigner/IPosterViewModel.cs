﻿using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace MSetExplorer
{
	internal interface IPosterViewModel
	{
		bool InDesignMode { get; }
		event PropertyChangedEventHandler? PropertyChanged;

		SizeDbl CanvasSize { get; set; }
		SizeDbl LogicalDisplaySize { get; set; }

		Poster? CurrentPoster { get; }
		string? CurrentPosterName { get; }
		bool CurrentPosterOnFile { get; }
		bool CurrentPosterIsDirty { get; }

		MapAreaInfo PosterAreaInfo { get; }
		SizeInt PosterSize { get; }

		ColorBandSet ColorBandSet { get; set; }

		VectorInt DisplayPosition { get; set; }
		double DisplayZoom { get; set; }

		JobAreaAndCalcSettings JobAreaAndCalcSettings { get; }

		void UpdateMapView(TransformType transformType, RectangleInt newArea);
		void ResetMapView(MapAreaInfo newMapAreaInfo);
		void UpdateColorBandSet(ColorBandSet colorBandSet);


		bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster);
		bool Open(string name);
		void Load(Poster poster);
		void Save();
		bool SaveAs(string name, string? description);
		void Close();
	}
}
