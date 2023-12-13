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

		Poster? CurrentPoster { get; }
		string? CurrentPosterName { get; }
		bool CurrentPosterOnFile { get; }
		bool CurrentPosterIsDirty { get; }
		int GetGetNumberOfDirtyJobs();

		SizeDbl PosterSize { get; /*set;*/ }

		Job CurrentJob { get; }
		AreaColorAndCalcSettings CurrentAreaColorAndCalcSettings { get; }
		MapCenterAndDelta PosterAreaInfo { get; }

		ColorBandSet CurrentColorBandSet { get; set; }
		ColorBandSet? PreviewColorBandSet { get; set; }
		
		bool ColorBandSetIsPreview { get; }
		
		bool SaveTheZValues { get; set; }
		bool CalculateEscapeVelocities { get; set; }

		VectorDbl DisplayPosition { get; set; }
		double DisplayZoom { get; set; }

		MapCenterAndDelta GetUpdatedMapAreaInfo(MapCenterAndDelta mapAreaInfo, SizeDbl currentPosterSize, SizeDbl newPosterSize, RectangleDbl screenArea, out double diagReciprocal);
		MapCenterAndDelta GetUpdatedMapAreaInfo(MapCenterAndDelta mapAreaInfo, TransformType transformType, VectorInt panAmount, double factor, out double diagReciprocal);

		void AddNewCoordinateUpdateJob(MapCenterAndDelta newMapAreaInfo, SizeDbl posterSize);

		bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster);

		void PosterAddNewJobAndLoad(Poster poster, MapCenterAndDelta? newMapAreaInfo, SizeDbl posterSize);
		void PosterLoad(Poster poster);

		bool PosterOpen(string name);
		bool PosterSave();
		bool PosterSaveAs(string name, string? description, [MaybeNullWhen(true)] out string errorText);
		void PosterClose();

		long DeleteMapSectionsForUnsavedJobs();

	}
}
