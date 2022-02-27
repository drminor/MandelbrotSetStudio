using MSS.Types;
using MSS.Types.Screen;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;

namespace MSetExplorer
{
	internal class MapSectionCollectionBinder : IMapSectionCollectionBinder
	{
		private readonly IScreenSectionCollection _screenSectionCollection;
		private Image _mapDisplayImage => _screenSectionCollection.MapDisplayImage;

		#region Constructor

		public MapSectionCollectionBinder(Canvas canvas, SizeInt blockSize, ObservableCollection<MapSection> mapSections)
		{
			var canvasSize = new SizeDbl(canvas.Width, canvas.Height).Round();
			_screenSectionCollection = new ScreenSectionCollection(canvasSize, blockSize);
			_ = canvas.Children.Add(_screenSectionCollection.MapDisplayImage);

			CanvasOffset = new VectorInt();

			mapSections.CollectionChanged += MapSections_CollectionChanged;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// The position of the canvas' origin relative to the Image Block Data
		/// </summary>
		public VectorInt CanvasOffset
		{
			get
			{
				var pointDbl = new PointDbl(
					(double)_mapDisplayImage.GetValue(Canvas.LeftProperty),
					(double)_mapDisplayImage.GetValue(Canvas.BottomProperty)
					);

				return new VectorInt(pointDbl.Round()).Invert();
			}

			set
			{
				var curVal = CanvasOffset;
				if (value != curVal)
				{
					Debug.WriteLine($"CanvasOffset is being set to {value}.");
					var offset = value.Invert();
					_mapDisplayImage.SetValue(Canvas.LeftProperty, (double)offset.X);
					_mapDisplayImage.SetValue(Canvas.BottomProperty, (double)offset.Y);
				}
			}
		}

		#endregion

		#region Event Handlers

		private void MapSections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
			{
				//	Reset
				_screenSectionCollection.HideScreenSections();
			}
			else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				// Adding new items
				foreach (var mapSection in GetList(e.NewItems))
				{
					//Debug.WriteLine($"About to draw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");
					_screenSectionCollection.Draw(mapSection);
				}
			}
		}

		private IEnumerable<MapSection> GetList(IList lst)
		{
			return lst?.Cast<MapSection>() ?? new List<MapSection>();
		}

		#endregion

	}
}
