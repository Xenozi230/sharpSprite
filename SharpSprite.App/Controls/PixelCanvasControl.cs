using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SharpSprite.App.Tools;
using SharpSprite.Core.Commands;
using SharpSprite.Core.Document;
using SharpSprite.Rendering;
using SkiaSharp;

namespace SharpSprite.App.Controls
{
    /// <summary>
    /// The pixel canvas control.
    ///
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Composite the document's visible layers via <see cref="SpriteCompositor"/>.</item>
    ///   <item>Render the composited <see cref="SKBitmap"/> via a Skia draw op
    ///         with nearest-neighbour sampling, checkerboard background, and
    ///         integer-zoom + pan transform.</item>
    ///   <item>Dispatch pointer events to the active <see cref="ITool"/>.</item>
    ///   <item>Handle Ctrl+scroll-wheel zoom independently of the active tool.</item>
    /// </list>
    /// </summary>
    public sealed class PixelCanvasControl : Control
    {
        // ══════════════════════════════════════════════════════════════════
        // Avalonia styled properties
        // ══════════════════════════════════════════════════════════════════

        public static readonly StyledProperty<Document?> DocumentProperty =
            AvaloniaProperty.Register<PixelCanvasControl, Document?>(nameof(Document));

        public static readonly StyledProperty<int> ActiveFrameProperty =
            AvaloniaProperty.Register<PixelCanvasControl, int>(nameof(ActiveFrame), defaultValue: 0);

        public static readonly StyledProperty<int> ZoomProperty =
            AvaloniaProperty.Register<PixelCanvasControl, int>(nameof(Zoom), defaultValue: 0);

        public static readonly StyledProperty<Vector> PanOffsetProperty =
            AvaloniaProperty.Register<PixelCanvasControl, Vector>(nameof(PanOffset));

        /// <summary>The active tool type.  Set by the ViewModel / tool palette.</summary>
        public static readonly StyledProperty<ToolType> ActiveToolTypeProperty =
            AvaloniaProperty.Register<PixelCanvasControl, ToolType>(nameof(ActiveToolType), ToolType.Pencil);

        /// <summary>The undo stack to pass through to tools.</summary>
        public static readonly StyledProperty<UndoStack?> UndoStackProperty =
            AvaloniaProperty.Register<PixelCanvasControl, UndoStack?>(nameof(UndoStack));

        /// <summary>Foreground (primary) drawing color.</summary>
        public static readonly StyledProperty<Rgba32> ForegroundColorProperty =
            AvaloniaProperty.Register<PixelCanvasControl, Rgba32>(nameof(ForegroundColor),
                defaultValue: new Rgba32(0, 0, 0, 255));

        /// <summary>Background (secondary) drawing color.</summary>
        public static readonly StyledProperty<Rgba32> BackgroundColorProperty =
            AvaloniaProperty.Register<PixelCanvasControl, Rgba32>(nameof(BackgroundColor),
                defaultValue: new Rgba32(255, 255, 255, 255));

        // ══════════════════════════════════════════════════════════════════
        // CLR wrappers
        // ══════════════════════════════════════════════════════════════════

        public Document? Document { get => GetValue(DocumentProperty); set => SetValue(DocumentProperty, value); }
        public int ActiveFrame { get => GetValue(ActiveFrameProperty); set => SetValue(ActiveFrameProperty, value); }
        public int Zoom { get => GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }
        public Vector PanOffset { get => GetValue(PanOffsetProperty); set => SetValue(PanOffsetProperty, value); }
        public ToolType ActiveToolType { get => GetValue(ActiveToolTypeProperty); set => SetValue(ActiveToolTypeProperty, value); }
        public UndoStack? UndoStack { get => GetValue(UndoStackProperty); set => SetValue(UndoStackProperty, value); }
        public Rgba32 ForegroundColor { get => GetValue(ForegroundColorProperty); set => SetValue(ForegroundColorProperty, value); }
        public Rgba32 BackgroundColor { get => GetValue(BackgroundColorProperty); set => SetValue(BackgroundColorProperty, value); }

        public Action<int, int>? CursorMoved;
        public Action<int>? ZoomChanged;

        // ══════════════════════════════════════════════════════════════════
        // Private state
        // ══════════════════════════════════════════════════════════════════

        private readonly SpriteCompositor _compositor = new();
        private readonly ToolRegistry _toolRegistry = new();

        private SKBitmap? _latestBitmap;
        private Document? _subscribedDocument;

        // Cached transform so tools can read it synchronously during events
        private float _cachedScale = 1f;
        private float _cachedOffsetX = 0f;
        private float _cachedOffsetY = 0f;

        private bool _middlePanning;
        private Point _middlePanLastPt;

        // ══════════════════════════════════════════════════════════════════
        // Construction / lifecycle
        // ══════════════════════════════════════════════════════════════════

