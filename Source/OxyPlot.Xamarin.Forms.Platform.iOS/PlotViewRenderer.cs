using OxyPlot.Xamarin.Forms;
using OxyPlot.Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

// Exports the renderer.
[assembly: ExportRenderer(typeof(PlotView), typeof(PlotViewRenderer))]
namespace OxyPlot.Xamarin.Forms.Platform.iOS {
	using System.ComponentModel;
	using OxyPlot.Xamarin.iOS;

	/// <summary>
	/// Provides a custom <see cref="OxyPlot.Xamarin.Forms.PlotView" /> renderer for Xamarin.iOS.
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
		// ReSharper disable once EmptyConstructor
		public PlotViewRenderer() {
			// Do not delete
		}

		/// <summary>
		/// Initializes the renderer.
		/// </summary>
		/// <remarks>This method must be called before a <see cref="T:PlotView" /> is used.</remarks>
		public static new void Init() {
			OxyPlot.Xamarin.Forms.PlotView.IsRendererInitialized = true;
		}

		/// <summary>
		/// Raises the element changed event.
		/// </summary>
		/// <param name="e">The event arguments.</param>
		protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.PlotView> e) {
			base.OnElementChanged(e);
			if (e.OldElement != null || Element == null) {
				return;
			}

#pragma warning disable CA2000
			PlotView plotView = new() {
				Model = Element.Model,
				Controller = Element.Controller,
				BackgroundColor = Element.BackgroundColor.ToOxyColor().ToUIColor()
			};
#pragma warning restore CA2000

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
				Control.Model = Element.Model;
			}

			if (e.PropertyName == Xamarin.Forms.PlotView.ControllerProperty.PropertyName) {
				Control.Controller = Element.Controller;
			}

			if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName) {
				Control.BackgroundColor = Element.BackgroundColor.ToOxyColor().ToUIColor();
			}

			if (e.PropertyName == VisualElement.IsVisibleProperty.PropertyName) {
				Control.InvalidatePlot(false);
			}
		}
	}
}
