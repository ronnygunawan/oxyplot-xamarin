// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CanvasRenderContext.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides a render context for Android.Graphics.Canvas.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;

namespace OxyPlot.Xamarin.Android {
	/// <summary>
	/// Provides a render context for Android.Graphics.Canvas.
	/// </summary>
	public class CanvasRenderContext : RenderContextBase, IDisposable {
		/// <summary>
		/// The images in use
		/// </summary>
		private readonly HashSet<OxyImage> _imagesInUse = new();

		/// <summary>
		/// The image cache
		/// </summary>
		private readonly Dictionary<OxyImage, Bitmap> _imageCache = new();

		/// <summary>
		/// The current paint.
		/// </summary>
		private readonly Paint _paint;

		/// <summary>
		/// A reusable path object.
		/// </summary>
		private readonly Path _path;

		/// <summary>
		/// A reusable bounds rectangle.
		/// </summary>
		private readonly Rect _bounds;

		/// <summary>
		/// A reusable list of points.
		/// </summary>
		private readonly List<float> _pts;

		/// <summary>
		/// The canvas to draw on.
		/// </summary>
		private Canvas? _canvas;

		private bool _disposedValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="CanvasRenderContext" /> class.
		/// </summary>
		/// <param name="scale">The scale.</param>
		/// <param name="fontScale">The scale to use for text and font.</param>
		public CanvasRenderContext(double scale, double fontScale) {
			_paint = new Paint();
			_path = new Path();
			_bounds = new Rect();
			_pts = new List<float>();
			Scale = scale;
			FontScale = fontScale;
		}

		/// <summary>
		/// Gets the factor for Android´s density-independent pixels (160 dpi as baseline density).
		/// </summary>
		/// <remarks>See <a href="http://developer.android.com/guide/practices/screens_support.html">Supporting multiple screens.</a>.</remarks>
		public double Scale { get; }

		/// <summary>
		/// Gets the factor for Android's scale-independent pixels (160 dpi as baseline density).
		/// </summary>
		public double FontScale { get; }

		public override int ClipCount => _canvas?.SaveCount ?? 0;

		/// <summary>
		/// Sets the target.
		/// </summary>
		/// <param name="c">The canvas.</param>
		public void SetTarget(Canvas c) {
			_canvas = c;
		}

		/// <summary>
		/// Draws an ellipse.
		/// </summary>
		/// <param name="rect">The rectangle.</param>
		/// <param name="fill">The fill color.</param>
		/// <param name="stroke">The stroke color.</param>
		/// <param name="thickness">The thickness.</param>
		/// <param name="edgeRenderingMode"></param>
		public override void DrawEllipse(OxyRect rect, OxyColor fill, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode) {
			_paint.Reset();
			{
				if (fill.IsVisible()) {
					SetFill(fill);
					using RectF rectF = Convert(rect);
					_canvas?.DrawOval(rectF, _paint);
				}

				if (stroke.IsVisible()) {
					SetStroke(stroke, thickness);
					using RectF rectF = Convert(rect);
					_canvas?.DrawOval(rectF, _paint);
				}
			}
		}

		/// <summary>
		/// Draws the collection of ellipses, where all have the same stroke and fill.
		/// This performs better than calling DrawEllipse multiple times.
		/// </summary>
		/// <param name="rectangles">The rectangles.</param>
		/// <param name="fill">The fill color.</param>
		/// <param name="stroke">The stroke color.</param>
		/// <param name="thickness">The stroke thickness.</param>
		/// <param name="edgeRenderingMode"></param>
		public override void DrawEllipses(IList<OxyRect> rectangles, OxyColor fill, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode) {
			_paint.Reset();
			{
				foreach (OxyRect rect in rectangles) {
					if (fill.IsVisible()) {
						SetFill(fill);
						using RectF rectF = Convert(rect);
						_canvas?.DrawOval(rectF, _paint);
					}

					if (stroke.IsVisible()) {
						SetStroke(stroke, thickness);
						using RectF rectF = Convert(rect);
						_canvas?.DrawOval(rectF, _paint);
					}
				}
			}
		}

