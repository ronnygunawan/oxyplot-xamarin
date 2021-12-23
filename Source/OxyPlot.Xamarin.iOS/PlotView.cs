#nullable enable
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotView.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides a view that can show a <see cref="PlotModel" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using CoreGraphics;

namespace OxyPlot.Xamarin.iOS {
	using Foundation;
	using OxyPlot;
	using UIKit;

	/// <summary>
	/// Provides a view that can show a <see cref="PlotModel" />.
	/// </summary>
	[Register("PlotView")]
	public class PlotView : UIView, IPlotView {
		/// <summary>
		/// The current plot model.
		/// </summary>
		private PlotModel? _model;

		/// <summary>
		/// The default plot controller.
		/// </summary>
		private IPlotController? _defaultController;

		/// <summary>
		/// The pan zoom gesture recognizer
		/// </summary>
		private readonly PanZoomGestureRecognizer _panZoomGesture = new();

		/// <summary>
		/// The tap gesture recognizer
		/// </summary>
		private readonly UITapGestureRecognizer _tapGesture = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="OxyPlot.Xamarin.iOS.PlotView"/> class.
		/// </summary>
		public PlotView() {
			Initialize();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OxyPlot.Xamarin.iOS.PlotView"/> class.
		/// </summary>
		/// <param name="frame">The initial frame.</param>
		public PlotView(
			CGRect frame
		) : base(frame) {
			Initialize();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OxyPlot.Xamarin.iOS.PlotView"/> class.
		/// </summary>
		/// <param name="coder">Coder.</param>
		[Export("initWithCoder:")]
		public PlotView(
			NSCoder coder
		) : base(coder) {
			Initialize();
		}

		/// <summary>
		/// Uses the new layout.
		/// </summary>
		/// <returns><c>true</c>, if new layout was used, <c>false</c> otherwise.</returns>
		[Export("requiresConstraintBasedLayout")]
		// ReSharper disable once UnusedMember.Local
		private bool UseNewLayout() {
			return true;
		}

		/// <summary>
		/// Initialize the view.
		/// </summary>
		private void Initialize() {
			UserInteractionEnabled = true;
			MultipleTouchEnabled = true;
			BackgroundColor = UIColor.White;
			KeepAspectRatioWhenPinching = true;

			_panZoomGesture.AddTarget(HandlePanZoomGesture);
			_tapGesture.AddTarget(HandleTapGesture);
			//Prevent panZoom and tap gestures from being recognized simultaneously
			_tapGesture.RequireGestureRecognizerToFail(_panZoomGesture);

			// Do not intercept touches on overlapping views
			_panZoomGesture.ShouldReceiveTouch += (
				_,
				touch
			) => ReferenceEquals(touch.View, this);
			_tapGesture.ShouldReceiveTouch += (
				_,
				touch
			) => ReferenceEquals(touch.View, this);
		}

		/// <summary>
		/// Gets or sets the <see cref="PlotModel"/> to show in the view.
		/// </summary>
		/// <value>The <see cref="PlotModel"/>.</value>
		public PlotModel? Model {
			get => _model;

			set {
				if (_model == value) return;

				if (_model != null) {
					((IPlotModel)_model).AttachPlotView(null);
					_model = null;
				}

				if (value != null) {
					((IPlotModel)value).AttachPlotView(this);
					_model = value;
				}

				InvalidatePlot();
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="IPlotController"/> that handles input events.
		/// </summary>
		/// <value>The <see cref="IPlotController"/>.</value>
		public IPlotController? Controller { get; set; }

		/// <summary>
		/// Gets the actual model in the view.
		/// </summary>
		/// <value>
		/// The actual model.
		/// </value>
		Model? IView.ActualModel => Model;

		/// <summary>
		/// Gets the actual <see cref="PlotModel"/> to show.
		/// </summary>
		/// <value>The actual model.</value>
		public PlotModel? ActualModel => Model;

		/// <summary>
		/// Gets the actual controller.
		/// </summary>
		/// <value>
		/// The actual <see cref="IController" />.
		/// </value>
		IController IView.ActualController => ActualController;

		/// <summary>
		/// Gets the coordinates of the client area of the view.
		/// </summary>
		public OxyRect ClientArea =>
			// TODO
			new(0, 0, 100, 100);

		/// <summary>
		/// Gets the actual <see cref="IPlotController"/>.
		/// </summary>
		/// <value>The actual plot controller.</value>
		public IPlotController ActualController => Controller ?? (_defaultController ??= new PlotController());

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="OxyPlot.Xamarin.iOS.PlotView"/> keeps the aspect ratio when pinching.
		/// </summary>
		/// <value><c>true</c> if keep aspect ratio when pinching; otherwise, <c>false</c>.</value>
		public bool KeepAspectRatioWhenPinching {
			get => _panZoomGesture.KeepAspectRatioWhenPinching;
			set => _panZoomGesture.KeepAspectRatioWhenPinching = value;
		}

		/// <summary>
		/// How far apart touch points must be on a certain axis to enable scaling that axis.
		/// (only applies if KeepAspectRatioWhenPinching == false)
		/// </summary>
		public double ZoomThreshold {
			get => _panZoomGesture.ZoomThreshold;
			set => _panZoomGesture.ZoomThreshold = value;
		}

		/// <summary>
		/// If <c>true</c>, and KeepAspectRatioWhenPinching is <c>false</c>, a zoom-out gesture
		/// can turn into a zoom-in gesture if the fingers cross. Setting to <c>false</c> will
		/// instead simply stop the zoom at that point.
		/// </summary>
		public bool AllowPinchPastZero {
			get => _panZoomGesture.AllowPinchPastZero;
			set => _panZoomGesture.AllowPinchPastZero = value;
		}

		/// <summary>
		/// Hides the tracker.
		/// </summary>
		public void HideTracker() { }

		/// <summary>
		/// Hides the zoom rectangle.
		/// </summary>
		public void HideZoomRectangle() { }

		/// <summary>
		/// Invalidates the plot (not blocking the UI thread)
		/// </summary>
		/// <param name="updateData">If set to <c>true</c> update data.</param>
		public void InvalidatePlot(
			bool updateData = true
		) {
			PlotModel? actualModel = _model;
			// TODO: update the model on a background thread
			(actualModel as IPlotModel)?.Update(updateData);

			SetNeedsDisplay();
		}

		/// <summary>
		/// Sets the cursor type.
		/// </summary>
		/// <param name="cursorType">The cursor type.</param>
		public void SetCursorType(
			CursorType cursorType
		) {
			// No cursor on iOS
		}

		/// <summary>
		/// Shows the tracker.
		/// </summary>
		/// <param name="trackerHitResult">The tracker data.</param>
		public void ShowTracker(
			TrackerHitResult trackerHitResult
		) {
			// TODO: how to show a tracker on iOS
			// the tracker must be moved away from the finger...
		}

		/// <summary>
		/// Shows the zoom rectangle.
		/// </summary>
		/// <param name="rectangle">The rectangle.</param>
		public void ShowZoomRectangle(
			OxyRect rectangle
		) {
			// Not needed - better with pinch events on iOS?
		}

		/// <summary>
		/// Stores text on the clipboard.
		/// </summary>
		/// <param name="text">The text.</param>
		public void SetClipboardText(
			string text
		) {
#pragma warning disable CA2000
			UIPasteboard.General.SetValue(new NSString(text), "public.utf8-plain-text");
#pragma warning restore CA2000
		}

		/// <summary>
		/// Draws the content of the view.
		/// </summary>
		/// <param name="rect">The rectangle to draw.</param>
		public override void Draw(
			CGRect rect
		) {
			if (_model is not IPlotModel actualModel) return;

			CGContext context = UIGraphics.GetCurrentContext();
			using CoreGraphicsRenderContext renderer = new(context);
			if (actualModel.Background.IsVisible()) {
				context.SaveState();
				using CGColor fillColor = actualModel.Background.ToCGColor();
				context.SetFillColor(fillColor);
				context.FillRect(rect);
				context.RestoreState();
			}

			actualModel.Render(renderer, new OxyRect(0, 0, rect.Width, rect.Height));
		}

		/// <summary>
		/// Method invoked when a motion (a shake) has started.
		/// </summary>
		/// <param name="motion">The motion subtype.</param>
		/// <param name="evt">The event arguments.</param>
		public override void MotionBegan(
			UIEventSubtype motion,
			UIEvent? evt
		) {
			base.MotionBegan(motion, evt);
			if (motion == UIEventSubtype.MotionShake) {
				ActualController.HandleGesture(this, new OxyShakeGesture(), new OxyKeyEventArgs());
			}
		}

		/// <summary>
		/// Used to add/remove the gesture recognizer so that it
		/// doesn't prevent the PlotView from being garbage-collected.
		/// </summary>
		/// <param name="newsuper">New superview</param>
		public override void WillMoveToSuperview(
			UIView? newsuper
		) {
			if (newsuper == null) {
				RemoveGestureRecognizer(_panZoomGesture);
				RemoveGestureRecognizer(_tapGesture);
			}
			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			else if (Superview == null) {
				AddGestureRecognizer(_panZoomGesture);
				AddGestureRecognizer(_tapGesture);
			}

			base.WillMoveToSuperview(newsuper);
		}

		private void HandlePanZoomGesture() {
			switch (_panZoomGesture.State) {
				case UIGestureRecognizerState.Began:
					ActualController.HandleTouchStarted(this, _panZoomGesture.TouchEventArgs);
					break;
				case UIGestureRecognizerState.Changed:
					ActualController.HandleTouchDelta(this, _panZoomGesture.TouchEventArgs);
					break;
				case UIGestureRecognizerState.Ended:
				case UIGestureRecognizerState.Cancelled:
					ActualController.HandleTouchCompleted(this, _panZoomGesture.TouchEventArgs);
					break;
			}
		}

		private void HandleTapGesture() {
			CGPoint location = _tapGesture.LocationInView(this);
			ActualController.HandleTouchStarted(this, location.ToTouchEventArgs());
			ActualController.HandleTouchCompleted(this, location.ToTouchEventArgs());
		}
	}
}