        public PixelCanvasControl()
        {
            Focusable = true;
            ClipToBounds = true;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Focus();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            UnsubscribeDocument();
            _compositor.Dispose();
        }

        // ══════════════════════════════════════════════════════════════════
        // Property change handling
        // ══════════════════════════════════════════════════════════════════

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DocumentProperty)
            {
                UnsubscribeDocument();
                _subscribedDocument = Document;
                if (_subscribedDocument != null)
                    _subscribedDocument.Changed += OnDocumentChanged;
                RefreshComposite();
            }
            else if (change.Property == ActiveFrameProperty)
            {
                RefreshComposite();
            }
            else if (change.Property == ZoomProperty ||
                     change.Property == PanOffsetProperty)
            {
                ZoomChanged?.Invoke(Zoom <= 0 ? (int)ComputeAutoZoom() : Zoom);
                InvalidateVisual();
            }
            else if (change.Property == ActiveToolTypeProperty)
            {
                UpdateCursor();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Rendering
        // ══════════════════════════════════════════════════════════════════

        public override void Render(DrawingContext context)
        {
            if (_latestBitmap == null) return;

            var transform = ComputeTransform(_latestBitmap.Width, _latestBitmap.Height);
            _cachedScale = transform.scale;
            _cachedOffsetX = transform.offsetX;
            _cachedOffsetY = transform.offsetY;

            context.Custom(new PixelCanvasDrawOperation(
                new Rect(Bounds.Size),
                _latestBitmap,
                transform));
        }

        // ══════════════════════════════════════════════════════════════════
        // Composite
        // ══════════════════════════════════════════════════════════════════

        private void RefreshComposite()
        {
            var doc = Document;
            if (doc == null) { _latestBitmap = null; InvalidateVisual(); return; }

            int frame = Math.Clamp(ActiveFrame, 0, doc.Sprite.FrameCount - 1);
            _compositor.Composite(doc.Sprite, frame);
            _latestBitmap = _compositor.Bitmap;
            InvalidateVisual();
        }

        private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
            => RefreshComposite();

        // ══════════════════════════════════════════════════════════════════
        // Pointer / keyboard input
        // ══════════════════════════════════════════════════════════════════

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            Focus();

            // Middle-mouse button → start pan, don't forward to tool
            if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
            {
                _middlePanning = true;
                _middlePanLastPt = e.GetPosition(this);
                e.Handled = true;
                return;
            }

            var ctx = BuildToolContext();
            if (ctx == null) return;
            _toolRegistry.Get(ActiveToolType).OnPointerPressed(ctx, e);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            // Report cursor position
            var pos = e.GetPosition(this);
            // Always report — even outside sprite bounds
            float spriteX = (float)(pos.X - _cachedOffsetX) / _cachedScale;
            float spriteY = (float)(pos.Y - _cachedOffsetY) / _cachedScale;
            CursorMoved?.Invoke((int)Math.Floor(spriteX), (int)Math.Floor(spriteY));

            // Middle-mouse pan in progress
            if (_middlePanning)
            {
                // Cancel if button was released outside the control
                if (!e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
                {
                    _middlePanning = false;
                    return;
                }

                var current = e.GetPosition(this);
                double dx = current.X - _middlePanLastPt.X;
                double dy = current.Y - _middlePanLastPt.Y;
                _middlePanLastPt = current;
                PanOffset += new Vector(dx, dy);
                e.Handled = true;
                return;
            }

            var ctx = BuildToolContext();
            if (ctx == null) return;
            _toolRegistry.Get(ActiveToolType).OnPointerMoved(ctx, e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (_middlePanning)
            {
                _middlePanning = false;
                e.Handled = true;
                return;
            }

            var ctx = BuildToolContext();
            if (ctx == null) return;
            _toolRegistry.Get(ActiveToolType).OnPointerReleased(ctx, e);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);

            // Shift + scroll → pan horizontally; Control + scroll → pan vertically
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                PanOffset += new Vector(e.Delta.Y * 16, 0);
                e.Handled = true;
                return;
            }
            else if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) {
                PanOffset += new Vector(0, e.Delta.Y * 16);
                e.Handled = true;
                return;
            }