		/// <summary>
		/// Draws a polyline.
		/// </summary>
		/// <param name="points">The points.</param>
		/// <param name="stroke">The stroke color.</param>
		/// <param name="thickness">The stroke thickness.</param>
		/// <param name="edgeRenderingMode"></param>
		/// <param name="dashArray">The dash array.</param>
		/// <param name="lineJoin">The line join type.</param>
		public override void DrawLine(IList<ScreenPoint> points, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode, double[] dashArray, LineJoin lineJoin) {
			_paint.Reset();
			{
				_path.Reset();
				{
					SetPath(points, edgeRenderingMode == EdgeRenderingMode.PreferGeometricAccuracy);
					SetStroke(stroke, thickness, dashArray, lineJoin, edgeRenderingMode == EdgeRenderingMode.PreferGeometricAccuracy);
					_canvas?.DrawPath(_path, _paint);
				}
			}
		}

		/// <summary>
		/// Draws multiple line segments defined by points (0,1) (2,3) (4,5) etc.
		/// This should have better performance than calling DrawLine for each segment.
		/// </summary>
		/// <param name="points">The points.</param>
		/// <param name="stroke">The stroke color.</param>
		/// <param name="thickness">The stroke thickness.</param>
		/// <param name="edgeRenderingMode"></param>
		/// <param name="dashArray">The dash array.</param>
		/// <param name="lineJoin">The line join type.</param>
		public override void DrawLineSegments(IList<ScreenPoint> points, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode, double[] dashArray, LineJoin lineJoin) {
			_paint.Reset();
			{
				SetStroke(stroke, thickness, dashArray, lineJoin, edgeRenderingMode == EdgeRenderingMode.PreferGeometricAccuracy);
				_pts.Clear();
				if (edgeRenderingMode == EdgeRenderingMode.PreferGeometricAccuracy) {
					foreach (ScreenPoint p in points) {
						_pts.Add(ConvertAliased(p.X));
						_pts.Add(ConvertAliased(p.Y));
					}
				} else {
					foreach (ScreenPoint p in points) {
						_pts.Add(Convert(p.X));
						_pts.Add(Convert(p.Y));
					}
				}

				_canvas?.DrawLines(_pts.ToArray(), _paint);
			}
		}

		/// <summary>
		/// Draws a polygon. The polygon can have stroke and/or fill.
		/// </summary>
		/// <param name="points">The points.</param>
		/// <param name="fill">The fill color.</param>
		/// <param name="stroke">The stroke color.</param>
		/// <param name="thickness">The stroke thickness.</param>
		/// <param name="edgeRenderingMode"></param>
		/// <param name="dashArray">The dash array.</param>
		/// <param name="lineJoin">The line join type.</param>
		public override void DrawPolygon(IList<ScreenPoint> points, OxyColor fill, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode, double[] dashArray, LineJoin lineJoin) {
			_paint.Reset();
			{
				_path.Reset();
				{
					SetPath(points, edgeRenderingMode == EdgeRenderingMode.PreferGeometricAccuracy);
					_path.Close();

					if (fill.IsVisible()) {
						SetFill(fill);
						_canvas?.DrawPath(_path, _paint);
					}

					if (stroke.IsVisible()) {
						SetStroke(stroke, thickness, dashArray, lineJoin, edgeRenderingMode == EdgeRenderingMode.PreferGeometricAccuracy);
						_canvas?.DrawPath(_path, _paint);
					}
				}
			}
		}

