using ImageBuilder;
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

		public void CreateImage(string imageFilePath, Poster poster)
		{
			ImageFilePath = imageFilePath;
			Poster = poster;

			_task = Task.Run(() => _pngBuilder.BuildAsync(imageFilePath, poster.MapAreaInfo, poster.ColorBandSet, poster.MapCalcSettings, StatusCallBack, _cancellationTokenSource.Token), _cancellationTokenSource.Token);

			//_task.ContinueWith(t =>
			//{

			//}
			//);
		}

		public void CancelCreateImage()
		{
			_cancellationTokenSource.Cancel();

			if (_task != null)
			{
				_task.Wait();
			}

			// TODO: Schedule this or yield to allow the file to become free.
			if (ImageFilePath != null && File.Exists(ImageFilePath))
			{
				Thread.Sleep(10000);
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
