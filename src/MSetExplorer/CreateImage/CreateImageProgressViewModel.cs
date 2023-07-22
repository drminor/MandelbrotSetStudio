using ImageBuilder;
using MongoDB.Bson;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MSetExplorer
{
	public class CreateImageProgressViewModel
	{
		private readonly PngBuilder _pngBuilder;
		private readonly MapJobHelper _mapJobHelper;
		private CancellationTokenSource _cancellationTokenSource;
		private Task<bool>?_task;

		#region Constructor

		public CreateImageProgressViewModel(PngBuilder pngBuilder, MapJobHelper mapJobHelper)
		{
			_pngBuilder = pngBuilder;
			_mapJobHelper = mapJobHelper;
			_cancellationTokenSource = new CancellationTokenSource();
			_task = null;

			Progress = new Progress<double>();
		}

		#endregion

		#region Public Properties

		public Progress<double> Progress { get; init; }

		public string? ImageFilePath { get; private set; }
		public Poster? Poster { get; private set; }

		public long NumberOfCountValSwitches => _pngBuilder.NumberOfCountValSwitches;

		#endregion

		#region Public Methods

		//public void CreateImage(string imageFilePath, ObjectId jobId, OwnerType ownerType, MapAreaInfo mapAreaInfoWithSize, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings)
		//{
		//	ImageFilePath = imageFilePath;

		//	_task = Task.Run(() => _pngBuilder.BuildAsync(imageFilePath, jobId, ownerType, mapAreaInfoWithSize, colorBandSet, mapCalcSettings, StatusCallBack, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
		//}

		public void CreateImage(string imageFilePath, ObjectId jobId, OwnerType ownerType, MapAreaInfo2 mapAreaInfoV2, SizeDbl canvasSize, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings)
		{
			ImageFilePath = imageFilePath;

			var mapAreaInfoWithSize = _mapJobHelper.GetMapAreaWithSize(mapAreaInfoV2, canvasSize);

			_task = Task.Run(() => _pngBuilder.BuildAsync(imageFilePath, jobId, ownerType, mapAreaInfoWithSize, colorBandSet, mapCalcSettings, StatusCallBack, _cancellationTokenSource.Token), _cancellationTokenSource.Token);

		}

		public void CancelCreateImage()
		{
			_cancellationTokenSource.Cancel();

			if (_task != null)
			{
				_task.Wait();
			}

			if (ImageFilePath != null && File.Exists(ImageFilePath))
			{
				Thread.Sleep(10 * 1000);
				File.Delete(ImageFilePath);
			}
		}

		public void WaitForImageToComplete()
		{
			if (_task != null)
			{
				_task.Wait();
			}
		}

		#endregion

		private void StatusCallBack(double value)
		{
			((IProgress<double>)Progress).Report(value);
		}

	}
}
