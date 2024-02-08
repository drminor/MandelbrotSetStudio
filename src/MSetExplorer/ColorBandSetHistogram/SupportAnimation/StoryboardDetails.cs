using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MSetExplorer
{
	public class StoryboardDetails
	{
		private const int WAIT_DURATION = 100;
		private DebounceDispatcher _waitDispatcher;

		private bool _debounce;

		private Action<int>? _completionCallback;
		private int _callbackIndex;

		public StoryboardDetails(Storyboard storyboard, FrameworkElement containingObject)
		{
			_waitDispatcher = new DebounceDispatcher()
			{
				Priority = DispatcherPriority.Render
			};

			OurNameScope = GetOrCreateNameScope(containingObject);

			Storyboard = storyboard;
			ContainingObject = containingObject;
			RateFactor = 1.0;

			Storyboard.Completed += Storyboard_Completed;
			_debounce = false;
		}

		#region Public Properties

		public INameScope OurNameScope { get; init; }

		public Storyboard Storyboard { get; init; }
		public FrameworkElement ContainingObject { get; init; }

		public double RateFactor { get; set; }

		#endregion

		#region Public Methods

		public void UnregisterName(string name)
		{
			ContainingObject.UnregisterName(name);
		}

		public int AddRectAnimation(string objectName, string propertyPath, Rect from, Rect to, TimeSpan beginTime, TimeSpan duration)
		{
			var da = new RectAnimation(from, to, duration);
			return AddTimeline(objectName, propertyPath, da, beginTime);
		}

		public int AddChangePosition(string objectName, string propertyPath, Rect from, Point newPosition, TimeSpan beginTime, TimeSpan duration)
		{
			var to = new Rect(newPosition, from.Size);
			var da = new RectAnimation(from, to, duration);

			return AddTimeline(objectName, propertyPath, da, beginTime);
		}

		public int AddChangeLeft(string objectName, string propertyPath, Rect from, double newX1, TimeSpan beginTime, TimeSpan duration)
		{
			var diff = newX1 - from.X;

			var to = new Rect(newX1, from.Y, from.Width - diff, from.Height);
			var da = new RectAnimation(from, to, duration);

			return AddTimeline(objectName, propertyPath, da, beginTime);
		}

		public int AddChangeWidth(string objectName, string propertyPath, Rect from, double newWidth, TimeSpan beginTime, TimeSpan duration)
		{
			var to = new Rect(from.X, from.Y, newWidth, from.Height);
			var da = new RectAnimation(from, to, duration);

			return AddTimeline(objectName, propertyPath, da, beginTime);
		}

		// Inflate / Deflate
		public int AddChangeSize(string objectName, string propertyPath, Rect from, Size newSize, TimeSpan beginTime, TimeSpan duration)
		{
			Rect to;

			if (from.Size.Width > newSize.Width)
			{
				// Deflating
				to = new Rect(from.X + newSize.Width / 2, from.Y + newSize.Height / 2, newSize.Width, newSize.Height);
			}
			else
			{
				// Inflating
				to = new Rect(from.X - newSize.Width / 2, from.Y - newSize.Height / 2, newSize.Width, newSize.Height);
			}

			var da = new RectAnimation(from, to, duration);

			return AddTimeline(objectName, propertyPath, da, beginTime);
		}

		public int AddMakeTransparent(string objectName, TimeSpan beginTime, TimeSpan duration)
		{
			var da = new DoubleAnimation(1, 0, duration);
			da.EasingFunction = new PowerEase { Power = 3, EasingMode = EasingMode.EaseOut };

			return AddTimeline(objectName, "Opacity", da, beginTime);
		}

		public int AddMakeOpaque(string objectName, TimeSpan beginTime, TimeSpan duration)
		{
			var da = new DoubleAnimation(0, 1, duration);
			da.EasingFunction = new PowerEase { Power = 3, EasingMode = EasingMode.EaseOut };

			return AddTimeline(objectName, "Opacity", da, beginTime);
		}

		public int AddDoubleAnimation(string objectName, string propertyPath, double from, double to, TimeSpan beginTime, TimeSpan duration)
		{
			var da = new DoubleAnimation(from, to, duration);
			return AddTimeline(objectName, propertyPath, da, beginTime);
		}

		public int AddOpacityAnimation(string objectName, string propertyPath, double from, double to, TimeSpan beginTime, TimeSpan duration)
		{
			var da = new DoubleAnimation(from, to, duration);
			da.EasingFunction = new PowerEase { Power = 3, EasingMode = EasingMode.EaseOut };

			return AddTimeline(objectName, propertyPath, da, beginTime);
		}

		public int AddTimeline(string objectName, string propertyPath, AnimationTimeline animationTimeline, TimeSpan beginTime)
		{
			animationTimeline.Duration = animationTimeline.Duration.TimeSpan.Multiply(RateFactor);
			animationTimeline.BeginTime = beginTime.Multiply(RateFactor);

			//animationTimeline.FillBehavior = FillBehavior.Stop;

			Storyboard.SetTargetName(animationTimeline, objectName);
			Storyboard.SetTargetProperty(animationTimeline, new PropertyPath(propertyPath));

			Storyboard.Children.Add(animationTimeline);
			return Storyboard.Children.Count;
		}

		public void Begin(Action<int> completionCallback, int index, bool debounce)
		{
			_completionCallback = completionCallback;
			_callbackIndex = index;
			Storyboard.Begin(ContainingObject);
			_debounce = debounce;
		}

		#endregion

		#region Private Methods

		private void Storyboard_Completed(object? sender, EventArgs e)
		{
			if (_debounce)
			{
				_waitDispatcher.Debounce(
					interval: WAIT_DURATION,
					action: parm =>
					{
						AfterDebounce_Storyboard_Completed();
					},
					param: null);
			}
			else
			{
				AfterDebounce_Storyboard_Completed();
			}
		}

		private void AfterDebounce_Storyboard_Completed()
		{
			Storyboard.Children.Clear();
			RateFactor = 1.0;

			if (_completionCallback != null)
			{
				_completionCallback(_callbackIndex);
			}
		}

		private INameScope GetOrCreateNameScope(FrameworkElement containingObject)
		{
			var ns = NameScope.GetNameScope(containingObject);

			if (ns == null)
			{
				ns = new NameScope();
				NameScope.SetNameScope(containingObject, ns);
			}

			return ns;
		}

		#endregion
	}
}