            int current = (int)(Zoom <= 0 ? ComputeAutoZoom() : Zoom);
            if (e.Delta.Y > 0)
                Zoom = Math.Min(ZoomTool.MaxZoom, current + 1);
            else
                Zoom = Math.Max(ZoomTool.MinZoom, current - 1);
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Keyboard shortcuts that live on the canvas (Ctrl+Z / Ctrl+Y / Ctrl+Shift+Z)
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                if (e.Key == Key.Z && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    UndoStack?.Undo();
                    e.Handled = true;
                }
                else if (e.Key == Key.Y ||
                        (e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Shift)))
                {
                    UndoStack?.Redo();
                    e.Handled = true;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // ToolContext factory
        // ══════════════════════════════════════════════════════════════════

        private ToolContext? BuildToolContext()
        {
            var doc = Document;
            if (doc == null) return null;

            var undoStack = UndoStack;
            if (undoStack == null) return null;

            // Resolve active layer (first visible image layer for now)
            Layer? activeLayer = null;
            foreach (var layer in doc.Sprite.GetLayersForCompositing())
            {
                if (layer is LayerImage && layer.IsVisible && layer.IsEditable)
                {
                    activeLayer = layer;
                    break;
                }
            }

            return new ToolContext
            {
                Document = doc,
                UndoStack = undoStack,
                ActiveLayer = activeLayer,
                ActiveFrame = Math.Clamp(ActiveFrame, 0, doc.Sprite.FrameCount - 1),
                ForegroundColor = ForegroundColor,
                BackgroundColor = BackgroundColor,
                CanvasScale = _cachedScale,
                CanvasOffsetX = _cachedOffsetX,
                CanvasOffsetY = _cachedOffsetY,
                Canvas = this,
            };
        }

        // ══════════════════════════════════════════════════════════════════
        // Transform helpers
        // ══════════════════════════════════════════════════════════════════

        private (float scale, float offsetX, float offsetY) ComputeTransform(int bmpW, int bmpH)
        {
            float scale = Zoom <= 0 ? ComputeAutoZoom() : Zoom;
            float offsetX = ((float)Bounds.Width - bmpW * scale) / 2f + (float)PanOffset.X;
            float offsetY = ((float)Bounds.Height - bmpH * scale) / 2f + (float)PanOffset.Y;
            return (scale, offsetX, offsetY);
        }

        private float ComputeAutoZoom()
        {
            if (_latestBitmap == null || Bounds.Width == 0 || Bounds.Height == 0) return 1f;
            float fitX = (float)Bounds.Width / _latestBitmap.Width;
            float fitY = (float)Bounds.Height / _latestBitmap.Height;
            return Math.Max(1f, (float)Math.Floor(Math.Min(fitX, fitY)));
        }

        private void UpdateCursor()
        {
            var ctx = BuildToolContext();
            Cursor = ctx != null
                ? _toolRegistry.Get(ActiveToolType).GetCursor(ctx) ?? Cursor.Default
                : Cursor.Default;
        }

        // ══════════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════════

        private void UnsubscribeDocument()
        {
            if (_subscribedDocument != null)
            {
                _subscribedDocument.Changed -= OnDocumentChanged;
                _subscribedDocument = null;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Draw operation
        // ══════════════════════════════════════════════════════════════════

        private sealed class PixelCanvasDrawOperation : ICustomDrawOperation
        {
            private readonly SKBitmap _bitmap;
            private readonly float _scale, _offsetX, _offsetY;

            public Rect Bounds { get; }

            public PixelCanvasDrawOperation(
                Rect bounds,
                SKBitmap bitmap,
                (float scale, float offsetX, float offsetY) transform)
            {
                Bounds = bounds;
                _bitmap = bitmap;
                (_scale, _offsetX, _offsetY) = transform;
            }

            public void Dispose() { }
            public bool HitTest(Point p) => Bounds.Contains(p);
            public bool Equals(ICustomDrawOperation? other) => false;

            public void Render(ImmediateDrawingContext context)
            {
                var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (lease == null) return;

                using var api = lease.Lease();
                var canvas = api.SkCanvas;
                canvas.Save();

                float spriteW = _bitmap.Width * _scale;
                float spriteH = _bitmap.Height * _scale;

                DrawCheckerboard(canvas, _offsetX, _offsetY, spriteW, spriteH, _scale);

                canvas.Translate(_offsetX, _offsetY);
                canvas.Scale(_scale);

                var sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
                using var paint = new SKPaint { IsAntialias = false };

                var image = SKImage.FromBitmap(_bitmap);
                canvas.DrawImage(image, 0, 0, sampling, paint);

                canvas.Restore();
            }

            private static void DrawCheckerboard(
                SKCanvas canvas,
                float x, float y, float width, float height,
                float scale)
            {
                const float BaseCellPx = 8f;
                float cell = BaseCellPx * scale;
                if (cell < 2f) cell = 2f;

                using var light = new SKPaint { Color = new SKColor(0xA0, 0xA0, 0xA0) };
                using var dark = new SKPaint { Color = new SKColor(0x70, 0x70, 0x70) };

                int cols = (int)Math.Ceiling(width / cell);
                int rows = (int)Math.Ceiling(height / cell);

                for (int row = 0; row < rows; row++)
                    for (int col = 0; col < cols; col++)
                    {
                        float cx = x + col * cell;
                        float cy = y + row * cell;
                        float cw = Math.Min(cell, x + width - cx);
                        float ch = Math.Min(cell, y + height - cy);
                        var rect = new SKRect(cx, cy, cx + cw, cy + ch);
                        canvas.DrawRect(rect, (row + col) % 2 == 0 ? light : dark);
                    }
            }
        }
    }
}
