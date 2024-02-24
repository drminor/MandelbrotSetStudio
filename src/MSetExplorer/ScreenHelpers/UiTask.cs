using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MSetExplorer.ScreenHelpers
{
	[AsyncMethodBuilder(typeof(UiTaskMethodBuilder))]
	public class UiTask
	{
		internal TaskCompletionSource<object> Promise { get; } = new TaskCompletionSource<object>();

		public Task AsTask() => Promise.Task;

		public TaskAwaiter<object> GetAwaiter()
		{
			return Promise.Task.GetAwaiter();
		}

		public static implicit operator Task(UiTask task) => task.AsTask();
	}

	/*
	  
	Now we can go back to our UpdateControls method and remove the boilerplate code.
	Just by setting the return type to UiTask,
	we indicate our desire to run this method in the UI thread.
	If the method is invoked from a non-UI thread, the context will automatically change:
	  
	private void UpdateControls()
	{
	    if (!Dispatcher.CheckAccess())
		{
			// We're not in the UI thread, ask the dispatcher to call this same method in the UI thread, then exit
			Dispatcher.BeginInvoke(new Action(UpdateControls));
			return;
		}

		// We're in the UI thread, update the controls
		TextTime.Text = DateTime.Now.ToLongTimeString();
	}  

	private async UiTask UpdateControls()
	{
		TextTime.Text = DateTime.Now.ToLongTimeString();
	}


	*/

}
