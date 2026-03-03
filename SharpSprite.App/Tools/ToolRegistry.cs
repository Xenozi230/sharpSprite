using System.Collections.Generic;

namespace SharpSprite.App.Tools
{
    /// <summary>
    /// Holds a singleton instance of every <see cref="ITool"/>.
    /// Unknown tool types fall back to the pencil tool.
    /// </summary>
    public sealed class ToolRegistry
    {
        private readonly Dictionary<ToolType, ITool> _tools;
        private readonly ITool _fallback;

        public ToolRegistry()
        {
            var pencil = new PencilTool();
            _fallback = pencil;

            _tools = new Dictionary<ToolType, ITool>
            {
                [ToolType.Pencil] = pencil,
                [ToolType.Eraser] = new EraserTool(),
                [ToolType.Pan] = new PanTool(),
                [ToolType.Zoom] = new ZoomTool(),
                // Placeholder – these tools will be implemented later;
                // they are registered as no-ops so ToolRegistry doesn't throw.
                [ToolType.Eyedropper] = new NoOpTool(ToolType.Eyedropper),
                [ToolType.Fill] = new NoOpTool(ToolType.Fill),
                [ToolType.Selection] = new NoOpTool(ToolType.Selection),
                [ToolType.Line] = new NoOpTool(ToolType.Line),
                [ToolType.Rectangle] = new NoOpTool(ToolType.Rectangle),
                [ToolType.Ellipse] = new NoOpTool(ToolType.Ellipse),
            };
        }

        public ITool Get(ToolType type)
            => _tools.TryGetValue(type, out var tool) ? tool : _fallback;
    }

    /// <summary>Placeholder tool that does nothing. Used for not-yet-implemented tools.</summary>
    internal sealed class NoOpTool : ITool
    {
        public ToolType Type { get; }
        public NoOpTool(ToolType type) => Type = type;
        public Avalonia.Input.Cursor? GetCursor(ToolContext ctx)
            => new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Cross);
        public void OnPointerPressed(ToolContext ctx, Avalonia.Input.PointerPressedEventArgs e) { }
        public void OnPointerMoved(ToolContext ctx, Avalonia.Input.PointerEventArgs e) { }
        public void OnPointerReleased(ToolContext ctx, Avalonia.Input.PointerReleasedEventArgs e) { }
    }
}
