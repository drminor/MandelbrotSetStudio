﻿using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Animation;

namespace MSetExplorer
{
	public class StoryboardDetails
	{
		private Action<Action<int>, int>? _completionCallback;
		private Action<int>? _onAnimationComplete;
		private int _callbackIndex;

		public StoryboardDetails(Storyboard storyboard, FrameworkElement containingObject)
		{
			OurNameScope = GetOrCreateNameScope(containingObject);

			Storyboard = storyboard;
			ContainingObject = containingObject;
			RateFactor = 1.0;

			Storyboard.Completed += Storyboard_Completed;
		}

		#region Public Properties

		public INameScope OurNameScope { get; init; }

		public Storyboard Storyboard { get; init; }
		public FrameworkElement ContainingObject { get; init; }

		public double RateFactor { get; set; }

		#endregion

		#region Public Methods

		public int AddChangePosition(string objectName, string propertyPath, Rect from, Point newPosition, TimeSpan duration, TimeSpan beginTime)
		{
			//_width = _cutoff - _xPosition;
			var to = new Rect(newPosition, from.Size);
			var da = new RectAnimation(from, to, duration);


			//var dp = (PropertyPath)new PropertyPathConverter().ConvertFromString("(FrameworkElement.LayoutTransform).(ScaleTransform.ScaleX)"));

			return AddTimeline(objectName, propertyPath, da, beginTime);
		}

		// Move the x1 value and adjust the width to keep x2 the same
		public int AddChangeLeft(string objectName, string propertyPath, Rect from, double newX1, TimeSpan duration, TimeSpan beginTime)
		{
			//_width = _cutoff - _xPosition;
			var to = new Rect(newX1, from.Y, from.Right - newX1, from.Height);
			var da = new RectAnimation(from, to, duration);

			return AddTimeline(objectName, propertyPath, da, beginTime);
		}

		// Update the Width and Right to keep X1 constant.
		public int AddChangeWidth(string objectName, string propertyPath, Rect from, double newWidth, TimeSpan duration, TimeSpan beginTime)
		{
			//_cutoff = _xPosition + _width;
			var to = new Rect(from.X, from.Y, newWidth, from.Height);
			var da = new RectAnimation(from, to, duration);

			return AddTimeline(objectName, propertyPath, da, beginTime);
		}

		// Inflate / Deflate
		public int AddChangeSize(string objectName, string propertyPath, Rect from, Size newSize, TimeSpan duration, TimeSpan beginTime)
		{
			var to = new Rect(from.X + newSize.Width / 2, from.Y + newSize.Height / 2, newSize.Width, newSize.Height);
			var da = new RectAnimation(from, to, duration);

			return AddTimeline(objectName, propertyPath, da, beginTime);
		}

		public int AddMakeTransparent(string objectName, TimeSpan duration, TimeSpan beginTime)
		{
			var da = new DoubleAnimation(1, 0, duration);
			da.EasingFunction = new PowerEase { Power = 3, EasingMode = EasingMode.EaseOut };

			return AddTimeline(objectName, "Opacity", da, beginTime);
		}

		public int AddMakeOpaque(string objectName, TimeSpan duration, TimeSpan beginTime)
		{
			var da = new DoubleAnimation(0, 1, duration);
			da.EasingFunction = new PowerEase { Power = 3, EasingMode = EasingMode.EaseOut };

			return AddTimeline(objectName, "Opacity", da, beginTime);
		}

		public int AddDoubleAnimation(string objectName, string propertyPath, double from, double to, TimeSpan duration, TimeSpan beginTime)
		{
			var da = new DoubleAnimation(from, to, duration);
			return AddTimeline(objectName, propertyPath, da, beginTime);
		}

		public int AddOpacityAnimation(string objectName, string propertyPath, double from, double to, TimeSpan duration, TimeSpan beginTime)
		{
			var da = new DoubleAnimation(from, to, duration);
			da.EasingFunction = new PowerEase { Power = 3, EasingMode = EasingMode.EaseOut };

			return AddTimeline(objectName, propertyPath, da, beginTime);
		}

		public int AddTimeline(string objectName, string propertyPath, AnimationTimeline animationTimeline, TimeSpan beginTime)
		{
			animationTimeline.Duration = animationTimeline.Duration.TimeSpan.Multiply(RateFactor);
			animationTimeline.BeginTime = beginTime.Multiply(RateFactor);

			Storyboard.SetTargetName(animationTimeline, objectName);
			Storyboard.SetTargetProperty(animationTimeline, new PropertyPath(propertyPath));

			Storyboard.Children.Add(animationTimeline);
			return Storyboard.Children.Count;
		}

		public void Begin(Action<Action<int>, int> completionCallback, Action<int> onAnimationComplete, int index)
		{
			_completionCallback = completionCallback;
			_onAnimationComplete = onAnimationComplete;
			_callbackIndex = index;
			Storyboard.Begin(ContainingObject);
		}

		#endregion

		#region Private Methods

		private void Storyboard_Completed(object? sender, EventArgs e)
		{
			Storyboard.Children.Clear();
			RateFactor = 1.0;

			if (_onAnimationComplete == null)
			{
				throw new InvalidOperationException("The provided onAnimationComplete method is null as the StoryBoard_Completed callback is called.");
			}

			if (_completionCallback != null)
			{
				_completionCallback(_onAnimationComplete, _callbackIndex);
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