		/// <summary>
		/// Draws a rectangle.
		/// </summary>
		/// <param name="rect">The rectangle.</param>
		/// <param name="fill">The fill color.</param>
		/// <param name="stroke">The stroke color.</param>
		/// <param name="thickness">The stroke thickness.</param>
		/// <param name="edgeRenderingMode"></param>
		public override void DrawRectangle(OxyRect rect, OxyColor fill, OxyColor stroke, double thickness, EdgeRenderingMode edgeRenderingMode) {
			_paint.Reset();
			{
				if (fill.IsVisible()) {
					SetFill(fill);
					_canvas?.DrawRect(ConvertAliased(rect.Left), ConvertAliased(rect.Top), ConvertAliased(rect.Right), ConvertAliased(rect.Bottom), _paint);
				}

				if (stroke.IsVisible()) {
					SetStroke(stroke, thickness, aliased: true);
					_canvas?.DrawRect(ConvertAliased(rect.Left), ConvertAliased(rect.Top), ConvertAliased(rect.Right), ConvertAliased(rect.Bottom), _paint);
				}
			}
		}

		/// <summary>
		/// Draws the text.
		/// </summary>
		/// <param name="p">The position of the text.</param>
		/// <param name="text">The text.</param>
		/// <param name="fill">The fill color.</param>
		/// <param name="fontFamily">The font family.</param>
		/// <param name="fontSize">Size of the font.</param>
		/// <param name="fontWeight">The font weight.</param>
		/// <param name="rotate">The rotation angle.</param>
		/// <param name="halign">The horizontal alignment.</param>
		/// <param name="valign">The vertical alignment.</param>
		/// <param name="maxSize">The maximum size of the text.</param>
		public override void DrawText(ScreenPoint p, string text, OxyColor fill, string fontFamily, double fontSize, double fontWeight, double rotate, HorizontalAlignment halign, VerticalAlignment valign, OxySize? maxSize) {
			_paint.Reset();
			{
				_paint.TextSize = Convert(fontSize);
				SetFill(fill);

				float width;
				float height;
				float lineHeight, delta;
				GetFontMetrics(_paint, out lineHeight, out delta);
				if (maxSize.HasValue || halign != HorizontalAlignment.Left || valign != VerticalAlignment.Bottom) {
					_paint.GetTextBounds(text, 0, text.Length, _bounds);
					width = _bounds.Left + _bounds.Width();
					height = lineHeight;
				} else {
					width = height = 0f;
				}

				if (maxSize.HasValue) {
					float maxWidth = Convert(maxSize.Value.Width);
					float maxHeight = Convert(maxSize.Value.Height);

					if (width > maxWidth) {
						width = maxWidth;
					}

					if (height > maxHeight) {
						height = maxHeight;
					}
				}

				double dx = halign == HorizontalAlignment.Left ? 0d : (halign == HorizontalAlignment.Center ? -width * 0.5 : -width);
				double dy = valign == VerticalAlignment.Bottom ? 0d : (valign == VerticalAlignment.Middle ? height * 0.5 : height);
				int x0 = -_bounds.Left;
				float y0 = delta;

				_canvas?.Save();
				_canvas?.Translate(Convert(p.X), Convert(p.Y));
				_canvas?.Rotate((float)rotate);
				_canvas?.Translate((float)dx + x0, (float)dy + y0);

				if (maxSize.HasValue) {
					int x1 = -x0;
					float y1 = -height - y0;
					_canvas?.ClipRect(x1, y1, x1 + width, y1 + height);
					_canvas?.Translate(0, lineHeight - height);
				}

				_canvas?.DrawText(text, 0, 0, _paint);
				_canvas?.Restore();
			}
		}

		/// <summary>
		/// Measures the text.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="fontFamily">The font family.</param>
		/// <param name="fontSize">Size of the font.</param>
		/// <param name="fontWeight">The font weight.</param>
		/// <returns>The text size.</returns>
		public override OxySize MeasureText(string text, string fontFamily, double fontSize, double fontWeight) {
			if (string.IsNullOrEmpty(text)) {
				return OxySize.Empty;
			}

			_paint.Reset();
			{
				_paint.AntiAlias = true;
				_paint.TextSize = Convert(fontSize);
				float lineHeight;
				GetFontMetrics(_paint, out lineHeight, out float _);
				_paint.GetTextBounds(text, 0, text.Length, _bounds);
				return new OxySize(_bounds.Width() / FontScale, lineHeight / FontScale);
			}
		}

