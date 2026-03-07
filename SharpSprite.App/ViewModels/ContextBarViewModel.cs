using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpSprite.App.Tools;

namespace SharpSprite.App.ViewModels
{
    public partial class ContextBarViewModel : ObservableObject
    {
        // ── Context visibility ────────────────────────────────────────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowPencilContext))]
        [NotifyPropertyChangedFor(nameof(ShowEraserContext))]
        [NotifyPropertyChangedFor(nameof(ShowSelectionContext))]
        [NotifyPropertyChangedFor(nameof(ShowZoomContext))]
        [NotifyPropertyChangedFor(nameof(ShowShapeContext))]
        [NotifyPropertyChangedFor(nameof(ShowFillContext))]
        private ToolType _activeTool = ToolType.Pencil;

        public bool ShowPencilContext => ActiveTool == ToolType.Pencil;
        public bool ShowEraserContext => ActiveTool == ToolType.Eraser;
        public bool ShowSelectionContext => ActiveTool == ToolType.Selection;
        public bool ShowZoomContext => ActiveTool == ToolType.Zoom;
        public bool ShowShapeContext => ActiveTool is ToolType.Line or ToolType.Rectangle or ToolType.Ellipse;
        public bool ShowFillContext => ActiveTool == ToolType.Fill;

        // ── Pencil / Eraser options ───────────────────────────────────────
        [ObservableProperty] private int _brushSize = 1;
        [ObservableProperty] private bool _pixelPerfect = true;
        [ObservableProperty] private bool _contiguous = true;

        // Brush shape: "circle", "square"
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCircleBrush))]
        [NotifyPropertyChangedFor(nameof(IsSquareBrush))]
        private string _brushShape = "circle";

        public bool IsCircleBrush => BrushShape == "circle";
        public bool IsSquareBrush => BrushShape == "square";

        // ── Shape options ─────────────────────────────────────────────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFilled))]
        [NotifyPropertyChangedFor(nameof(IsOutlineOnly))]
        private bool _filledShape = false;

        public bool IsFilled => FilledShape;
        public bool IsOutlineOnly => !FilledShape;

        // ── Zoom ─────────────────────────────────────────────────────────
        [ObservableProperty] private int _currentZoom = 1;
        public string ZoomText => $"{CurrentZoom * 100}%";
        partial void OnCurrentZoomChanged(int value) => OnPropertyChanged(nameof(ZoomText));

        // ── Opacity ──────────────────────────────────────────────────────
        [ObservableProperty] private int _toolOpacity = 100;

        // ── Commands ──────────────────────────────────────────────────────
        [RelayCommand]
        public void SetCircleBrush() => BrushShape = "circle";

        [RelayCommand]
        public void SetSquareBrush() => BrushShape = "square";

        [RelayCommand]
        public void TogglePixelPerfect() => PixelPerfect = !PixelPerfect;

        [RelayCommand]
        public void ToggleFilled() => FilledShape = !FilledShape;

        [RelayCommand]
        public void IncreaseBrush() { if (BrushSize < 64) BrushSize++; }

        [RelayCommand]
        public void DecreaseBrush() { if (BrushSize > 1) BrushSize--; }
    }
}
