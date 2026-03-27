using CodeGame.Blocks;
using Terminal.Gui;

namespace CodeGame.UI.Views;

/// <summary>
/// Right panel: the ordered list of blocks the player has assembled.
/// Supports drag-to-reorder and drag-to-delete. Repeat blocks act as containers —
/// they show as a single block when empty and expand when children are dropped inside.
/// </summary>
public class BlockSequenceView : View
{
    public List<CodeBlock> Blocks { get; } = new();

    // Visual row currently highlighted as a drop target (-1 = none)
    public int DropTargetIndex { get; set; } = -1;

    /// <summary>Where to insert a dropped block: a target list and an index within it.</summary>
    public record DropSlot(List<CodeBlock> List, int Index);

    /// <summary>Fired when user starts dragging a block from the sequence.
    /// Args: (block, ownerList, indexInOwnerList, mouseEvent)</summary>
    public event Action<CodeBlock, List<CodeBlock>, int, MouseEvent>? BlockReorderStarted;

    // ── Draw plan ──────────────────────────────────────────────────
    private enum RowKind { Block, RepeatHeader, RepeatFooter, DropZone }

    private record DrawRow(
        RowKind Kind,
        CodeBlock? Block,
        int Depth,
        List<CodeBlock> DropList,
        int DropIndex,
        List<CodeBlock>? SourceList,
        int SourceIndex
    );

    private readonly List<DrawRow> _rows = new();

    // Same label width as the palette: "[ DisplayNm  ]" = 14 chars
    private const int LabelWidth = BlockPaletteView.BlockLabelWidth;

    public BlockSequenceView()
    {
        CanFocus = false;
        WantMousePositionReports = true;
    }

    // ── Hit testing ────────────────────────────────────────────────

    public override bool MouseEvent(MouseEvent me)
    {
        if (me.Flags.HasFlag(MouseFlags.Button1Pressed))
        {
            int row = me.Y - 1;
            if (row >= 0 && row < _rows.Count)
            {
                var r = _rows[row];
                if (r.SourceList != null && r.Block != null)
                    BlockReorderStarted?.Invoke(r.Block, r.SourceList, r.SourceIndex, me);
            }
        }
        return true;
    }

    /// <summary>Returns the visual row index for a given absolute screen Y.</summary>
    public int VisualRowAtScreenY(int screenY)
    {
        int localY = screenY - Frame.Y;
        return localY - 1;
    }

    /// <summary>Returns the drop target (list + index) for a given absolute screen Y, or null.</summary>
    public DropSlot? GetDropSlotAtScreenY(int screenY)
    {
        int row = VisualRowAtScreenY(screenY);
        if (row < 0 || row >= _rows.Count) return null;
        var r = _rows[row];
        return new DropSlot(r.DropList, r.DropIndex);
    }

    // ── Draw plan construction ─────────────────────────────────────

    private void BuildRows()
    {
        _rows.Clear();
        BuildRowsFrom(Blocks, 0);
        _rows.Add(new DrawRow(RowKind.DropZone, null, 0,
            Blocks, Blocks.Count, null, -1));
    }

    private void BuildRowsFrom(List<CodeBlock> blocks, int depth)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];

            if (block.Type == BlockType.Repeat)
            {
                _rows.Add(new DrawRow(RowKind.RepeatHeader, block, depth,
                    block.Children, 0,
                    blocks, i));

                if (block.Children.Count > 0)
                {
                    BuildRowsFrom(block.Children, depth + 1);

                    _rows.Add(new DrawRow(RowKind.DropZone, null, depth + 1,
                        block.Children, block.Children.Count, null, -1));

                    _rows.Add(new DrawRow(RowKind.RepeatFooter, block, depth,
                        blocks, i + 1, null, -1));
                }
            }
            else
            {
                _rows.Add(new DrawRow(RowKind.Block, block, depth,
                    blocks, i,
                    blocks, i));
            }
        }
    }

    // ── Rendering ──────────────────────────────────────────────────

    public override void Redraw(Rect bounds)
    {
        base.Redraw(bounds);
        BuildRows();

        Driver.SetAttribute(new TAttr(Color.White, Color.Black));
        Move(1, 0);
        Driver.AddStr("Your Program");

        for (int i = 0; i < _rows.Count; i++)
        {
            int y = i + 1;
            if (y >= bounds.Height) break;

            var row = _rows[i];
            int x = 1 + row.Depth * 2;

            if (i == DropTargetIndex)
            {
                Driver.SetAttribute(new TAttr(Color.White, Color.DarkGray));
                Move(x, y);
                Driver.AddStr(new string('\u2500', LabelWidth));
            }

            switch (row.Kind)
            {
                case RowKind.Block:
                    DrawBlockLabel(x, y, row.Block!);
                    break;

                case RowKind.RepeatHeader:
                    if (row.Block!.Children.Count == 0)
                        DrawBlockLabel(x, y, row.Block);
                    else
                        DrawRepeatHeader(x, y, row.Block);
                    break;

                case RowKind.RepeatFooter:
                    DrawRepeatFooter(x, y);
                    break;

                case RowKind.DropZone:
                    DrawDropZone(x, y, i == DropTargetIndex);
                    break;
            }
        }
    }

    private void DrawBlockLabel(int x, int y, CodeBlock block)
    {
        var attr = BlockPaletteView.BlockColor(block.Type);
        Driver.SetAttribute(attr);
        Move(x, y);
        Driver.AddStr($"[ {block.DisplayName,-10} ]");
    }

    private void DrawRepeatHeader(int x, int y, CodeBlock block)
    {
        var attr = BlockPaletteView.BlockColor(BlockType.Repeat);
        Driver.SetAttribute(attr);
        Move(x, y);
        string title = $"Repeat x{block.RepeatCount}";
        Driver.AddStr($"\u250c {title,-10} \u2510");
    }

    private void DrawRepeatFooter(int x, int y)
    {
        var attr = BlockPaletteView.BlockColor(BlockType.Repeat);
        Driver.SetAttribute(attr);
        Move(x, y);
        Driver.AddStr("\u2514" + new string('\u2500', 12) + "\u2518");
    }

    private void DrawDropZone(int x, int y, bool highlighted)
    {
        Driver.SetAttribute(highlighted
            ? new TAttr(Color.White, Color.DarkGray)
            : new TAttr(Color.DarkGray, Color.Black));
        Move(x, y);
        Driver.AddStr("[ + drop here ]");
    }
}
