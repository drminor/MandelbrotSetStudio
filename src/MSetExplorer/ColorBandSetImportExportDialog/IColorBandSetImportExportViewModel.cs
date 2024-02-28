using MongoDB.Bson;
using MSS.Types;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace MSetExplorer
{
	public interface IColorBandSetImportExportViewModel : INotifyPropertyChanged
	{
		DialogType DialogType { get; }

		ObservableCollection<ColorBandSetInfo> ColorBandSetInfos { get; }
		ColorBandSetInfo? SelectedColorBandSetInfo { get; set; }

		string? SelectedName { get; set; }
		string? SelectedDescription { get; set; }
		bool UserIsSettingTheName { get; set; }

		bool ExportColorBandSet(ColorBandSet colorBandSet);

		bool TryImportColorBandSet(ObjectId colorBandSetId, [MaybeNullWhen(false)] out ColorBandSet colorBandSet);

		bool IsNameTaken(string name);
	}

}