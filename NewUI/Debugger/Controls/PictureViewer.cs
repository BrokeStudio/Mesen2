﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Mesen.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mesen.Debugger.Controls
{
	public class PictureViewer : Control
	{
		public static readonly StyledProperty<IImage> SourceProperty = AvaloniaProperty.Register<PictureViewer, IImage>(nameof(Source));
		public static readonly StyledProperty<int> ZoomProperty = AvaloniaProperty.Register<PictureViewer, int>(nameof(Zoom), 1, defaultBindingMode: BindingMode.TwoWay);
		public static readonly StyledProperty<int> GridSizeXProperty = AvaloniaProperty.Register<PictureViewer, int>(nameof(GridSizeX), 8);
		public static readonly StyledProperty<int> GridSizeYProperty = AvaloniaProperty.Register<PictureViewer, int>(nameof(GridSizeY), 8);
		public static readonly StyledProperty<bool> ShowGridProperty = AvaloniaProperty.Register<PictureViewer, bool>(nameof(ShowGrid), false);

		public static readonly StyledProperty<int> AltGridSizeXProperty = AvaloniaProperty.Register<PictureViewer, int>(nameof(AltGridSizeX), 8);
		public static readonly StyledProperty<int> AltGridSizeYProperty = AvaloniaProperty.Register<PictureViewer, int>(nameof(AltGridSizeY), 8);
		public static readonly StyledProperty<bool> ShowAltGridProperty = AvaloniaProperty.Register<PictureViewer, bool>(nameof(ShowAltGrid), false);
		public static readonly StyledProperty<bool> AllowSelectionProperty = AvaloniaProperty.Register<PictureViewer, bool>(nameof(AllowSelection), true);

		public static readonly StyledProperty<bool> ShowMousePositionProperty = AvaloniaProperty.Register<PictureViewer, bool>(nameof(ShowMousePosition), true);
		public static readonly StyledProperty<Rect?> MouseOverRectProperty = AvaloniaProperty.Register<PictureViewer, Rect?>(nameof(MouseOverRect), null, defaultBindingMode: BindingMode.OneWay);

		public static readonly StyledProperty<Rect> SelectionRectProperty = AvaloniaProperty.Register<PictureViewer, Rect>(nameof(SelectionRect), Rect.Empty, defaultBindingMode: BindingMode.TwoWay);
		public static readonly StyledProperty<Rect> OverlayRectProperty = AvaloniaProperty.Register<PictureViewer, Rect>(nameof(OverlayRect), Rect.Empty);

		public static readonly StyledProperty<List<Rect>?> HighlightRectsProperty = AvaloniaProperty.Register<PictureViewer, List<Rect>?>(nameof(HighlightRects), null);

		public static readonly RoutedEvent<PositionClickedEventArgs> PositionClickedEvent = RoutedEvent.Register<PictureViewer, PositionClickedEventArgs>(nameof(PositionClicked), RoutingStrategies.Bubble);
		public event EventHandler<PositionClickedEventArgs> PositionClicked
		{
			add => AddHandler(PositionClickedEvent, value);
			remove => RemoveHandler(PositionClickedEvent, value);
		}

		private delegate void PositionClickedHandler(Point p);

		private Stopwatch _stopWatch = Stopwatch.StartNew();
		private DispatcherTimer _timer = new DispatcherTimer();

		public IImage Source
		{
			get { return GetValue(SourceProperty); }
			set { SetValue(SourceProperty, value); }
		}

		public int Zoom
		{
			get { return GetValue(ZoomProperty); }
			set { SetValue(ZoomProperty, value); }
		}
		
		public bool AllowSelection
		{
			get { return GetValue(AllowSelectionProperty); }
			set { SetValue(AllowSelectionProperty, value); }
		}

		public bool ShowMousePosition
		{
			get { return GetValue(ShowMousePositionProperty); }
			set { SetValue(ShowMousePositionProperty, value); }
		}

		public int GridSizeX
		{
			get { return GetValue(GridSizeXProperty); }
			set { SetValue(GridSizeXProperty, value); }
		}

		public int GridSizeY
		{
			get { return GetValue(GridSizeYProperty); }
			set { SetValue(GridSizeYProperty, value); }
		}

		public bool ShowGrid
		{
			get { return GetValue(ShowGridProperty); }
			set { SetValue(ShowGridProperty, value); }
		}

		public int AltGridSizeX
		{
			get { return GetValue(AltGridSizeXProperty); }
			set { SetValue(AltGridSizeXProperty, value); }
		}

		public int AltGridSizeY
		{
			get { return GetValue(AltGridSizeYProperty); }
			set { SetValue(AltGridSizeYProperty, value); }
		}

		public bool ShowAltGrid
		{
			get { return GetValue(ShowAltGridProperty); }
			set { SetValue(ShowAltGridProperty, value); }
		}

		public Rect SelectionRect
		{
			get { return GetValue(SelectionRectProperty); }
			set { SetValue(SelectionRectProperty, value); }
		}

		public Rect OverlayRect
		{
			get { return GetValue(OverlayRectProperty); }
			set { SetValue(OverlayRectProperty, value); }
		}

		public Rect? MouseOverRect
		{
			get { return GetValue(MouseOverRectProperty); }
			set { SetValue(MouseOverRectProperty, value); }
		}

		public List<Rect>? HighlightRects
		{
			get { return GetValue(HighlightRectsProperty); }
			set { SetValue(HighlightRectsProperty, value); }
		}

		static PictureViewer()
		{
			AffectsRender<PictureViewer>(
				SourceProperty, ZoomProperty, GridSizeXProperty, GridSizeYProperty,
				ShowGridProperty, SelectionRectProperty, OverlayRectProperty,
				HighlightRectsProperty, MouseOverRectProperty
			);

			SourceProperty.Changed.AddClassHandler<PictureViewer>((x, e) => {
				x.UpdateSize();

				if(e.OldValue is IDynamicBitmap oldSource) {
					oldSource.Invalidated -= x.OnSourceInvalidated;
				}

				if(x.Source is IDynamicBitmap newSource) {
					newSource.Invalidated += x.OnSourceInvalidated;
				}
			});

			ZoomProperty.Changed.AddClassHandler<PictureViewer>((x, e) => {
				x.UpdateSize();
			});
		}

		private void OnSourceInvalidated(object? sender, EventArgs e)
		{
			InvalidateVisual();
		}

		protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
		{
			base.OnAttachedToVisualTree(e);
			_timer.Interval = TimeSpan.FromMilliseconds(250);
			_timer.Tick += timer_Tick;
			_timer.Start();
			UpdateSize();
		}

		private void timer_Tick(object? sender, EventArgs e)
		{
			if(SelectionRect != Rect.Empty) {
				InvalidateVisual();
			}
		}

		protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
		{
			base.OnDetachedFromVisualTree(e);
			_timer.Stop();
		}

		protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
		{
			base.OnPointerWheelChanged(e);
			if(e.KeyModifiers == KeyModifiers.Control) {
				if(e.Delta.Y > 0) {
					ZoomIn();
				} else {
					ZoomOut();
				}
				e.Handled = true;
			}
		}

		public void ZoomIn()
		{
			Zoom = Math.Min(20, Math.Max(1, Zoom + 1));
		}

		public void ZoomOut()
		{
			Zoom = Math.Min(20, Math.Max(1, Zoom - 1));
		}

		private void UpdateSize()
		{
			if(Source == null) {
				MinWidth = 0;
				MinHeight = 0;
			} else {
				double dpiScale = LayoutHelper.GetLayoutScale(this);
				MinWidth = (int)Source.Size.Width * Zoom / dpiScale;
				MinHeight = (int)Source.Size.Height * Zoom / dpiScale;
			}
		}

		protected override void OnPointerMoved(PointerEventArgs e)
		{
			base.OnPointerMoved(e);
			if(ShowMousePosition) {
				PixelPoint? p = GetGridPointFromMousePoint(e.GetCurrentPoint(this).Position);
				if(p != null) {
					MouseOverRect = GetTileRect(p.Value);
				} else {
					MouseOverRect = null;
				}
			}
		}

		protected override void OnPointerLeave(PointerEventArgs e)
		{
			base.OnPointerLeave(e);
			MouseOverRect = null;
		}

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			PixelPoint? p = GetGridPointFromMousePoint(e.GetCurrentPoint(this).Position);
			if(p == null) {
				e.Handled = true;
				return;
			}

			PositionClickedEventArgs args = new() { RoutedEvent = PositionClickedEvent, Position = p.Value };
			RaiseEvent(args);

			if(!args.Handled && AllowSelection) {
				SelectionRect = GetTileRect(p.Value);
			}
		}

		public PixelPoint? GetGridPointFromMousePoint(Point p)
		{
			if(p.X >= MinWidth || p.Y >= MinHeight) {
				return null;
			}

			double scale = LayoutHelper.GetLayoutScale(this) / Zoom;
			return PixelPoint.FromPoint(p, scale);
		}

		private Rect GetTileRect(PixelPoint p)
		{
			return new Rect(
				p.X / GridSizeX * GridSizeX,
				p.Y / GridSizeY * GridSizeY,
				GridSizeX,
				GridSizeY
			);
		}

		private Rect ToDrawRect(Rect r)
		{
			return new Rect(
				r.X * Zoom - 0.5,
				r.Y * Zoom - 0.5,
				r.Width * Zoom + 1,
				r.Height * Zoom + 1
			);
		}

		private void DrawGrid(DrawingContext context, bool show, int gridX, int gridY, Color color)
		{
			if(show) {
				int width = (int)Source.Size.Width * Zoom;
				int height = (int)Source.Size.Height * Zoom;
				int gridSizeX = gridX * Zoom;
				int gridSizeY = gridY * Zoom;

				Pen pen = new Pen(color.ToUint32(), 1);
				double offset = 0.5;
				for(int i = 1; i < width / gridSizeX; i++) {
					context.DrawLine(pen, new Point(i * gridSizeX + offset, 0), new Point(i * gridSizeX + offset, height));
				}
				for(int i = 1; i < height / gridSizeY; i++) {
					context.DrawLine(pen, new Point(0, i * gridSizeY + offset), new Point(width, i * gridSizeY + offset));
				}
			}
		}

		public override void Render(DrawingContext context)
		{
			if(Source == null) {
				return;
			}

			int width = (int)Source.Size.Width * Zoom;
			int height = (int)Source.Size.Height * Zoom;

			double dpiScale = 1 / LayoutHelper.GetLayoutScale(this);
			using var scale = context.PushPostTransform(Matrix.CreateScale(dpiScale, dpiScale));

			using var clip = context.PushClip(new Rect(0, 0, width, height));

			context.DrawImage(
				Source,
				new Rect(0, 0, (int)Source.Size.Width, (int)Source.Size.Height),
				new Rect(0, 0, width, height),
				Avalonia.Visuals.Media.Imaging.BitmapInterpolationMode.Default
			);

			DrawGrid(context, ShowGrid, GridSizeX, GridSizeY, Color.FromArgb(192, Colors.LightBlue.R, Colors.LightBlue.G, Colors.LightBlue.B));
			DrawGrid(context, ShowAltGrid, AltGridSizeX, AltGridSizeY, Color.FromArgb(192, Colors.Red.R, Colors.Red.G, Colors.Red.B));

			if(OverlayRect != Rect.Empty) {
				Rect rect = ToDrawRect(OverlayRect);
				Brush brush = new SolidColorBrush(Colors.Gray, 0.4);
				Pen pen = new Pen(Brushes.White, 2);

				context.FillRectangle(brush, rect);
				context.DrawRectangle(pen, rect.Inflate(0.5));

				if((rect.Top + rect.Height) > height) {
					Rect offsetRect = rect.Translate(new Vector(0, -height));
					context.FillRectangle(brush, offsetRect);
					context.DrawRectangle(pen, offsetRect.Inflate(0.5));
				}

				if((rect.Left + rect.Width) > width) {
					Rect offsetRect = rect.Translate(new Vector(-width, 0));
					context.FillRectangle(brush, offsetRect);
					context.DrawRectangle(pen, offsetRect.Inflate(0.5));

					if((rect.Top + rect.Height) > height) {
						offsetRect = rect.Translate(new Vector(-width, -height));
						context.FillRectangle(brush, offsetRect);
						context.DrawRectangle(pen, offsetRect.Inflate(0.5));
					}
				}
			}

			if(HighlightRects?.Count > 0) {
				Pen pen = new Pen(Brushes.LightSteelBlue, 1);
				foreach(Rect highlightRect in HighlightRects) {
					Rect rect = ToDrawRect(highlightRect);
					context.DrawRectangle(pen, rect);
				}
			}
			
			if(MouseOverRect != null && MouseOverRect != Rect.Empty) {
				Rect rect = ToDrawRect(MouseOverRect.Value);
				DashStyle dashes = new DashStyle(DashStyle.Dash.Dashes, 0);
				context.DrawRectangle(new Pen(Brushes.DimGray, 2), rect.Inflate(0.5));
				context.DrawRectangle(new Pen(Brushes.LightYellow, 2, dashes), rect.Inflate(0.5));
			}

			if(SelectionRect != Rect.Empty) {
				Rect rect = ToDrawRect(SelectionRect);
				
				DashStyle dashes = new DashStyle(DashStyle.Dash.Dashes, _stopWatch.ElapsedMilliseconds / 250.0);
				context.DrawRectangle(new Pen(Brushes.Black, 2), rect.Inflate(0.5));
				context.DrawRectangle(new Pen(Brushes.White, 2, dashes), rect.Inflate(0.5));
			}
		}
	}

	public class PositionClickedEventArgs : RoutedEventArgs
	{
		public PixelPoint Position;
	}
}
