﻿using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace MSetExplorer
{
	public interface IPosterViewModel
	{
		bool InDesignMode { get; }
		event PropertyChangedEventHandler? PropertyChanged;

		SizeInt CanvasSize { get; set; }
		SizeInt LogicalDisplaySize { get; set; }

		Poster? CurrentPoster { get; }
		string? CurrentPosterName { get; }
		bool CurrentPosterOnFile { get; }
		bool CurrentPosterIsDirty { get; }

		JobAreaInfo PosterAreaInfo { get; }
		SizeInt PosterSize { get; set; }

		ColorBandSet? ColorBandSet { get; set; }

		VectorInt DisplayPosition { get; set; }
		double DisplayZoom { get; set; }

		JobAreaAndCalcSettings JobAreaAndCalcSettings { get; }

		void UpdateMapView(TransformType transformType, RectangleInt newArea);
		//void UpdateMapCoordinates(RRectangle coords);
		void UpdateColorBandSet(ColorBandSet colorBandSet);

		bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster);
		bool PosterOpen(string name);
		void LoadPoster(Poster poster);
		void PosterSave();
		bool PosterSaveAs(string name, string? description);
		void PosterClose();
	}
}
