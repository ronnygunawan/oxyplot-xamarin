#nullable enable
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PanZoomGestureRecognizer.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Recognizes drag/pinch multi-touch gestures and translates them into pan/zoom information.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#if __UNIFIED__
namespace OxyPlot.Xamarin.iOS
#else
namespace OxyPlot.MonoTouch
#endif
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

#if __UNIFIED__
	using Foundation;
	using UIKit;

#else
    using global::MonoTouch.Foundation;
    using global::MonoTouch.UIKit;
#endif

	/// <summary>
	/// Recognizes drag/pinch multi-touch gestures and translates them into pan/zoom information.
	/// </summary>
	public class PanZoomGestureRecognizer : UIGestureRecognizer {
		/// <summary>
		/// Up to 2 touches being currently tracked in a pan/zoom.
		/// </summary>
		private readonly List<UITouch> _activeTouches = new();

		/// <summary>
		/// Distance between touch points when the second touch point begins. Used to determine
		/// whether the touch points cross along a given axis during the zoom gesture.
		/// </summary>
		private ScreenVector _startingDistance;

		/// <summary>
		/// Initializes a new instance of the <see cref="PanZoomGestureRecognizer"/> class.
		/// </summary>
		/// <remarks>
		/// To add methods that will be invoked upon recognition, you can use the AddTarget method.
		/// </remarks>
		public PanZoomGestureRecognizer() {
			ZoomThreshold = 20d;
			AllowPinchPastZero = true;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="PanZoomGestureRecognizer"/> keeps the aspect ratio when pinching.
		/// </summary>
		/// <value><c>true</c> if keep aspect ratio when pinching; otherwise, <c>false</c>.</value>
		public bool KeepAspectRatioWhenPinching { get; set; }

		/// <summary>
		/// Gets or sets how far apart touch points must be on a certain axis to enable scaling that axis.
		/// (only applies if KeepAspectRatioWhenPinching is <c>false</c>)
		/// </summary>
		public double ZoomThreshold { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether a zoom-out gesture can turn into a zoom-in gesture if the fingers cross.
		/// If <c>true</c>, and <see cref="KeepAspectRatioWhenPinching" /> is <c>false</c>, a zoom-out gesture
		/// can turn into a zoom-in gesture if the fingers cross. Setting to <c>false</c> will
		/// instead simply stop the zoom at that point.
		/// </summary>
		public bool AllowPinchPastZero { get; set; }

		/// <summary>
		/// Gets or sets the current calculated pan/zoom changes.
		/// </summary>
		/// <value>
		/// The touch event arguments.
		/// </value>
		public OxyTouchEventArgs? TouchEventArgs { get; set; }

		/// <summary>
		/// Called when a touch gesture begins.
		/// </summary>
		/// <param name="touches">The touches.</param>
		/// <param name="evt">The event arguments.</param>
		public override void TouchesBegan(
			NSSet touches,
			UIEvent evt
		) {
			base.TouchesBegan(touches, evt);

			if (_activeTouches.Count >= 2) {
				// we already have two touches
				return;
			}

			// Grab 1-2 touches to track
			UITouch[] newTouches = touches.ToArray<UITouch>();
			bool firstTouch = _activeTouches.Count == 0;

			_activeTouches.AddRange(newTouches.Take(2 - _activeTouches.Count));

			if (firstTouch) {
				// HandleTouchStarted initializes the entire multitouch gesture,
				// with the first touch used for panning.
				TouchEventArgs = _activeTouches[0].ToTouchEventArgs(View);
			}

			CalculateStartingDistance();
		}

		/// <summary>
		/// Called when a touch gesture is moving.
		/// </summary>
		/// <param name="touches">The touches.</param>
		/// <param name="evt">The event arguments.</param>
		public override void TouchesMoved(
			NSSet touches,
			UIEvent evt
		) {
			base.TouchesMoved(touches, evt);

			if (!_activeTouches.Any(touch => touch.Phase == UITouchPhase.Moved)) return;

			// get current and previous location of the first touch point
			UITouch t1 = _activeTouches[0];
			ScreenPoint l1 = t1.LocationInView(View).ToScreenPoint();
			ScreenPoint pl1 = t1.Phase == UITouchPhase.Moved
				? t1.PreviousLocationInView(View).ToScreenPoint()
				: l1;

			ScreenPoint l = l1;
			ScreenVector t = l1 - pl1;
			ScreenVector s = new(1, 1);

			if (_activeTouches.Count > 1) {
				// get current and previous location of the second touch point
				UITouch t2 = _activeTouches[1];
				ScreenPoint l2 = t2.LocationInView(View).ToScreenPoint();
				ScreenPoint pl2 = t2.Phase == UITouchPhase.Moved
					? t2.PreviousLocationInView(View).ToScreenPoint()
					: l2;

				ScreenVector d = l1 - l2;
				ScreenVector pd = pl1 - pl2;

				if (!KeepAspectRatioWhenPinching) {
					if (!AllowPinchPastZero) {
						// Don't allow fingers crossing in a zoom-out gesture to turn it back into a zoom-in gesture
						d = PreventCross(d);
					}

					double scaleX = CalculateScaleFactor(d.X, pd.X);
					double scaleY = CalculateScaleFactor(d.Y, pd.Y);
					s = new ScreenVector(scaleX, scaleY);
				} else {
					double scale = pd.Length > 0
						? d.Length / pd.Length
						: 1;
					s = new ScreenVector(scale, scale);
				}
			}

			OxyTouchEventArgs e = new() {
				Position = l,
				DeltaTranslation = t,
				DeltaScale = s
			};
			TouchEventArgs = e;
			State = UIGestureRecognizerState.Changed;
		}

		/// <summary>
		/// Called when a touch gesture ends.
		/// </summary>
		/// <param name="touches">The touches.</param>
		/// <param name="evt">The event arguments.</param>
		public override void TouchesEnded(
			NSSet touches,
			UIEvent evt
		) {
			base.TouchesEnded(touches, evt);

			// We already have the only two touches we care about, so ignore the params
			UITouch? secondTouch = _activeTouches.ElementAtOrDefault(1);

			if (secondTouch is {
				Phase: UITouchPhase.Ended
			}) {
				_activeTouches.Remove(secondTouch);
			}

			UITouch? firstTouch = _activeTouches.FirstOrDefault();

			if (firstTouch is not {
				Phase: UITouchPhase.Ended
			}) {
				return;
			}

			_activeTouches.Remove(firstTouch);

			if (_activeTouches.Count > 0) return;

			TouchEventArgs = firstTouch.ToTouchEventArgs(View);

			State = State == UIGestureRecognizerState.Possible
				? UIGestureRecognizerState.Failed
				: UIGestureRecognizerState.Ended;
		}

		/// <summary>
		/// Called when a touch gesture is cancelled.
		/// </summary>
		/// <param name="touches">The touches.</param>
		/// <param name="evt">The event arguments.</param>
		public override void TouchesCancelled(
			NSSet touches,
			UIEvent evt
		) {
			base.TouchesCancelled(touches, evt);

			// I'm not sure if it's actually possible for one touch to be canceled without
			// both being canceled, but just to be safe let's stay consistent with TouchesEnded
			// and handle that scenario.

			// We already have the only two touches we care about, so ignore the params
			UITouch? secondTouch = _activeTouches.ElementAtOrDefault(1);

			if (secondTouch is {
				Phase: UITouchPhase.Cancelled
			}) {
				_activeTouches.Remove(secondTouch);
			}

			UITouch? firstTouch = _activeTouches.FirstOrDefault();

			if (firstTouch is not {
				Phase: UITouchPhase.Cancelled
			}) {
				return;
			}

			_activeTouches.Remove(firstTouch);

			if (_activeTouches.Count > 0) return;

			TouchEventArgs = firstTouch.ToTouchEventArgs(View);

			State = State == UIGestureRecognizerState.Possible
				? UIGestureRecognizerState.Failed
				: UIGestureRecognizerState.Cancelled;
		}

		/// <summary>
		/// Determines whether the direction has changed.
		/// </summary>
		/// <param name="current">The current value.</param>
		/// <param name="original">The original value.</param>
		/// <returns><c>true</c> if the direction changed.</returns>
		private static bool DidDirectionChange(
			double current,
			double original
		) {
			return (current >= 0) != (original >= 0);
		}

		/// <summary>
		/// Calculates the scale factor.
		/// </summary>
		/// <param name="distance">The distance.</param>
		/// <param name="previousDistance">The previous distance.</param>
		/// <returns>The scale factor.</returns>
		private double CalculateScaleFactor(
			double distance,
			double previousDistance
		) {
			return Math.Abs(previousDistance) > ZoomThreshold
				&& Math.Abs(distance) > ZoomThreshold
					? Math.Abs(distance / previousDistance)
					: 1;
		}

		/// <summary>
		/// Calculates the starting distance.
		/// </summary>
		private void CalculateStartingDistance() {
			if (_activeTouches.Count < 2) {
				_startingDistance = default(ScreenVector);
				return;
			}

			ScreenPoint loc1 = _activeTouches[0].LocationInView(View).ToScreenPoint();
			ScreenPoint loc2 = _activeTouches[1].LocationInView(View).ToScreenPoint();

			_startingDistance = loc1 - loc2;
		}

		/// <summary>
		/// Applies the "prevent fingers crossing" to the specified vector.
		/// </summary>
		/// <param name="currentDistance">The current distance.</param>
		/// <returns>A vector where the "prevent fingers crossing" is applied.</returns>
		private ScreenVector PreventCross(
			ScreenVector currentDistance
		) {
			double x = currentDistance.X;
			double y = currentDistance.Y;

			if (DidDirectionChange(x, _startingDistance.X)) {
				x = 0;
			}

			if (DidDirectionChange(y, _startingDistance.Y)) {
				y = 0;
			}

			return new ScreenVector(x, y);
		}
	}
}
