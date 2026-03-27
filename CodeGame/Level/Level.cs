namespace CodeGame.Level;

public class Level
{
    public int Width { get; }
    public int Height { get; }
    private readonly TileType[,] _tiles;

    public (int X, int Y) StartPosition { get; }
    public (int X, int Y) CheckpointPosition { get; }

    public Level(TileType[,] tiles, (int, int) start, (int, int) checkpoint)
    {
        _tiles = tiles;
        Width = tiles.GetLength(0);
        Height = tiles.GetLength(1);
        StartPosition = start;
        CheckpointPosition = checkpoint;
    }

    public TileType GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return TileType.Empty;
        return _tiles[x, y];
    }

    public bool IsSolid(int x, int y) =>
        GetTile(x, y) == TileType.Platform ||
        GetTile(x, y) == TileType.Start ||
        GetTile(x, y) == TileType.Checkpoint;

    public bool IsSpike(int x, int y) => GetTile(x, y) == TileType.Spike;

    public bool IsCheckpoint(int x, int y) => GetTile(x, y) == TileType.Checkpoint;
}
