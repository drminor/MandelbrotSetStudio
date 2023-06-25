using MongoDB.Bson;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System.Collections.Generic;
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

		SizeInt PosterSize { get; }

		Job CurrentJob { get; }
		AreaColorAndCalcSettings CurrentAreaColorAndCalcSettings { get; }
		MapAreaInfo2 PosterAreaInfo { get; }

		ColorBandSet CurrentColorBandSet { get; set; }
		ColorBandSet? PreviewColorBandSet { get; set; }

		VectorDbl DisplayPosition { get; set; }
		double DisplayZoom { get; set; }

		void UpdateMapSpecs(MapAreaInfo2 newMapAreaInfo, SizeDbl posterSize);

		void UpdateMapSpecs(TransformType transformType, VectorInt panAmount, double factor, MapAreaInfo2? diagnosticAreaInfo, out double diagReciprocal);

		MapAreaInfo2 GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, SizeDbl currentPosterSize, SizeDbl newPosterSize, RectangleDbl screenArea, out double diagReciprocal);

		bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster);
		void Load(Poster poster, MapAreaInfo2? newMapAreaInfo);

		bool PosterOpen(string name);
		bool PosterSave();
		bool PosterSaveAs(string name, string? description, [MaybeNullWhen(true)] out string errorText);
		long PosterClose();

		long DeleteMapSectionsForUnsavedJobs();
		long DeleteMapSections(List<MapSectionRequest> mapSectionRequests);

		List<ObjectId> GetAllNonCurrentJobIds();
		List<ObjectId> GetAllJobIdsNotMatchingCurrentSPD();
	}
}
