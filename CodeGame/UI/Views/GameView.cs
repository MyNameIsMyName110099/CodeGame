using CodeGame.Entities;
using CodeGame.Level;
using Terminal.Gui;

namespace CodeGame.UI.Views;

public class GameView : View
{
    private Level.Level? _level;
    private Character? _character;

    public GameView()
    {
        CanFocus = false;
    }

    public void SetLevel(Level.Level level, Character character)
    {
        _level = level;
        _character = character;
        SetNeedsDisplay();
    }

    public void Refresh(Character character)
    {
        _character = character;
        SetNeedsDisplay();
    }

    public override void Redraw(Rect bounds)
    {
        base.Redraw(bounds);
        if (_level == null || _character == null) return;

        for (int y = 0; y < _level.Height && y < bounds.Height; y++)
        {
            for (int x = 0; x < _level.Width && x < bounds.Width; x++)
            {
                int charX = (int)Math.Round(_character.X);
                int charY = (int)Math.Round(_character.Y);

                if (x == charX && y == charY)
                {
                    Driver.SetAttribute(new TAttr(Color.BrightYellow, Color.Black));
                    AddRune(x, y, '@');
                    continue;
                }

                var tile = _level.GetTile(x, y);
                var (attr, ch) = TileVisual(tile);
                Driver.SetAttribute(attr);
                AddRune(x, y, ch);
            }
        }
    }

    private static (TAttr attr, char ch) TileVisual(TileType tile) => tile switch
    {
        // ── Core tiles ─────────────────────────────────────────
        TileType.Platform   => (new TAttr(Color.Gray,        Color.Black), '='),
        TileType.Start      => (new TAttr(Color.Green,       Color.Black), '='),
        TileType.Spike      => (new TAttr(Color.BrightRed,   Color.Black), '^'),
        TileType.Checkpoint => (new TAttr(Color.BrightGreen,  Color.Black), 'E'),

        // ── Platform variants ──────────────────────────────────
        TileType.Stone      => (new TAttr(Color.Gray,        Color.Black), '#'),
        TileType.Grass      => (new TAttr(Color.Green,       Color.Black), '"'),
        TileType.Sand       => (new TAttr(Color.BrightYellow,Color.Black), '.'),
        TileType.Wood       => (new TAttr(Color.Brown,       Color.Black), '='),
        TileType.Metal      => (new TAttr(Color.BrightCyan,  Color.Black), '='),
        TileType.Bridge     => (new TAttr(Color.DarkGray,    Color.Black), '-'),

        // ── Hazard variants ────────────────────────────────────
        TileType.Lava       => (new TAttr(Color.Brown,       Color.Black), '~'),
        TileType.Water      => (new TAttr(Color.BrightCyan,  Color.Black), '~'),
        TileType.Thorns     => (new TAttr(Color.Magenta,     Color.Black), '*'),
        TileType.Fire       => (new TAttr(Color.BrightRed,   Color.Black), '^'),

        // ── Special ────────────────────────────────────────────
        TileType.Crumble    => (new TAttr(Color.DarkGray,    Color.Black), ':'),
        TileType.Wall       => (new TAttr(Color.White,       Color.Black), '#'),

        // ── Empty ──────────────────────────────────────────────
        _                   => (new TAttr(Color.Black,       Color.Black), ' '),
    };

    private void AddRune(int x, int y, char c)
    {
        Move(x, y);
        Driver.AddRune(c);
    }
}
