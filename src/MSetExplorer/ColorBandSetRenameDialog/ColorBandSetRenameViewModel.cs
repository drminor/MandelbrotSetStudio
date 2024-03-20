using MSS.Common;
using MSS.Types;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace MSetExplorer
{
	public class ColorBandSetRenameViewModel : INotifyPropertyChanged
	{
		private readonly IProjectAdapter _projectAdapter;

		private readonly IList<ColorBandSetInfo> _colorBandSetInfos;


		private string? _selectedNameSource;
		private string? _selectedNameNew;

		#region Constructor

		public ColorBandSetRenameViewModel(IProjectAdapter projectAdapter, int targetIterations, IEnumerable<ColorBandSetInfo> cbsInfos, ColorBandSetInfo cbsInfoSource, ColorBandSetInfo cbsInfoNew)
		{
			_projectAdapter = projectAdapter;
			TargetIterations = targetIterations;

			_colorBandSetInfos = new List<ColorBandSetInfo>(cbsInfos);

			CbsInfoSource = cbsInfoSource;
			CbsInfoNew = cbsInfoNew;

			_selectedNameSource = cbsInfoSource.Name;
			_selectedNameNew = cbsInfoNew.Name;
		}


		#endregion

		#region Public Methods 

		//public bool SaveColorBandSet(ColorBandSet colorBandSet)
		//{
		//	_projectAdapter.InsertColorBandSet(colorBandSet);
		//	return true;
		//}

		//public bool TryOpenColorBandSet(ObjectId colorBandSetId, [MaybeNullWhen(false)] out ColorBandSet colorBandSet)
		//{
		//	var result = _projectAdapter.TryGetColorBandSet(colorBandSetId, out colorBandSet);
		//	return result;
		//}

		#endregion

		#region Public Properties

		public int TargetIterations { get; init; }

		public ColorBandSetInfo CbsInfoSource { get; init; }
		public ColorBandSetInfo CbsInfoNew { get; init; }

		public string? SelectedNameSource
		{
			get => _selectedNameSource;
			set
			{
				if (value != _selectedNameSource)
				{
					_selectedNameSource = value;
					OnPropertyChanged();
				}
			}
		}

		public string? SelectedNameNew
		{
			get => _selectedNameNew;
			set
			{
				if (value != _selectedNameNew)
				{
					_selectedNameNew = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsNameTaken(string name)
		{
			var result = _colorBandSetInfos.Any(x => x.Name == name && x.MaxIterations == TargetIterations);
			return result;
		}

		public bool AreNamesOk()
		{
			return true;
		}

		#endregion

		#region INotifyPropertyChanged Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
