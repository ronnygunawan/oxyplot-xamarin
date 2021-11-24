// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotView.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a view that can show a <see cref="PlotModel" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

namespace OxyPlot.Xamarin.Android {
	/// <summary>
	/// Represents a view that can show a <see cref="PlotModel" />.
	/// </summary>
	public class PlotView : View, IPlotView {
		/// <summary>
		/// The factor that scales from OxyPlot´s device independent pixels (96 dpi) to
		/// Android´s current density-independent pixels (dpi).
		/// </summary>
		/// <remarks>See <a href="http://developer.android.com/guide/practices/screens_support.html">Supporting multiple screens.</a>.</remarks>
		public double Scale;

		/// <summary>
		/// The rendering lock object.
		/// </summary>
		private readonly object _renderingLock = new();

		/// <summary>
		/// The invalidation lock object.
		/// </summary>
		private readonly object _invalidateLock = new();

		/// <summary>
		/// The touch points of the previous touch event.
		/// </summary>
		private ScreenPoint[]? _previousTouchPoints;

		/// <summary>
		/// The current model.
		/// </summary>
		private PlotModel? _model;

		/// <summary>
		/// The default controller
		/// </summary>
		private IPlotController? _defaultController;

		/// <summary>
		/// The current render context.
		/// </summary>
		private CanvasRenderContext? _rc;

		/// <summary>
		/// The model invalidated flag.
		/// </summary>
		private bool _isModelInvalidated;

		/// <summary>
		/// The update data flag.
		/// </summary>
		private bool _updateDataFlag = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="PlotView" /> class.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <remarks>Use this constructor when creating the view from code.</remarks>
		public PlotView(Context context) :
			base(context) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PlotView" /> class.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="attrs">The attribute set.</param>
		/// <remarks>This constructor is called when inflating the view from XML.</remarks>
		public PlotView(Context context, IAttributeSet attrs) :
			base(context, attrs) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PlotView" /> class.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="attrs">The attribute set.</param>
		/// <param name="defStyle">The definition style.</param>
		/// <remarks>This constructor performs inflation from XML and applies a class-specific base style.</remarks>
		public PlotView(Context context, IAttributeSet attrs, int defStyle) :
			base(context, attrs, defStyle) {
		}

