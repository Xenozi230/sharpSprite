using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SharpSprite.App.ViewModels;
using SkiaSharp;

namespace SharpSprite.App.Controls
{
    /// <summary>
    /// Custom Skia-rendered timeline control.
    /// Draws layer rows on the left and a grid of cel indicators on the right.
    /// </summary>
    public sealed class TimelineSkiaControl : Control
    {
        // ── Styled properties ──────────────────────────────────────────────────
        public static readonly StyledProperty<TimelineViewModel?> ViewModelProperty =
            AvaloniaProperty.Register<TimelineSkiaControl, TimelineViewModel?>(nameof(ViewModel));

        public TimelineViewModel? ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        // ── Layout constants ───────────────────────────────────────────────────
        private const float RowHeight = 22f;
        private const float LayerPanelW = 140f;
        private const float FrameCellW = 18f;
        private const float HeaderH = 20f;
        private const float IconSize = 14f;

        // ── Colors ─────────────────────────────────────────────────────────────
        private static readonly SKColor ColBg = new(0x22, 0x22, 0x22);
        private static readonly SKColor ColRowEven = new(0x26, 0x26, 0x26);
        private static readonly SKColor ColRowOdd = new(0x22, 0x22, 0x22);
        private static readonly SKColor ColRowSelected = new(0x1a, 0x4a, 0x7a);
        private static readonly SKColor ColRowHover = new(0x30, 0x30, 0x30);
        private static readonly SKColor ColSep = new(0x18, 0x18, 0x18);
        private static readonly SKColor ColText = new(0xcc, 0xcc, 0xcc);
        private static readonly SKColor ColTextDim = new(0x66, 0x66, 0x66);
        private static readonly SKColor ColCel = new(0x44, 0x88, 0xdd);
        private static readonly SKColor ColCelBorder = new(0x22, 0x66, 0xbb);
        private static readonly SKColor ColCurFrame = new(0x1a, 0x6f, 0xb5, 0x80);
        private static readonly SKColor ColHeader = new(0x1e, 0x1e, 0x1e);
        private static readonly SKColor ColFrameNum = new(0x77, 0x77, 0x77);
        private static readonly SKColor ColCurrentNum = new(0xff, 0xff, 0xff);

        // ── State ──────────────────────────────────────────────────────────────
        private float _scrollX = 0f;
        private int _hoverRow = -1;

        // ── Construction ───────────────────────────────────────────────────────
        public TimelineSkiaControl()
        {
            ClipToBounds = true;
            IsHitTestVisible = true;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ViewModelProperty)
            {
                if (change.OldValue is TimelineViewModel oldVm)
                {
                    oldVm.PropertyChanged -= OnVmPropertyChanged;
                    oldVm.Layers.CollectionChanged -= OnLayersChanged;
                }
                if (change.NewValue is TimelineViewModel newVm)
                {
                    newVm.PropertyChanged += OnVmPropertyChanged;
                    newVm.Layers.CollectionChanged += OnLayersChanged;
                }
                InvalidateVisual();
            }
        }

        private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            => InvalidateVisual();

        private void OnLayersChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => InvalidateVisual();

        // ── Rendering ──────────────────────────────────────────────────────────
        public override void Render(DrawingContext ctx)
        {
            var vm = ViewModel;
            if (vm == null) return;

            ctx.Custom(new TimelineDrawOp(
                new Rect(Bounds.Size),
                vm,
                _scrollX,
                _hoverRow));
        }

        // ── Input ──────────────────────────────────────────────────────────────
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            var vm = ViewModel;
            if (vm == null) return;

            var pos = e.GetPosition(this);
            int row = RowAtY((float)pos.Y);

            if (row >= 0 && row < vm.Layers.Count)
            {
                vm.SelectLayerCommand.Execute(vm.Layers[row]);

                float ix = (float)pos.X;
                if (ix >= 2 && ix <= 2 + IconSize)
                    vm.ToggleLayerVisibilityCommand.Execute(vm.Layers[row]);
                else if (ix >= 2 + IconSize + 2 && ix <= 2 + IconSize * 2 + 2)
                    vm.ToggleLayerLockCommand.Execute(vm.Layers[row]);
            }
            else if (pos.Y < HeaderH)
            {
                float fx = (float)pos.X - LayerPanelW + _scrollX;
                int frame = (int)(fx / FrameCellW);
                if (frame >= 0 && frame < vm.FrameCount)
                    vm.CurrentFrame = frame;
            }

