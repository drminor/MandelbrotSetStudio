using MSS.Common;
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

		Job CurrentJob { get; }
		AreaColorAndCalcSettings CurrentAreaColorAndCalcSettings { get; }
		MapAreaInfo PosterAreaInfo { get; }
		SizeInt PosterSize { get; }

		ColorBandSet ColorBandSet { get; }
		ColorBandSet? PreviewColorBandSet { get; set; }

		VectorInt DisplayPosition { get; set; }
		double DisplayZoom { get; set; }


		//void UpdateMapView(Poster poster, RectangleInt? newArea);

		void UpdateMapSpecs(TransformType transformType, RectangleInt newArea);
		void UpdateMapSpecs(Poster currentPoster, MapAreaInfo newMapAreaInfo);
		MapAreaInfo GetUpdatedMapAreaInfo(MapAreaInfo mapAreaInfo, RectangleDbl screenArea, SizeDbl newMapSize);

		void UpdateColorBandSet(ColorBandSet colorBandSet);

		bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster);
		bool PosterOpen(string name);
		void Load(Poster poster, MapAreaInfo? newMapAreaInfo);
		
		void PosterSave();
		void PosterSaveAs(string name, string? description);
		void Close();
	}
}
