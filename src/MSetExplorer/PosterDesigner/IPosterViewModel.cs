using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace MSetExplorer
{
	public interface IPosterViewModel
	{
		bool InDesignMode { get; }
		event PropertyChangedEventHandler? PropertyChanged;

		//SizeDbl CanvasSize { get; set; }
		//SizeDbl LogicalDisplaySize { get; set; }

		Poster? CurrentPoster { get; }
		string? CurrentPosterName { get; }
		bool CurrentPosterOnFile { get; }
		bool CurrentPosterIsDirty { get; }
		int GetGetNumberOfDirtyJobs();

		SizeInt PosterSize { get; }

		Job CurrentJob { get; }
		AreaColorAndCalcSettings CurrentAreaColorAndCalcSettings { get; }
		MapAreaInfo2 PosterAreaInfo { get; }

		ColorBandSet CurrentColorBandSet { get; set; }
		ColorBandSet? PreviewColorBandSet { get; set; }

		VectorInt DisplayPosition { get; set; }
		double DisplayZoom { get; set; }

		void UpdateMapSpecs(Poster currentPoster, MapAreaInfo2 newMapAreaInfo);

		void UpdateMapSpecs(TransformType transformType, VectorInt panAmount, double factor, MapAreaInfo2? diagnosticAreaInfo);

		//MapAreaInfo2 GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, RectangleDbl screenArea, SizeDbl newMapSize);
		MapAreaInfo2 GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, SizeInt posterSize, VectorInt offsetFromCenter, RectangleDbl screenArea, SizeDbl newMapSize);

		bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster);
		bool PosterOpen(string name);
		void Load(Poster poster, MapAreaInfo2? newMapAreaInfo);
		
		bool PosterSave();
		bool PosterSaveAs(string name, string? description, [MaybeNullWhen(true)] out string errorText);

		void Close();
	}
}
