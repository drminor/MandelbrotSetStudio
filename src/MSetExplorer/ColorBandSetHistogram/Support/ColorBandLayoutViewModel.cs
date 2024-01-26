using MSS.Types;
using System;
using System.Windows.Controls;

namespace MSetExplorer
{
	public class ColorBandLayoutViewModel : ViewModelBase, ICloneable
	{
		#region Private Fields

		private SizeDbl _contentScale;

		private bool _parentIsFocused;

		#endregion

		#region Constructor

		public ColorBandLayoutViewModel(Canvas canvas, SizeDbl contentScale, bool parentIsFocused, IsSelectedChangedCallback isSelectedChangedCallback, Action<int, ColorBandSetEditMode> requestContextMenuShown)
		{
			Canvas = canvas;

			if (contentScale.IsNAN() || contentScale.Width == 0)
			{
				_contentScale = new SizeDbl(1);
			}
			else
			{
				_contentScale = new SizeDbl(contentScale.Width, 1);
			}

			_parentIsFocused = parentIsFocused;
			IsSelectedChangedCallback = isSelectedChangedCallback;
			RequestContextMenuShown = requestContextMenuShown;
		}

		#endregion

		#region Public Properties

		public Canvas Canvas { get; init; }

		public SizeDbl ContentScale
		{
			get => _contentScale;

			set
			{
				if (value.Width != _contentScale.Width)
				{
					_contentScale = new SizeDbl(value.Width, 1);
					OnPropertyChanged();
				}
			}
		}

		public bool ParentIsFocused
		{
			get => _parentIsFocused;
			set
			{
				if (value != _parentIsFocused)
				{
					_parentIsFocused = value;
					OnPropertyChanged();
				}
			}
		}

		public IsSelectedChangedCallback IsSelectedChangedCallback { get; init; }
		public Action<int, ColorBandSetEditMode> RequestContextMenuShown { get; init; }

		#endregion

		#region Public Methods

		object ICloneable.Clone() => Clone();

		public ColorBandLayoutViewModel Clone()
		{
			var result = new ColorBandLayoutViewModel(Canvas, ContentScale, ParentIsFocused, IsSelectedChangedCallback, RequestContextMenuShown);
			return result;
		}

		#endregion
	}
}
