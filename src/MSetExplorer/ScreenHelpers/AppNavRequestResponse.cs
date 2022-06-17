using System;

namespace MSetExplorer.ScreenHelpers
{
	public class AppNavRequestResponse
	{
		public AppNavRequestResponse(OnCloseBehavior onCloseBehavior, RequestResponseCommand requestCommand, string[]? requestParameters)
		{
			OnCloseBehavior = onCloseBehavior;
			RequestCommand = requestCommand;
			RequestParameters = requestParameters;
			ResponseCommand = null;
			ResponseParameters = null;
		}

		public OnCloseBehavior OnCloseBehavior { get; set; }

		public RequestResponseCommand RequestCommand { get; init; }
		public string[]? RequestParameters { get; init; }

		public RequestResponseCommand? ResponseCommand { get; set; }
		public string[]? ResponseParameters { get; set; }


		public AppNavRequestResponse BuildRequestFromResponse()
		{
			if (ResponseCommand.HasValue)
			{
				var result = new AppNavRequestResponse(OnCloseBehavior, ResponseCommand.Value, ResponseParameters);
				return result;
			}
			else
			{
				throw new InvalidOperationException("Cannot create a Request, the ResponseCommand is null.");
			}
		}

		public static AppNavRequestResponse BuildEmptyRequest(OnCloseBehavior? onCloseBehavior = null)
		{
			if (onCloseBehavior == null)
			{
				var showTopNav = Properties.Settings.Default.ShowTopNav;
				onCloseBehavior = showTopNav ? OnCloseBehavior.ReturnToTopNav : OnCloseBehavior.Close;
			}

			return new AppNavRequestResponse(onCloseBehavior ?? OnCloseBehavior.ReturnToTopNav, RequestResponseCommand.None, null);
		}
	}

	public enum RequestResponseCommand
	{
		OpenPoster,
		OpenJob,
		None
	}

	public enum OnCloseBehavior
	{
		ReturnToTopNav,
		Close
	}

	public interface IHaveAppNavRequestResponse
	{
		public AppNavRequestResponse AppNavRequestResponse { get; }
	}

}
