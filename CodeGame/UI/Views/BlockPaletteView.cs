using CodeGame.Blocks;
using Terminal.Gui;

namespace CodeGame.UI.Views;

/// <summary>
/// Left panel showing available block types to drag from.
/// </summary>
public class BlockPaletteView : View
{
    public static readonly BlockType[] AvailableBlocks =
    [
        BlockType.Walk,
        BlockType.Jump,
        BlockType.Repeat,
        BlockType.End,
        BlockType.Pause
    ];

    // Fired when the user starts dragging a block from the palette
    public event Action<BlockType, MouseEvent>? BlockDragStarted;

    public BlockPaletteView()
    {
        CanFocus = false;
        WantMousePositionReports = true;
    }

    // Label format: "[ Walk       ]" — 2 + 10 + 2 = 14 chars, drawn at x=1
    private const int BlockLabelX = 1;
    private const int BlockLabelWidth = 14;

    public override bool MouseEvent(MouseEvent me)
    {
        if (me.Flags.HasFlag(MouseFlags.Button1Pressed))
        {
            // Blocks drawn at y = 1 + i (title at y=0)
            int index = me.Y - 1;
            bool inXBounds = me.X >= BlockLabelX && me.X < BlockLabelX + BlockLabelWidth;
            if (inXBounds && index >= 0 && index < AvailableBlocks.Length)
                BlockDragStarted?.Invoke(AvailableBlocks[index], me);
        }
        return true;
    }

    public override void Redraw(Rect bounds)
    {
        base.Redraw(bounds);
        // Title at y=0, blocks at y=1,2,3...
        DrawTitle(0, "Blocks");

        for (int i = 0; i < AvailableBlocks.Length; i++)
        {
            var block = new CodeBlock(AvailableBlocks[i]);
            DrawBlock(i + 1, block);
        }
    }

    private void DrawTitle(int y, string title)
    {
        Driver.SetAttribute(new TAttr(Color.White, Color.Black));
        Move(BlockLabelX, y);
        Driver.AddStr(title);
    }

    private void DrawBlock(int y, CodeBlock block)
    {
        if (y >= Frame.Height) return;
        var attr = BlockColor(block.Type);
        Driver.SetAttribute(attr);
        Move(BlockLabelX, y);
        string label = $"[ {block.DisplayName,-10} ]";
        Driver.AddStr(label);
    }

    internal static TAttr BlockColor(BlockType type) => type switch
    {
        BlockType.Walk   => new TAttr(Color.Black, Color.BrightCyan),
        BlockType.Jump   => new TAttr(Color.Black, Color.BrightGreen),
        BlockType.Repeat => new TAttr(Color.Black, Color.BrightYellow),
        BlockType.End    => new TAttr(Color.Black, Color.Brown),
        BlockType.Pause  => new TAttr(Color.Black, Color.Magenta),
        _                => new TAttr(Color.White, Color.Black)
    };
}