            InvalidateVisual();
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            var pos = e.GetPosition(this);
            int row = RowAtY((float)pos.Y);
            if (row != _hoverRow)
            {
                _hoverRow = row;
                InvalidateVisual();
            }
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            _hoverRow = -1;
            InvalidateVisual();
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            var vm = ViewModel;
            if (vm == null) return;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) || Math.Abs(e.Delta.X) > 0)
            {
                float maxScroll = Math.Max(0, vm.FrameCount * FrameCellW - ((float)Bounds.Width - LayerPanelW));
                _scrollX = Math.Clamp(_scrollX - (float)(e.Delta.X + e.Delta.Y) * FrameCellW * 2, 0, maxScroll);
                InvalidateVisual();
                e.Handled = true;
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private int RowAtY(float y)
        {
            float rel = y - HeaderH;
            if (rel < 0) return -1;
            return (int)(rel / RowHeight);
        }

        // ── Draw operation ─────────────────────────────────────────────────────
        private sealed class TimelineDrawOp : ICustomDrawOperation
        {
            private readonly TimelineViewModel _vm;
            private readonly float _scrollX;
            private readonly int _hoverRow;
            public Rect Bounds { get; }

            public TimelineDrawOp(Rect bounds, TimelineViewModel vm, float scrollX, int hoverRow)
            {
                Bounds = bounds;
                _vm = vm;
                _scrollX = scrollX;
                _hoverRow = hoverRow;
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
                canvas.ClipRect(new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height));

                DrawBackground(canvas);
                DrawFrameHeader(canvas);
                DrawLayers(canvas);
                DrawCurrentFrameIndicator(canvas);
                DrawPanelBorder(canvas);

                canvas.Restore();
            }

            // ── Background ────────────────────────────────────────────────────
            private void DrawBackground(SKCanvas canvas)
            {
                using var p = new SKPaint { Color = ColBg };
                canvas.DrawRect(0, 0, (float)Bounds.Width, (float)Bounds.Height, p);
            }

            // ── Frame-number header ───────────────────────────────────────────
            private void DrawFrameHeader(SKCanvas canvas)
            {
                using var bgP = new SKPaint { Color = ColHeader };
                canvas.DrawRect(LayerPanelW, 0, (float)Bounds.Width - LayerPanelW, HeaderH, bgP);

                // SKFont owns size/typeface; SKPaint owns color/style
                using var font = new SKFont(SKTypeface.Default, size: 8f);
                using var textP = new SKPaint { Color = ColFrameNum, IsAntialias = true };

                int visibleFrames = (int)((Bounds.Width - LayerPanelW) / FrameCellW) + 2;
                int startFrame = (int)(_scrollX / FrameCellW);

                for (int f = startFrame; f < _vm.FrameCount && f < startFrame + visibleFrames; f++)
                {
                    float fx = LayerPanelW + f * FrameCellW - _scrollX;

                    textP.Color = (f == _vm.CurrentFrame) ? ColCurrentNum : ColFrameNum;

                    if (f % 5 == 0 || FrameCellW >= 16)
                    {
                        string label = (f + 1).ToString();
                        canvas.DrawText(label, fx + 1, HeaderH - 5, SKTextAlign.Left, font, textP);
                    }

                    // Tick mark
                    using var tickP = new SKPaint { Color = ColSep };
                    canvas.DrawLine(fx, HeaderH - 4, fx, HeaderH, tickP);
                }

                // Bottom border
                using var sepP = new SKPaint { Color = ColSep };
                canvas.DrawLine(0, HeaderH, (float)Bounds.Width, HeaderH, sepP);
            }

            // ── All layer rows ────────────────────────────────────────────────
            private void DrawLayers(SKCanvas canvas)
            {
                for (int i = 0; i < _vm.Layers.Count; i++)
                {
                    var layer = _vm.Layers[i];
                    float y = HeaderH + i * RowHeight;

                    SKColor rowBg;
                    if (layer.IsSelected) rowBg = ColRowSelected;
                    else if (i == _hoverRow) rowBg = ColRowHover;
                    else rowBg = (i % 2 == 0) ? ColRowEven : ColRowOdd;

                    using var bgP = new SKPaint { Color = rowBg };
                    canvas.DrawRect(0, y, (float)Bounds.Width, RowHeight - 1, bgP);

                    DrawLayerPanel(canvas, layer, y, i);
                    DrawCelTrack(canvas, layer, y, i);

                    using var sepP = new SKPaint { Color = ColSep };
                    canvas.DrawLine(0, y + RowHeight - 1, (float)Bounds.Width, y + RowHeight - 1, sepP);
                }
            }

            // ── Left panel: icons + layer name ────────────────────────────────
            private void DrawLayerPanel(SKCanvas canvas, LayerRowViewModel layer, float y, int rowIdx)
            {
                float cx = 2f;
                float textY = y + RowHeight - 6;

                // Eye icon (visibility)
                using var eyeFont = new SKFont(SKTypeface.Default, size: 10f);
                using var eyePaint = new SKPaint
                {
                    Color = layer.IsVisible ? new SKColor(0xaa, 0xcc, 0xff) : ColTextDim,
                    IsAntialias = true,
                };
                canvas.DrawText(layer.IsVisible ? "◉" : "○", cx, textY, SKTextAlign.Left, eyeFont, eyePaint);
                cx += IconSize + 2;

                // Lock icon
                using var lockFont = new SKFont(SKTypeface.Default, size: 10f);
                using var lockPaint = new SKPaint
                {
                    Color = layer.IsLocked ? new SKColor(0xff, 0xaa, 0x44) : ColTextDim,
                    IsAntialias = true,
                };
                canvas.DrawText(layer.IsLocked ? "🔒" : "🔓", cx, textY, SKTextAlign.Left, lockFont, lockPaint);
                cx += IconSize + 2;

                // Continuous / linked icon
                using var contFont = new SKFont(SKTypeface.Default, size: 10f);
                using var contPaint = new SKPaint { Color = ColTextDim, IsAntialias = true };
                canvas.DrawText("⬡", cx, textY, SKTextAlign.Left, contFont, contPaint);
                cx += IconSize + 2;

                // Layer name — manual truncation using SKFont.MeasureText
                using var nameFont = new SKFont(SKTypeface.Default, size: 10f);
                using var namePaint = new SKPaint
                {
                    Color = layer.IsSelected ? SKColors.White : ColText,
                    IsAntialias = true,
                };

                float nameMaxWidth = LayerPanelW - cx - 4;
                string name = layer.Name;

                // Truncate until it fits (IsEllipsisText doesn't exist in SKPaint)
                while (name.Length > 0 && nameFont.MeasureText(name) > nameMaxWidth)
                    name = name[..^1];

                canvas.DrawText(name, cx + layer.Depth * 8, textY, SKTextAlign.Left, nameFont, namePaint);

                // Panel right border
                using var borderP = new SKPaint { Color = ColSep };
                canvas.DrawLine(LayerPanelW, y, LayerPanelW, y + RowHeight, borderP);
            }

            // ── Cel blocks on the right track ─────────────────────────────────
            private void DrawCelTrack(SKCanvas canvas, LayerRowViewModel layer, float y, int rowIdx)
            {
                int visibleFrames = (int)((Bounds.Width - LayerPanelW) / FrameCellW) + 2;
                int startFrame = (int)(_scrollX / FrameCellW);

                for (int f = startFrame; f < layer.HasCelAtFrame.Count && f < startFrame + visibleFrames; f++)
                {
                    float fx = LayerPanelW + f * FrameCellW - _scrollX;
                    bool hasCel = f < layer.HasCelAtFrame.Count && layer.HasCelAtFrame[f];

                    if (hasCel)
                    {
                        float margin = 2f;
                        var celRect = new SKRect(fx + margin, y + 3, fx + FrameCellW - margin, y + RowHeight - 4);

                        using var celP = new SKPaint
                        {
                            Color = f == _vm.CurrentFrame ? new SKColor(0x66, 0xaa, 0xff) : ColCel,
                            Style = SKPaintStyle.Fill,
                        };
                        canvas.DrawRoundRect(celRect, 2, 2, celP);

                        using var borderP = new SKPaint
                        {
                            Color = ColCelBorder,
                            Style = SKPaintStyle.Stroke,
                            StrokeWidth = 1,
                        };
                        canvas.DrawRoundRect(celRect, 2, 2, borderP);
                    }

                    // Vertical grid lines every 5 frames
                    if (f % 5 == 0)
                    {
                        using var gridP = new SKPaint
                        {
                            Color = new SKColor(0x33, 0x33, 0x33),
                            StrokeWidth = 1,
                        };
                        canvas.DrawLine(fx, y, fx, y + RowHeight, gridP);
                    }
                }
            }

            // ── Current-frame indicator: triangle + vertical line ─────────────
            private void DrawCurrentFrameIndicator(SKCanvas canvas)
            {
                float fx = LayerPanelW + _vm.CurrentFrame * FrameCellW - _scrollX;
                if (fx < LayerPanelW || fx > Bounds.Width) return;

                float totalH = HeaderH + _vm.Layers.Count * RowHeight;

                // Semi-transparent column highlight
                using var highlightP = new SKPaint { Color = ColCurFrame };
                canvas.DrawRect(fx, HeaderH, FrameCellW, totalH - HeaderH, highlightP);

                // Triangle at top of header
                using var triP = new SKPaint { Color = new SKColor(0x44, 0xaa, 0xff), IsAntialias = true };
                var path = new SKPath();
                float mid = fx + FrameCellW / 2;
                path.MoveTo(mid - 5, 0);
                path.LineTo(mid + 5, 0);
                path.LineTo(mid, 8);
                path.Close();
                canvas.DrawPath(path, triP);

                // Vertical line down through all rows
                using var lineP = new SKPaint
                {
                    Color = new SKColor(0x44, 0xaa, 0xff),
                    StrokeWidth = 1.5f,
                };
                canvas.DrawLine(mid, 8, mid, totalH, lineP);
            }

            // ── Panel/cel divider (heavier stroke) ────────────────────────────
            private void DrawPanelBorder(SKCanvas canvas)
            {
                using var p = new SKPaint { Color = new SKColor(0x10, 0x10, 0x10), StrokeWidth = 2 };
                canvas.DrawLine(LayerPanelW, 0, LayerPanelW, (float)Bounds.Height, p);
            }
        }
    }
}