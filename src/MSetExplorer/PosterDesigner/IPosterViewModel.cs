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

		SizeDbl PosterSize { get; set; }

		Job CurrentJob { get; }
		AreaColorAndCalcSettings CurrentAreaColorAndCalcSettings { get; }
		MapAreaInfo2 PosterAreaInfo { get; }

		ColorBandSet CurrentColorBandSet { get; set; }
		ColorBandSet? PreviewColorBandSet { get; set; }

		VectorDbl DisplayPosition { get; set; }
		double DisplayZoom { get; set; }

		MapAreaInfo2 GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, SizeDbl currentPosterSize, SizeDbl newPosterSize, RectangleDbl screenArea, out double diagReciprocal);
		MapAreaInfo2 GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, TransformType transformType, VectorInt panAmount, double factor, out double diagReciprocal);

		void UpdateMapSpecs(MapAreaInfo2 newMapAreaInfo/*, SizeDbl posterSize*/);

		//void UpdateMapSpecs(TransformType transformType, VectorInt panAmount, double factor, MapAreaInfo2? diagnosticAreaInfo, out double diagReciprocal);

		bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster);
		void Load(Poster poster, MapAreaInfo2? newMapAreaInfo);

		bool PosterOpen(string name);
		bool PosterSave();
		bool PosterSaveAs(string name, string? description, [MaybeNullWhen(true)] out string errorText);
		void PosterClose();

		long DeleteMapSectionsForUnsavedJobs();
		long DeleteNonEssentialMapSections(Job job, SizeDbl posterSize);

		//List<ObjectId> GetAllNonCurrentJobIds();
		//List<ObjectId> GetAllJobIdsNotMatchingCurrentSPD();
	}
}
