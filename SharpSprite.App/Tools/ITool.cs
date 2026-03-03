using Avalonia.Input;

namespace SharpSprite.App.Tools
{
    /// <summary>
    /// A tool that responds to pointer events on the canvas.
    /// </summary>
    public interface ITool
    {
        ToolType Type { get; }
        void OnPointerPressed(ToolContext ctx, PointerPressedEventArgs e);
        void OnPointerMoved(ToolContext ctx, PointerEventArgs e);
        void OnPointerReleased(ToolContext ctx, PointerReleasedEventArgs e);
        Cursor? GetCursor(ToolContext ctx);
    }
}
