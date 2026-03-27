using CodeGame.Core;
using CodeGame.Level;
using Terminal.Gui;

namespace CodeGame.UI.Views;

/// <summary>
/// Full-screen level selection menu. Arrow keys navigate, Enter selects a level.
/// Completed levels show a star.
/// </summary>
public class MenuView : View
{
    private readonly ProgressTracker _progress;
    private int _selectedIndex;

    public event Action<int>? LevelSelected;

    public MenuView(ProgressTracker progress)
    {
        _progress = progress;
        CanFocus = true;
    }

    public void RefreshList() => SetNeedsDisplay();

    public override bool ProcessKey(KeyEvent keyEvent)
    {
        switch (keyEvent.Key)
        {
            case Key.CursorUp:
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
                SetNeedsDisplay();
                return true;
            case Key.CursorDown:
                _selectedIndex = Math.Min(LevelGenerator.LevelCount - 1, _selectedIndex + 1);
                SetNeedsDisplay();
                return true;
            case Key.Enter:
                LevelSelected?.Invoke(_selectedIndex + 1);
                return true;
        }
        return base.ProcessKey(keyEvent);
    }

    public override void Redraw(Rect bounds)
    {
        base.Redraw(bounds);

        int cx = bounds.Width / 2;
        int y = 2;

        // ── Title ──────────────────────────────────────────────────
        Driver.SetAttribute(new TAttr(Color.BrightYellow, Color.Black));
        CenterText(cx, y, "CODE GAME");
        y += 2;

        Driver.SetAttribute(new TAttr(Color.DarkGray, Color.Black));
        CenterText(cx, y, new string('\u2500', 32));
        y += 2;

        // ── Level list ─────────────────────────────────────────────
        int listX = cx - 16;

        for (int i = 0; i < LevelGenerator.LevelCount; i++)
        {
            if (y >= bounds.Height - 3) break;

            bool selected  = i == _selectedIndex;
            bool completed = _progress.IsCompleted(i + 1);

            Driver.SetAttribute(selected
                ? new TAttr(Color.Black, Color.White)
                : new TAttr(Color.White, Color.Black));

            string star = completed ? "*" : " ";
            string name = LevelGenerator.GetLevelName(i + 1);
            string line = $"  {star} {i + 1,2}. {name,-25} ";

            Move(Math.Max(0, listX), y);
            Driver.AddStr(line);
            y++;
        }

        // ── Footer ─────────────────────────────────────────────────
        y += 1;
        if (y < bounds.Height)
        {
            Driver.SetAttribute(new TAttr(Color.DarkGray, Color.Black));
            CenterText(cx, y, "Up/Down  Navigate    Enter  Select    Ctrl+Q  Quit");
        }
    }

    private void CenterText(int cx, int y, string text)
    {
        Move(Math.Max(0, cx - text.Length / 2), y);
        Driver.AddStr(text);
    }
}
