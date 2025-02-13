#nullable enable
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CoreGraphicsRenderContext.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Implements a <see cref="IRenderContext"/> for MonoTouch CoreGraphics.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Xamarin.iOS {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using CoreGraphics;
	using CoreText;
	using Foundation;
	using UIKit;

	/// <summary>
	/// Implements a <see cref="IRenderContext"/> for CoreGraphics.
	/// </summary>
	public class CoreGraphicsRenderContext : RenderContextBase, IDisposable {
		/// <summary>
		/// The images in use.
		/// </summary>
		private readonly HashSet<OxyImage> _imagesInUse = new();

		/// <summary>
		/// The fonts cache.
		/// </summary>
		private readonly Dictionary<string, CTFont> _fonts = new();

		/// <summary>
		/// The image cache.
		/// </summary>
		private readonly Dictionary<OxyImage, UIImage?> _imageCache = new();

		/// <summary>
		/// The graphics context.
		/// </summary>
		private readonly CGContext _gCtx;

		private int _clipCount;

		public override int ClipCount => _clipCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoreGraphicsRenderContext"/> class.
		/// </summary>
		/// <param name="context">The context.</param>
		public CoreGraphicsRenderContext(
			CGContext context
		) {
			_gCtx = context;

			// Set rendering quality
			_gCtx.SetAllowsFontSmoothing(true);
			_gCtx.SetAllowsFontSubpixelQuantization(true);
			_gCtx.SetAllowsAntialiasing(true);
			_gCtx.SetShouldSmoothFonts(true);
			_gCtx.SetShouldAntialias(true);
			_gCtx.InterpolationQuality = CGInterpolationQuality.High;
			_gCtx.SetTextDrawingMode(CGTextDrawingMode.Fill);
		}

		/// <summary>
		/// Draws an ellipse.
		/// </summary>
		/// <param name="rect">The rectangle.</param>
		/// <param name="fill">The fill color.</param>
		/// <param name="stroke">The stroke color.</param>
		/// <param name="thickness">The thickness.</param>
		/// <param name="edgeRenderingMode"></param>
		public override void DrawEllipse(
			OxyRect rect,
			OxyColor fill,
			OxyColor stroke,
			double thickness,
			EdgeRenderingMode edgeRenderingMode
		) {
			SetAlias(false);
			CGRect convertedRectangle = rect.Convert();
			if (fill.IsVisible()) {
				_gCtx.SaveState();
				using CGColor fillColor = SetFill(fill);
				using (CGPath path = new()) {
					path.AddEllipseInRect(convertedRectangle);
					_gCtx.AddPath(path);
				}

				_gCtx.DrawPath(CGPathDrawingMode.Fill);
				_gCtx.RestoreState();
			}

			if (stroke.IsVisible()
				&& thickness > 0) {
				_gCtx.SaveState();
				using CGColor strokeColor = SetStroke(stroke, thickness);

				using (CGPath path = new()) {
					path.AddEllipseInRect(convertedRectangle);
					_gCtx.AddPath(path);
				}

				_gCtx.DrawPath(CGPathDrawingMode.Stroke);
				_gCtx.RestoreState();
			}
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
		/// <param name="interpolate">Interpolate if set to <c>true</c>.</param>
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
			bool interpolate
		) {
			UIImage? image = GetImage(source);
			if (image == null) {
				return;
			}

			_gCtx.SaveState();

			double x = destX - (srcX / srcWidth * destWidth);
			double y = destY - (srcY / srcHeight * destHeight);
			_gCtx.ScaleCTM(1, -1);
			_gCtx.TranslateCTM((float)x, -(float)(y + destHeight));
			_gCtx.SetAlpha((float)opacity);
			_gCtx.InterpolationQuality = interpolate
				? CGInterpolationQuality.High
				: CGInterpolationQuality.None;
			CGRect destRect = new(0f, 0f, (float)destWidth, (float)destHeight);
			_gCtx.DrawImage(destRect, image.CGImage);
			_gCtx.RestoreState();
		}

		/// <summary>
		/// Cleans up resources not in use.
		/// </summary>
		/// <remarks>This method is called at the end of each rendering.</remarks>
		public override void CleanUp() {
			List<OxyImage> imagesToRelease = _imageCache.Keys.Where(i => !_imagesInUse.Contains(i)).ToList();
			foreach (OxyImage i in imagesToRelease) {
				UIImage? image = GetImage(i);
				image?.Dispose();
				_imageCache.Remove(i);
			}

			_imagesInUse.Clear();
		}

		/// <summary>
		/// Sets the clip rectangle.
		/// </summary>
		/// <param name="rect">The clip rectangle.</param>
		/// <returns>True if the clip rectangle was set.</returns>
		public override void PushClip(
			OxyRect rect
		) {
			_gCtx.SaveState();
			_gCtx.ClipToRect(rect.Convert());
			_clipCount++;
		}

		/// <summary>
		/// Resets the clip rectangle.
		/// </summary>
		public override void PopClip() {
			_gCtx.RestoreState();
			_clipCount--;
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
		public override void DrawLine(
			IList<ScreenPoint> points,
			OxyColor stroke,
			double thickness,
			EdgeRenderingMode edgeRenderingMode,
			double[] dashArray,
			LineJoin lineJoin
		) {
			if (!stroke.IsVisible()
				|| !(thickness > 0)) {
				return;
			}

			_gCtx.SaveState();
			SetAlias(edgeRenderingMode == EdgeRenderingMode.PreferGeometricAccuracy);
			using CGColor strokeColor = SetStroke(stroke, thickness, dashArray, lineJoin);

			using (CGPath path = new()) {
				CGPoint[] convertedPoints = (edgeRenderingMode == EdgeRenderingMode.PreferGeometricAccuracy
					? points.Select(p => p.ConvertAliased())
					: points.Select(p => p.Convert())).ToArray();
				path.AddLines(convertedPoints);
				_gCtx.AddPath(path);
			}

			_gCtx.DrawPath(CGPathDrawingMode.Stroke);
			_gCtx.RestoreState();
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
		public override void DrawPolygon(
			IList<ScreenPoint> points,
			OxyColor fill,
			OxyColor stroke,
			double thickness,
			EdgeRenderingMode edgeRenderingMode,
			double[] dashArray,
			LineJoin lineJoin
		) {
			SetAlias(edgeRenderingMode == EdgeRenderingMode.PreferGeometricAccuracy);
			CGPoint[] convertedPoints = (edgeRenderingMode == EdgeRenderingMode.PreferGeometricAccuracy
				? points.Select(p => p.ConvertAliased())
				: points.Select(p => p.Convert())).ToArray();
			if (fill.IsVisible()) {
				_gCtx.SaveState();
				using CGColor fillColor = SetFill(fill);
				using (CGPath path = new()) {
					path.AddLines(convertedPoints);
					path.CloseSubpath();
					_gCtx.AddPath(path);
				}

				_gCtx.DrawPath(CGPathDrawingMode.Fill);
				_gCtx.RestoreState();
			}

			if (stroke.IsVisible()
				&& thickness > 0) {
				_gCtx.SaveState();
				using CGColor strokeColor = SetStroke(stroke, thickness, dashArray, lineJoin);

				using (CGPath path = new()) {
					path.AddLines(convertedPoints);
					path.CloseSubpath();
					_gCtx.AddPath(path);
				}

				_gCtx.DrawPath(CGPathDrawingMode.Stroke);
				_gCtx.RestoreState();
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
		public override void DrawRectangle(
			OxyRect rect,
			OxyColor fill,
			OxyColor stroke,
			double thickness,
			EdgeRenderingMode edgeRenderingMode
		) {
			SetAlias(true);
			CGRect convertedRect = rect.ConvertAliased();

			if (fill.IsVisible()) {
				_gCtx.SaveState();
				using CGColor fillColor = SetFill(fill);
				using (CGPath path = new()) {
					path.AddRect(convertedRect);
					_gCtx.AddPath(path);
				}

				_gCtx.DrawPath(CGPathDrawingMode.Fill);
				_gCtx.RestoreState();
			}

			if (stroke.IsVisible()
				&& thickness > 0) {
				_gCtx.SaveState();
				using CGColor strokeColor = SetStroke(stroke, thickness);
				using (CGPath path = new()) {
					path.AddRect(convertedRect);
					_gCtx.AddPath(path);
				}

				_gCtx.DrawPath(CGPathDrawingMode.Stroke);
				_gCtx.RestoreState();
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
		public override void DrawText(
			ScreenPoint p,
			string text,
			OxyColor fill,
			string fontFamily,
			double fontSize,
			double fontWeight,
			double rotate,
			HorizontalAlignment halign,
			VerticalAlignment valign,
			OxySize? maxSize
		) {
			if (string.IsNullOrEmpty(text)) {
				return;
			}

			string fontName = GetActualFontName(fontFamily, fontWeight);

			CTFont font = GetCachedFont(fontName, fontSize);
			using NSAttributedString attributedString = new(
				text, new CTStringAttributes {
					ForegroundColorFromContext = true,
					Font = font
				}
			);
			using CTLine textLine = new(attributedString);
			nfloat width;
			nfloat height;

			_gCtx.TextPosition = new CGPoint(0, 0);

			GetFontMetrics(font, out nfloat lineHeight, out nfloat delta);
			CGRect bounds = textLine.GetImageBounds(_gCtx);

			if (maxSize.HasValue
				|| halign != HorizontalAlignment.Left
				|| valign != VerticalAlignment.Bottom) {
				width = bounds.Left + bounds.Width;
				height = lineHeight;
			} else {
				width = height = 0f;
			}

			if (maxSize.HasValue) {
				if (width > maxSize.Value.Width) {
					width = (float)maxSize.Value.Width;
				}

				if (height > maxSize.Value.Height) {
					height = (float)maxSize.Value.Height;
				}
			}

			double dx = halign == HorizontalAlignment.Left
				? 0d
				: (halign == HorizontalAlignment.Center
					? -width * 0.5
					: -width);
			double dy = valign == VerticalAlignment.Bottom
				? 0d
				: (valign == VerticalAlignment.Middle
					? height * 0.5
					: height);
			nfloat x0 = -bounds.Left;
			nfloat y0 = delta;

			CGColor fillColor = SetFill(fill);
			SetAlias(false);

			_gCtx.SaveState();
			_gCtx.TranslateCTM((float)p.X, (float)p.Y);
			if (!rotate.Equals(0)) {
				_gCtx.RotateCTM((float)(rotate / 180 * Math.PI));
			}

			_gCtx.TranslateCTM((float)dx + x0, (float)dy + y0);
			_gCtx.ScaleCTM(1f, -1f);

			if (maxSize.HasValue) {
				CGRect clipRect = new(-x0, y0, (float)Math.Ceiling(width), (float)Math.Ceiling(height));
				_gCtx.ClipToRect(clipRect);
			}

			textLine.Draw(_gCtx);

			_gCtx.RestoreState();
			fillColor.Dispose();
		}

		/// <summary>
		/// Measures the text.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="fontFamily">The font family.</param>
		/// <param name="fontSize">Size of the font.</param>
		/// <param name="fontWeight">The font weight.</param>
		/// <returns>
		/// The size of the text.
		/// </returns>
		public override OxySize MeasureText(
			string text,
			string? fontFamily,
			double fontSize,
			double fontWeight
		) {
			if (string.IsNullOrEmpty(text)
				|| fontFamily == null) {
				return OxySize.Empty;
			}

			string fontName = GetActualFontName(fontFamily, fontWeight);
			CTFont font = GetCachedFont(fontName, (float)fontSize);
			using NSAttributedString attributedString = new(
				text, new CTStringAttributes {
					ForegroundColorFromContext = true,
					Font = font
				}
			);
			using CTLine textLine = new(attributedString);
			GetFontMetrics(font, out nfloat lineHeight, out nfloat _);
			_gCtx.TextPosition = new CGPoint(0, 0);
			CGRect bounds = textLine.GetImageBounds(_gCtx);
			return new OxySize(bounds.Left + bounds.Width, lineHeight);
		}

		/// <summary>
		/// Releases all resource used by the <see cref="OxyPlot.Xamarin.iOS.CoreGraphicsRenderContext"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the
		/// <see cref="OxyPlot.Xamarin.iOS.CoreGraphicsRenderContext"/>. The <see cref="Dispose"/> method leaves the
		/// <see cref="OxyPlot.Xamarin.iOS.CoreGraphicsRenderContext"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the
		/// <see cref="OxyPlot.Xamarin.iOS.CoreGraphicsRenderContext"/> so the garbage collector can reclaim the memory that
		/// the <see cref="OxyPlot.Xamarin.iOS.CoreGraphicsRenderContext"/> was occupying.</remarks>
		public void Dispose() {
			foreach (UIImage? image in _imageCache.Values) {
				image?.Dispose();
			}

			foreach (CTFont font in _fonts.Values) {
				font.Dispose();
			}
		}

		/// <summary>
		/// Gets the actual font for iOS.
		/// </summary>
		/// <param name="fontFamily">The font family.</param>
		/// <param name="fontWeight">The font weight.</param>
		/// <returns>The actual font name.</returns>
		private static string GetActualFontName(
			string fontFamily,
			double fontWeight
		) {
			string fontName = fontFamily switch {
				null or "Segoe UI" => "HelveticaNeue",
				"Arial" => "ArialMT",
				"Times" or "Times New Roman" => "TimesNewRomanPSMT",
				"Courier New" => "CourierNewPSMT",
				_ => fontFamily,
			};
			if (fontWeight >= 700) {
				fontName += "-Bold";
			}

			return fontName;
		}

		/// <summary>
		/// Gets font metrics for the specified font.
		/// </summary>
		/// <param name="font">The font.</param>
		/// <param name="defaultLineHeight">Default line height.</param>
		/// <param name="delta">The vertical delta.</param>
		private void GetFontMetrics(
			CTFont font,
			out nfloat defaultLineHeight,
			out nfloat delta
		) {
			nfloat ascent = font.AscentMetric;
			nfloat descent = font.DescentMetric;
			nfloat leading = font.LeadingMetric;

			//// http://stackoverflow.com/questions/5511830/how-does-line-spacing-work-in-core-text-and-why-is-it-different-from-nslayoutm

			leading = leading < 0
				? 0
				: (float)Math.Floor(leading + 0.5f);
			nfloat lineHeight = (nfloat)Math.Floor(ascent + 0.5f) + (nfloat)Math.Floor(descent + 0.5) + leading;
			nfloat ascenderDelta = leading >= 0
				? 0
				: (nfloat)Math.Floor((0.2 * lineHeight) + 0.5);
			defaultLineHeight = lineHeight + ascenderDelta;
			delta = ascenderDelta - descent;
		}

		/// <summary>
		/// Gets the specified from cache.
		/// </summary>
		/// <returns>The font.</returns>
		/// <param name="fontName">Font name.</param>
		/// <param name="fontSize">Font size.</param>
		private CTFont GetCachedFont(
			string fontName,
			double fontSize
		) {
			string key = $"{fontName}{fontSize:0.###}";
			if (_fonts.TryGetValue(key, out CTFont? font)) {
				return font;
			}

			return _fonts[key] = new CTFont(fontName, (float)fontSize);
		}

		/// <summary>
		/// Sets the alias state.
		/// </summary>
		/// <param name="alias">alias if set to <c>true</c>.</param>
		private void SetAlias(
			bool alias
		) {
			_gCtx.SetShouldAntialias(!alias);
		}

		/// <summary>
		/// Sets the fill color.
		/// </summary>
		/// <param name="c">The color.</param>
		private CGColor SetFill(
			OxyColor c
		) {
			CGColor color = c.ToCGColor();
			_gCtx.SetFillColor(color);
			return color;
		}

		/// <summary>
		/// Sets the stroke style.
		/// </summary>
		/// <param name="c">The stroke color.</param>
		/// <param name="thickness">The stroke thickness.</param>
		/// <param name="dashArray">The dash array.</param>
		/// <param name="lineJoin">The line join.</param>
		private CGColor SetStroke(
			OxyColor c,
			double thickness,
			double[]? dashArray = null,
			LineJoin lineJoin = LineJoin.Miter
		) {
			CGColor color = c.ToCGColor();
			_gCtx.SetStrokeColor(color);
			_gCtx.SetLineWidth((float)thickness);
			_gCtx.SetLineJoin(lineJoin.Convert());
			if (dashArray != null) {
				nfloat[] lengths = dashArray.Select(d => (nfloat)d).ToArray();
				_gCtx.SetLineDash(0f, lengths);
			} else {
				_gCtx.SetLineDash(0, null);
			}

			return color;
		}

		/// <summary>
		/// Gets the image from cache or converts the specified <paramref name="source"/> <see cref="OxyImage"/>.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <returns>The image.</returns>
		private UIImage? GetImage(
			OxyImage? source
		) {
			if (source == null) {
				return null;
			}

			if (!_imagesInUse.Contains(source)) {
				_imagesInUse.Add(source);
			}

			if (_imageCache.TryGetValue(source, out UIImage? src)) return src;

			using (NSData data = NSData.FromArray(source.GetData())) {
				src = UIImage.LoadFromData(data);
			}

			_imageCache.Add(source, src);

			return src;
		}
	}
}
