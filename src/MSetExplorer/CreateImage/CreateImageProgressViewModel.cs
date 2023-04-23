using ImageBuilder;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MSetExplorer
{
	public class CreateImageProgressViewModel
	{
		private readonly PngBuilder _pngBuilder;
		private CancellationTokenSource _cancellationTokenSource;
		private Task<bool>?_task;

		#region Constructor

		public CreateImageProgressViewModel(PngBuilder pngBuilder)
		{
			_pngBuilder = pngBuilder;
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

		public void CreateImage(string imageFilePath, AreaColorAndCalcSettings areaColorAndCalcSettings, SizeInt imageSize)
		{
			ImageFilePath = imageFilePath;

			var oldAreaInfo = MapJobHelper.GetMapAreaWithSizeLean(areaColorAndCalcSettings.MapAreaInfo, imageSize);

			_task = Task.Run(() => _pngBuilder.BuildAsync(imageFilePath, oldAreaInfo, areaColorAndCalcSettings.ColorBandSet, areaColorAndCalcSettings.MapCalcSettings, StatusCallBack, _cancellationTokenSource.Token), _cancellationTokenSource.Token);

			//_task.ContinueWith(t =>
			//{

			//}
			//);
		}

		//public void CreateImage(string imageFilePath, Project project)
		//{
		//	ImageFilePath = imageFilePath;
		//	//Poster = poster;

		//	var curJob = project.CurrentJob;
		//	//var oldAreaInfo = MapJobHelper2.Convert(curJob.MapAreaInfo, new MSS.Types.SizeInt(1024));
		//	var oldAreaInfo = new MapAreaInfo();


		//	_task = Task.Run(() => _pngBuilder.BuildAsync(imageFilePath, oldAreaInfo, project.CurrentColorBandSet, curJob.MapCalcSettings, _useEscapeVelocities, StatusCallBack, _cancellationTokenSource.Token), _cancellationTokenSource.Token);

		//	//_task.ContinueWith(t =>
		//	//{

		//	//}
		//	//);
		//}


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