		/// <summary>
		/// Sets the clip rectangle.
		/// </summary>
		/// <param name="rect">The clip rectangle.</param>
		/// <returns>True if the clip rectangle was set.</returns>
		public override void PushClip(OxyRect rect) {
			_canvas?.Save();
			using RectF rectF = Convert(rect);
			_canvas?.ClipRect(rectF);
		}

		/// <summary>
		/// Resets the clip rectangle.
		/// </summary>
		public override void PopClip() {
			_canvas?.Restore();
		}

		/// <summary>
		/// Draws the specified portion of the specified <see cref="OxyImage" /> at the specified location and with the specified size.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="srcX">The x-coordinate of the upper-left corner of the portion of the source image to draw.</param>
		/// <param name="srcY">The y-coordinate of the upper-left corner of the portion of the source image to draw.</param>
		/// <param name="srcWidth">Width of the portion of the source image to draw.</param>
		/// <param name="srcHeight">Height of the portion of the source image to draw.</param>
		/// <param name="destX">The x-coordinate of the upper-left corner of drawn image.</param>
		/// <param name="destY">The y-coordinate of the upper-left corner of drawn image.</param>
		/// <param name="destWidth">The width of the drawn image.</param>
		/// <param name="destHeight">The height of the drawn image.</param>
		/// <param name="opacity">The opacity.</param>
		/// <param name="interpolate">interpolate if set to <c>true</c>.</param>
		public override void DrawImage(
			OxyImage source,
			double srcX,
			double srcY,
			double srcWidth,
			double srcHeight,
			double destX,
			double destY,
			double destWidth,
			double destHeight,
			double opacity,
			bool interpolate) {
			Bitmap? image = GetImage(source);
			if (image == null) {
				return;
			}

			using Rect src = new((int)srcX, (int)srcY, (int)(srcX + srcWidth), (int)(srcY + srcHeight));
			using RectF dest = new(Convert(destX), Convert(destY), Convert(destX + destWidth), Convert(destY + destHeight));

			_paint.Reset();

			// TODO: support opacity
			_canvas?.DrawBitmap(image, src, dest, _paint);
		}

		/// <summary>
		/// Cleans up resources not in use.
		/// </summary>
		/// <remarks>This method is called at the end of each rendering.</remarks>
		public override void CleanUp() {
			List<OxyImage> imagesToRelease = _imageCache.Keys.Where(i => !_imagesInUse.Contains(i)).ToList();
			foreach (OxyImage i in imagesToRelease) {
				Bitmap? image = GetImage(i);
				image?.Dispose();
				_imageCache.Remove(i);
			}

			_imagesInUse.Clear();
		}

		/// <summary>
		/// Gets font metrics for the font in the specified paint.
		/// </summary>
		/// <param name="paint">The paint.</param>
		/// <param name="defaultLineHeight">Default line height.</param>
		/// <param name="delta">The vertical delta.</param>
		private static void GetFontMetrics(Paint paint, out float defaultLineHeight, out float delta) {
			Paint.FontMetrics metrics = paint.GetFontMetrics()!;
			float ascent = -metrics.Ascent;
			float descent = metrics.Descent;
			float leading = metrics.Leading;

			//// http://stackoverflow.com/questions/5511830/how-does-line-spacing-work-in-core-text-and-why-is-it-different-from-nslayoutm

			leading = leading < 0 ? 0 : (float)Math.Floor(leading + 0.5f);
			float lineHeight = (float)Math.Floor(ascent + 0.5f) + (float)Math.Floor(descent + 0.5) + leading;
			float ascenderDelta = leading >= 0 ? 0 : (float)Math.Floor((0.2 * lineHeight) + 0.5);
			defaultLineHeight = lineHeight + ascenderDelta;
			delta = ascenderDelta - descent;
		}

		/// <summary>
		/// Converts the specified coordinate to a scaled coordinate.
		/// </summary>
		/// <param name="x">The coordinate to convert.</param>
		/// <returns>The converted coordinate.</returns>
		private float Convert(double x) {
			return (float)(x * Scale);
		}

