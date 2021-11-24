using Android.Content;
using OxyPlot.Xamarin.Forms;
using OxyPlot.Xamarin.Forms.Platform.Android;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

// Exports the renderer.
[assembly: ExportRenderer(typeof(PlotView), typeof(PlotViewRenderer))]

namespace OxyPlot.Xamarin.Forms.Platform.Android {
	using System.ComponentModel;
	using System.Diagnostics.CodeAnalysis;
	using OxyPlot.Xamarin.Android;

	/// <summary>
	/// Provides a custom <see cref="Xamarin.Forms.PlotView" /> renderer for Xamarin.Android.
	/// </summary>
	public class PlotViewRenderer : ViewRenderer<Xamarin.Forms.PlotView, PlotView> {
		/// <summary>
		/// Initializes static members of the <see cref="PlotViewRenderer"/> class.
		/// </summary>
		static PlotViewRenderer() {
			Init();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PlotViewRenderer"/> class.
		/// </summary>
		public PlotViewRenderer(Context context) : base(context) {
		}

		/// <summary>
		/// Initializes the renderer.
		/// </summary>
		/// <remarks>This method must be called before a <see cref="T:PlotView" /> is used.</remarks>
		public static void Init() {
			OxyPlot.Xamarin.Forms.PlotView.IsRendererInitialized = true;
		}

		/// <summary>
		/// Raises the element changed event.
		/// </summary>
		/// <param name="e">The event arguments.</param>
		[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
			Justification = "Assigned to a view holder")]
		protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.PlotView> e) {
			base.OnElementChanged(e);
			if (e.OldElement != null || Element == null) {
				return;
			}

			DetachModelFromView();

			if (Context == null) {
				return;
			}

			PlotView plotView = new(Context) {Model = Element.Model, Controller = Element.Controller};

			plotView.SetBackgroundColor(Element.BackgroundColor.ToAndroid());

			SetNativeControl(plotView);
		}

		/// <summary>
		/// Raises the element property changed event.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The event arguments.</param>
		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e) {
			base.OnElementPropertyChanged(sender, e);
			if (Element == null || Control == null) {
				return;
			}

			if (e.PropertyName == Xamarin.Forms.PlotView.ModelProperty.PropertyName) {
				DetachModelFromView();
				Control.Model = Element.Model;
			}

			if (e.PropertyName == Xamarin.Forms.PlotView.ControllerProperty.PropertyName) {
				Control.Controller = Element.Controller;
			}

			if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName) {
				Control.SetBackgroundColor(Element.BackgroundColor.ToAndroid());
			}
		}

		void DetachModelFromView() {
			IPlotModel model = Element.Model;
			model.AttachPlotView(null);
		}
	}
}
