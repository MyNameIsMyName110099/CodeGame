using CodeGame.Blocks;
using Terminal.Gui;

namespace CodeGame.UI.Views;

/// <summary>
/// Right panel: the ordered list of blocks the player has assembled.
/// Blocks snap into numbered slots. Supports drag-to-reorder and drag-to-delete.
/// </summary>
public class BlockSequenceView : View
{
    public List<CodeBlock> Blocks { get; } = new();

    // Index of the slot currently highlighted as a drop target (-1 = none)
    public int DropTargetIndex { get; set; } = -1;

    // Fired when user starts dragging an existing block from the sequence
    public event Action<int, MouseEvent>? BlockReorderStarted;

    public BlockSequenceView()
    {
        CanFocus = false;
        WantMousePositionReports = true;
    }

    public override bool MouseEvent(MouseEvent me)
    {
        if (me.Flags.HasFlag(MouseFlags.Button1Pressed))
        {
            int index = SlotAtY(me.Y);
            if (index >= 0 && index < Blocks.Count)
                BlockReorderStarted?.Invoke(index, me);
        }
        return true;
    }

    /// <summary>Returns the slot index that corresponds to a y coordinate, or -1 if none.</summary>
    public int SlotAtY(int y)
    {
        // Slots start at y=1, one per row
        int idx = y - 1;
        return (idx >= 0 && idx <= Blocks.Count) ? idx : -1;
    }

    public int SlotAtScreenY(int screenY)
    {
        int localY = screenY - Frame.Y;
        return SlotAtY(localY);
    }

    public override void Redraw(Rect bounds)
    {
        base.Redraw(bounds);

        Driver.SetAttribute(new TAttr(Color.White, Color.Black));
        Move(1, 0);
        Driver.AddStr("Your Program");

        int innerWidth = bounds.Width - 2; // usable width between the left/right margins
        int depth = 0;

        for (int i = 0; i < Blocks.Count; i++)
        {
            int y = i + 1;
            if (y >= bounds.Height) break;

            // Drop-target indicator line
            if (i == DropTargetIndex)
            {
                Driver.SetAttribute(new TAttr(Color.White, Color.DarkGray));
                Move(1, y);
                Driver.AddStr(new string('─', innerWidth));
            }

            var block = Blocks[i];
            var attr  = BlockPaletteView.BlockColor(block.Type);

            if (block.Type == BlockType.Repeat)
            {
                // ┌─ Repeat x2 ──────┐
                string title   = $" Repeat x{block.RepeatCount} ";
                int    dashes  = Math.Max(0, innerWidth - 2 - title.Length - 2);
                string line    = "┌─" + title + new string('─', dashes) + "┐";
                Driver.SetAttribute(attr);
                Move(1, y);
                Driver.AddStr(line.PadRight(innerWidth));
                depth++;
            }
            else if (block.Type == BlockType.End)
            {
                // └──────────────────┘
                if (depth > 0) depth--;
                var repeatAttr = BlockPaletteView.BlockColor(BlockType.Repeat);
                string line = "└" + new string('─', innerWidth - 2) + "┘";
                Driver.SetAttribute(repeatAttr);
                Move(1, y);
                Driver.AddStr(line.PadRight(innerWidth));
            }
            else
            {
                string label = $"[ {block.DisplayName,-12} ]";
                Driver.SetAttribute(attr);

                if (depth > 0)
                {
                    // │  [ Walk         ]  │
                    int padding = innerWidth - 2 - 1 - label.Length - 1; // borders + spaces
                    string line = "│ " + label + new string(' ', Math.Max(0, padding)) + " │";
                    Move(1, y);
                    Driver.AddStr(line.PadRight(innerWidth));
                }
                else
                {
                    Move(1, y);
                    Driver.AddStr(label.PadRight(innerWidth));
                }
            }
        }

        // Drop zone at the bottom
        int dropY = Blocks.Count + 1;
        if (dropY < bounds.Height)
        {
            bool isTarget = DropTargetIndex == Blocks.Count;
            Driver.SetAttribute(isTarget
                ? new TAttr(Color.White, Color.DarkGray)
                : new TAttr(Color.DarkGray, Color.Black));
            Move(1, dropY);
            Driver.AddStr("[ + drop here ]");
        }
    }
}
