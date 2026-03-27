namespace CodeGame.Level;

public static class LevelData
{
    // Level: 58 wide x 20 tall
    // Character stands at y=14, platforms sit at y=15
    // Walk = 2 tiles, Jump = 7 tiles right
    // Gap width = 5 tiles → jump clears them with 2 tiles of margin
    //
    // Layout (y=15 row):
    //   Platform 1 (start): x=0..8
    //   Spike gap 1:        x=9..13
    //   Platform 2:         x=14..22
    //   Spike gap 2:        x=23..27
    //   Platform 3:         x=28..36
    //   Spike gap 3:        x=37..41
    //   Platform 4 (end):   x=42..57  (checkpoint at x=49..57)
    public static Level Level1()
    {
        const int W = 58;
        const int H = 20;
        var tiles = new TileType[W, H];

        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
                tiles[x, y] = TileType.Empty;

        // Platform 1 (start)
        FillRow(tiles, 0, 8, 15, TileType.Platform);
        tiles[0, 15] = TileType.Start;

        // Gap 1
        FillRow(tiles, 9, 13, 15, TileType.Spike);

        // Platform 2
        FillRow(tiles, 14, 22, 15, TileType.Platform);

        // Gap 2
        FillRow(tiles, 23, 27, 15, TileType.Spike);

        // Platform 3
        FillRow(tiles, 28, 36, 15, TileType.Platform);

        // Gap 3
        FillRow(tiles, 37, 41, 15, TileType.Spike);

        // Platform 4 — right half is all checkpoint so any landing there wins
        FillRow(tiles, 42, 48, 15, TileType.Platform);
        FillRow(tiles, 49, 57, 15, TileType.Checkpoint);

        // Bottom pit spikes
        for (int y = 16; y < H; y++)
            FillRow(tiles, 0, W - 1, y, TileType.Spike);

        return new Level(tiles, (2, 14), (53, 14));
    }

    private static void FillRow(TileType[,] tiles, int x1, int x2, int y, TileType type)
    {
        for (int x = x1; x <= x2; x++)
            tiles[x, y] = type;
    }
}
