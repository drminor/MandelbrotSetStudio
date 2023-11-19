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
		private readonly IImageBuilder _pngBuilder;
		private readonly MapJobHelper _mapJobHelper;
		private CancellationTokenSource _cts;
		private Task<bool>?_task;

		#region Constructor

		public CreateImageProgressViewModel(IImageBuilder pngBuilder, MapJobHelper mapJobHelper)
		{
			Successfull = false;
			_pngBuilder = pngBuilder;
			_mapJobHelper = mapJobHelper;
			_cts = new CancellationTokenSource();
			_task = null;

			Progress = new Progress<double>();
		}

		#endregion

		#region Public Properties

		public bool Successfull { get; private set; }
		public Progress<double> Progress { get; init; }

		public string? ImageFilePath { get; private set; }
		public Poster? Poster { get; private set; }

		public long NumberOfCountValSwitches => _pngBuilder.NumberOfCountValSwitches;

		#endregion

		#region Public Methods

		// TODO: CreateImageViewModel. If the task fails, we need to alert the user.

		public void CreateImage(string imageFilePath, ObjectId jobId, OwnerType ownerType, MapCenterAndDelta mapAreaInfoV2, SizeDbl canvasSize, ColorBandSet colorBandSet, bool useEscapeVelocities, MapCalcSettings mapCalcSettings)
		{
			ImageFilePath = imageFilePath;

			var mapAreaInfoWithSize = _mapJobHelper.GetMapPositionSizeAndDelta(mapAreaInfoV2, canvasSize);

			_task = Task.Run(() => _pngBuilder.BuildAsync(imageFilePath, jobId, ownerType, mapAreaInfoWithSize, colorBandSet, useEscapeVelocities, mapCalcSettings, StatusCallback, _cts.Token), _cts.Token);
		}

		public void CancelCreateImage()
		{
			_cts.Cancel();

			if (_task != null)
			{
				_task.Wait();
			}

			if (ImageFilePath != null && File.Exists(ImageFilePath))
			{
				Thread.Sleep(10 * 1000);
				File.Delete(ImageFilePath);
			}

			Successfull = false;
		}

		public void WaitForImageToComplete()
		{
			if (_task != null)
			{
				_task.Wait();
				Successfull = _task.Result;
			}
		}

		#endregion

		private void StatusCallback(double value)
		{
			((IProgress<double>)Progress).Report(value);
		}

	}
}
