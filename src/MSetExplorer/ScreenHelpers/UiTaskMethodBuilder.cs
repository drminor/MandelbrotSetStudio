using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MSetExplorer.ScreenHelpers
{
	public class UiTaskMethodBuilder
	{
		private readonly Dispatcher _dispatcher;

		public UiTaskMethodBuilder(Dispatcher dispatcher)
		{
			_dispatcher = dispatcher;
		}

		public void Start<TStateMachine>(ref TStateMachine stateMachine)
			where TStateMachine : IAsyncStateMachine
		{
			if (!_dispatcher.CheckAccess())
			{
				_dispatcher.BeginInvoke(new Action(stateMachine.MoveNext));
			}
			else
			{
				stateMachine.MoveNext();
			}
		}

		public static UiTaskMethodBuilder Create()
		{
			return new UiTaskMethodBuilder(Application.Current.Dispatcher);
		}

		public void SetStateMachine(IAsyncStateMachine stateMachine)
		{
		}

		public void SetResult()
		{
			SetResult(new object());
		}

		public void SetResult(object result)
		{
			Task.Promise.SetResult(result);
		}

		public void SetException(Exception exception)
		{
			Task.Promise.SetException(exception);
		}

		public UiTask Task { get; } = new UiTask();

		public void AwaitOnCompleted<TAwaiter, TStateMachine>(
			ref TAwaiter awaiter,
			ref TStateMachine stateMachine)
			where TAwaiter : INotifyCompletion
			where TStateMachine : IAsyncStateMachine
		{
			awaiter.OnCompleted(ResumeAfterAwait(stateMachine));
		}

		public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
			ref TAwaiter awaiter,
			ref TStateMachine stateMachine)
			where TAwaiter : ICriticalNotifyCompletion
			where TStateMachine : IAsyncStateMachine
		{
			awaiter.UnsafeOnCompleted(ResumeAfterAwait(stateMachine));
		}

		private Action ResumeAfterAwait<TStateMachine>(TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
		{
			return () =>
			{
				if (!_dispatcher.CheckAccess())
				{
					_dispatcher.BeginInvoke(new Action(stateMachine.MoveNext));
				}
				else
				{
					stateMachine.MoveNext();
				}
			};
		}
	}
}