		/// <summary>
		/// Converts the specified rectangle to a scaled rectangle.
		/// </summary>
		/// <param name="rect">The rectangle to convert.</param>
		/// <returns>The converted rectangle.</returns>
		private RectF Convert(OxyRect rect) {
			return new RectF(ConvertAliased(rect.Left), ConvertAliased(rect.Top), ConvertAliased(rect.Right), ConvertAliased(rect.Bottom));
		}

		/// <summary>
		/// Converts the specified coordinate to a pixel-aligned scaled coordinate.
		/// </summary>
		/// <param name="x">The coordinate to convert.</param>
		/// <returns>The converted coordinate.</returns>
		private float ConvertAliased(double x) {
			return (int)(x * Scale) + 0.5f;
		}

		/// <summary>
		/// Sets the path to the specified points.
		/// </summary>
		/// <param name="points">The points defining the path.</param>
		/// <param name="aliased">If set to <c>true</c> aliased.</param>
		private void SetPath(IList<ScreenPoint> points, bool aliased) {
			if (aliased) {
				_path.MoveTo(ConvertAliased(points[0].X), ConvertAliased(points[0].Y));
				for (int i = 1; i < points.Count; i++) {
					_path.LineTo(ConvertAliased(points[i].X), ConvertAliased(points[i].Y));
				}
			} else {
				_path.MoveTo(Convert(points[0].X), Convert(points[0].Y));
				for (int i = 1; i < points.Count; i++) {
					_path.LineTo(Convert(points[i].X), Convert(points[i].Y));
				}
			}
		}

		/// <summary>
		/// Sets the fill style.
		/// </summary>
		/// <param name="fill">The fill color.</param>
		private void SetFill(OxyColor fill) {
			_paint.SetStyle(Paint.Style.Fill);
			_paint.Color = fill.ToColor();
			_paint.AntiAlias = true;
		}

		/// <summary>
		/// Sets the stroke style.
		/// </summary>
		/// <param name="stroke">The stroke color.</param>
		/// <param name="thickness">The stroke thickness.</param>
		/// <param name="dashArray">The dash array.</param>
		/// <param name="lineJoin">The line join.</param>
		/// <param name="aliased">Use aliased strokes if set to <c>true</c>.</param>
		private void SetStroke(OxyColor stroke, double thickness, double[]? dashArray = null, LineJoin lineJoin = LineJoin.Miter, bool aliased = false) {
			_paint.SetStyle(Paint.Style.Stroke);
			_paint.Color = stroke.ToColor();
			_paint.StrokeWidth = Convert(thickness);
			_paint.StrokeJoin = lineJoin.Convert();
			if (dashArray != null) {
				float[] dashArrayF = dashArray.Select(Convert).ToArray();
#pragma warning disable CA2000 // Dispose objects before losing scope
				DashPathEffect dashPathEffect = new(dashArrayF, 0f);
#pragma warning restore CA2000 // Dispose objects before losing scope
				_paint.SetPathEffect(dashPathEffect);
			}

			_paint.AntiAlias = !aliased;
		}

		/// <summary>
		/// Gets the image from cache or creates a new <see cref="Bitmap" />.
		/// </summary>
		/// <param name="source">The source image.</param>
		/// <returns>A <see cref="Bitmap" />.</returns>
		private Bitmap? GetImage(OxyImage? source) {
			if (source == null) {
				return null;
			}

			if (!_imagesInUse.Contains(source)) {
				_imagesInUse.Add(source);
			}

			Bitmap? bitmap;
			if (!_imageCache.TryGetValue(source, out bitmap)) {
				byte[] bytes = source.GetData();
				bitmap = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);
				if (bitmap != null) {
					_imageCache.Add(source, bitmap);
				}
			}

			return bitmap;
		}

		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {
				if (disposing) {
					// dispose managed state (managed objects)
					foreach (Bitmap cachedBitmap in _imageCache.Values) {
						cachedBitmap.Dispose();
					}
					_paint.Dispose();
					_path.Dispose();
					_bounds.Dispose();
					_canvas?.Dispose();
				}

				_disposedValue = true;
			}
		}

		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