		/// <summary>
		/// Gets or sets the plot model.
		/// </summary>
		/// <value>The model.</value>
		public PlotModel? Model {
			get {
				return _model;
			}

			set {
				if (_model != value) {
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
		}

		/// <summary>
		/// Gets or sets the plot controller.
		/// </summary>
		/// <value>The controller.</value>
		public IPlotController? Controller { get; set; }

		/// <summary>
		/// Gets the actual model in the view.
		/// </summary>
		/// <value>
		/// The actual model.
		/// </value>
		Model? IView.ActualModel {
			get {
				return Model;
			}
		}

		/// <summary>
		/// Gets the actual <see cref="PlotModel" /> of the control.
		/// </summary>
		public PlotModel? ActualModel {
			get {
				return Model;
			}
		}

		/// <summary>
		/// Gets the actual controller.
		/// </summary>
		/// <value>
		/// The actual <see cref="IController" />.
		/// </value>
		IController IView.ActualController {
			get {
				return ActualController;
			}
		}

		/// <summary>
		/// Gets the coordinates of the client area of the view.
		/// </summary>
		public OxyRect ClientArea {
			get {
				return new OxyRect(0, 0, Width, Height);
			}
		}

		/// <summary>
		/// Gets the actual <see cref="IPlotController" /> of the control.
		/// </summary>
		/// <value>The actual plot controller.</value>
		public IPlotController ActualController {
			get {
				return Controller ?? (_defaultController ?? (_defaultController = new PlotController()));
			}
		}

		/// <summary>
		/// Hides the tracker.
		/// </summary>
		public void HideTracker() {
		}

		/// <summary>
		/// Hides the zoom rectangle.
		/// </summary>
		public void HideZoomRectangle() {
		}

		/// <summary>
		/// Invalidates the plot (not blocking the UI thread)
		/// </summary>
		/// <param name="updateData">if set to <c>true</c>, all data bindings will be updated.</param>
		public void InvalidatePlot(bool updateData = true) {
			lock (_invalidateLock) {
				_isModelInvalidated = true;
				_updateDataFlag = _updateDataFlag || updateData;
			}

			Invalidate();
		}

		/// <summary>
		/// Sets the cursor type.
		/// </summary>
		/// <param name="cursorType">The cursor type.</param>
		public void SetCursorType(CursorType cursorType) {
		}

		/// <summary>
		/// Shows the tracker.
		/// </summary>
		/// <param name="trackerHitResult">The tracker data.</param>
		public void ShowTracker(TrackerHitResult trackerHitResult) {
		}

		/// <summary>
		/// Shows the zoom rectangle.
		/// </summary>
		/// <param name="rectangle">The rectangle.</param>
		public void ShowZoomRectangle(OxyRect rectangle) {
		}

		/// <summary>
		/// Stores text on the clipboard.
		/// </summary>
		/// <param name="text">The text.</param>
		public void SetClipboardText(string text) {
		}

		/// <summary>
		/// Handles key down events.
		/// </summary>
		/// <param name="keyCode">The key code.</param>
		/// <param name="e">The event arguments.</param>
		/// <returns><c>true</c> if the event was handled.</returns>
		public override bool OnKeyDown(Keycode keyCode, KeyEvent? e) {
			bool handled = base.OnKeyDown(keyCode, e);
			if (!handled) {
				handled = ActualController.HandleKeyDown(this, e?.ToKeyEventArgs());
			}

			return handled;
		}

		/// <summary>
		/// Handles touch screen motion events.
		/// </summary>
		/// <param name="e">The motion event arguments.</param>
		/// <returns><c>true</c> if the event was handled.</returns>
		public override bool OnTouchEvent(MotionEvent? e) {
			bool handled = base.OnTouchEvent(e);
			if (!handled) {
				switch (e?.Action) {
					case MotionEventActions.Down:
						handled = OnTouchDownEvent(e);
						break;
					case MotionEventActions.Move:
						handled = OnTouchMoveEvent(e);
						break;
					case MotionEventActions.Up:
						handled = OnTouchUpEvent(e);
						break;
				}
			}

			return handled;
		}

		/// <summary>
		/// Draws the content of the control.
		/// </summary>
		/// <param name="canvas">The canvas to draw on.</param>
		protected override void OnDraw(Canvas? canvas) {
			base.OnDraw(canvas);
			PlotModel? actualModel = ActualModel;
			if (actualModel == null) {
				return;
			}

			if (actualModel.Background.IsVisible()) {
				canvas?.DrawColor(actualModel.Background.ToColor());
			} else {
				// do nothing
			}

			lock (_invalidateLock) {
				if (_isModelInvalidated) {
					((IPlotModel)actualModel).Update(_updateDataFlag);
					_updateDataFlag = false;
					_isModelInvalidated = false;
				}
			}

			lock (_renderingLock) {
				if (_rc == null) {
					DisplayMetrics? displayMetrics = Context?.Resources?.DisplayMetrics;

					// The factors for scaling to Android's DPI and SPI units.
					// The density independent pixel is equivalent to one physical pixel
					// on a 160 dpi screen (baseline density)
					if (displayMetrics != null) {
						Scale = displayMetrics.Density;
						_rc = new CanvasRenderContext(Scale, displayMetrics.ScaledDensity);
					}
				}

				if (canvas != null) {
					_rc?.SetTarget(canvas);
				}

				if (_rc != null) {
					((IPlotModel)actualModel).Render(_rc, new OxyRect(0, 0, Width / Scale, Height / Scale));
				}
			}
		}

		/// <summary>
		/// Handles touch down events.
		/// </summary>
		/// <param name="e">The motion event arguments.</param>
		/// <returns><c>true</c> if the event was handled.</returns>
		private bool OnTouchDownEvent(MotionEvent e) {
			OxyTouchEventArgs args = e.ToTouchEventArgs(Scale);
			bool handled = ActualController.HandleTouchStarted(this, args);
			_previousTouchPoints = e.GetTouchPoints(Scale);
			return handled;
		}

		/// <summary>
		/// Handles touch move events.
		/// </summary>
		/// <param name="e">The motion event arguments.</param>
		/// <returns><c>true</c> if the event was handled.</returns>
		private bool OnTouchMoveEvent(MotionEvent e) {
			ScreenPoint[] currentTouchPoints = e.GetTouchPoints(Scale);
			OxyTouchEventArgs args = new(currentTouchPoints, _previousTouchPoints);
			bool handled = ActualController.HandleTouchDelta(this, args);
			_previousTouchPoints = currentTouchPoints;
			return handled;
		}

		/// <summary>
		/// Handles touch released events.
		/// </summary>
		/// <param name="e">The motion event arguments.</param>
		/// <returns><c>true</c> if the event was handled.</returns>
		private bool OnTouchUpEvent(MotionEvent e) {
			return ActualController.HandleTouchCompleted(this, e.ToTouchEventArgs(Scale));
		}

		protected override void Dispose(bool disposing) {
			base.Dispose(disposing);
			_rc?.Dispose();
		}
	}
}
