namespace CodeGame.Entities;

public class Character
{
    public float X { get; set; }
    public float Y { get; set; }
    public bool IsAlive { get; set; } = true;
    public bool ReachedCheckpoint { get; set; } = false;

    private readonly float _startX;
    private readonly float _startY;

    public Character(float startX, float startY)
    {
        _startX = startX;
        _startY = startY;
        X = startX;
        Y = startY;
    }

    public void Respawn()
    {
        X = _startX;
        Y = _startY;
        IsAlive = true;
        ReachedCheckpoint = false;
    }

    public int TileX => (int)Math.Round(X);
    public int TileY => (int)Math.Round(Y);
}
