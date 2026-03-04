using CommunityToolkit.Mvvm.ComponentModel;

namespace SharpSprite.App.ViewModels
{
    public partial class StatusBarViewModel : ObservableObject
    {
        [ObservableProperty] private int _cursorX = 0;
        [ObservableProperty] private int _cursorY = 0;

        [ObservableProperty] private int _spriteWidth = 32;
        [ObservableProperty] private int _spriteHeight = 32;

        [ObservableProperty] private int _zoom = 1;

        [ObservableProperty] private string _colorMode = "RGBA";

        [ObservableProperty] private string _statusMessage = "Ready";

        [ObservableProperty] private int _currentFrame = 1;
        [ObservableProperty] private int _totalFrames = 1;
        [ObservableProperty] private int _fps = 10;

        public string CursorLabel => $"+ {CursorX} {CursorY}";
        public string SizeLabel => $"{SpriteWidth} {SpriteHeight}";
        public string ZoomLabel => $"{Zoom * 100}%";
        public string FrameLabel => $"Frame: {CurrentFrame}";

        partial void OnCursorXChanged(int value) => OnPropertyChanged(nameof(CursorLabel));
        partial void OnCursorYChanged(int value) => OnPropertyChanged(nameof(CursorLabel));
        partial void OnSpriteWidthChanged(int value) => OnPropertyChanged(nameof(SizeLabel));
        partial void OnSpriteHeightChanged(int value) => OnPropertyChanged(nameof(SizeLabel));
        partial void OnZoomChanged(int value) => OnPropertyChanged(nameof(ZoomLabel));
        partial void OnCurrentFrameChanged(int value) => OnPropertyChanged(nameof(FrameLabel));
    }
}
