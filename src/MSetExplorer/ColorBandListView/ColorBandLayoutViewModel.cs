using MSS.Types;
using System;
using System.Windows.Controls;

namespace MSetExplorer
{
	public class ColorBandLayoutViewModel : ViewModelBase, ICloneable
	{
		public const int BASE_ZINDEX = 10;
		public const int BACKGROUND_BASE_ZINDEX = 1;

		public const int DRAG_LINE_ZINDEX_DELTA = 25;
		public const int TOP_ARROW_AREA_ZINDEX_DELTA = 25;

		public const int COLOR_BLOCK_ZINDEX_DELTA = 5;
		public const int START_COLOR_ZINDEX_DELTA = 5;
		public const int END_COLOR_ZINDEX_DELTA = 5;

		public const int BLENDED_AREA_ZINDEX_DELTA = 7;

		public const int IS_CURRENT_AREA_ZINDEX_DELTA = 1;

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
