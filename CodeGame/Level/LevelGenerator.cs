namespace CodeGame.Level;

public static class LevelGenerator
{
    public const int LevelCount = 20;

    private record Theme(string Name, TileType Platform, TileType Hazard, TileType PitHazard);

    private static readonly Theme[] Themes =
    [
        new("Classic",  TileType.Platform, TileType.Spike,  TileType.Spike),
        new("Forest",   TileType.Grass,    TileType.Thorns, TileType.Thorns),
        new("Cave",     TileType.Stone,    TileType.Lava,   TileType.Lava),
        new("Beach",    TileType.Sand,     TileType.Water,  TileType.Water),
        new("Factory",  TileType.Metal,    TileType.Fire,   TileType.Fire),
    ];

    public static string GetLevelName(int levelNumber)
    {
        var theme = Themes[(levelNumber - 1) % Themes.Length];
        return $"Level {levelNumber} - {theme.Name}";
    }

    // ── Segment definition ─────────────────────────────────────────
    // Type: S=start, P=platform, G=gap, T=trap(crumble), E=end
    private record Segment(char Type, int Width, int GroundY);

    public static Level Generate(int levelNumber)
    {
        var rng = new Random(levelNumber * 31337);
        const int baseGroundY = 15;
        const int height = 20;

        var theme = Themes[(levelNumber - 1) % Themes.Length];

        // ── Difficulty parameters ──────────────────────────────────
        int numGaps = Math.Clamp(2 + (levelNumber + 1) / 2, 3, 10);
        int minGap  = 3;
        int maxGap  = Math.Clamp(3 + levelNumber / 5, 3, 6);
        int minPlat = Math.Clamp(7 - levelNumber / 3, 2, 6);
        int maxPlat = Math.Clamp(9 - levelNumber / 4, minPlat + 1, 8);

        // Height variation starts at level 6
        bool useVerticality = levelNumber >= 6;
        int heightChance = Math.Clamp(20 + (levelNumber - 6) * 5, 0, 60);

        // ── Build segments with height tracking ────────────────────
        var segments = new List<Segment>();
        int currentGroundY = baseGroundY;

        // Start platform — always at base height
        segments.Add(new Segment('S', rng.Next(5, 8), baseGroundY));

        for (int i = 0; i < numGaps; i++)
        {
            // Gap or crumble trap (at the current height)
            bool isTrap = levelNumber >= 8 && rng.Next(100) < 25;
            int gapWidth = rng.Next(minGap, maxGap + 1);
            segments.Add(new Segment(isTrap ? 'T' : 'G', gapWidth, currentGroundY));

            // Landing platform (skip after last gap — end platform follows)
            if (i < numGaps - 1)
            {
                int platWidth = rng.Next(minPlat, maxPlat + 1);

                // Possibly shift the platform height
                int newGroundY = currentGroundY;
                if (useVerticality && rng.Next(100) < heightChance)
                {
                    int shift = rng.Next(-2, 3); // -2, -1, 0, +1, +2
                    newGroundY = Math.Clamp(currentGroundY + shift, 12, 17);
                }
                segments.Add(new Segment('P', platWidth, newGroundY));
                currentGroundY = newGroundY;
            }
        }

        // End platform — stays at current height so the final jump is fair
        segments.Add(new Segment('E', rng.Next(6, 10), currentGroundY));

        // ── Allocate tile array ────────────────────────────────────
        int totalWidth = 0;
        foreach (var s in segments) totalWidth += s.Width;

        var tiles = new TileType[totalWidth, height];

        // ── Fill tiles ─────────────────────────────────────────────
        int cursor = 0;
        int checkpointX = totalWidth - 4;
        var platformRanges = new List<(int Start, int End, int GroundY)>();

        foreach (var seg in segments)
        {
            int segEnd = Math.Min(cursor + seg.Width, totalWidth);

            for (int x = cursor; x < segEnd; x++)
            {
                switch (seg.Type)
                {
                    case 'S':
                        tiles[x, seg.GroundY] = x == cursor ? TileType.Start : theme.Platform;
                        break;

                    case 'P':
                        tiles[x, seg.GroundY] = theme.Platform;
                        break;

                    case 'G':
                        // Fill hazards from groundY down to the bottom
                        for (int gy = seg.GroundY; gy < height; gy++)
                            tiles[x, gy] = gy == seg.GroundY ? theme.Hazard : theme.PitHazard;
                        break;

                    case 'T':
                        // Crumble tile at ground level, hazards below
                        tiles[x, seg.GroundY] = TileType.Crumble;
                        for (int gy = seg.GroundY + 1; gy < height; gy++)
                            tiles[x, gy] = theme.PitHazard;
                        break;

                    case 'E':
                        tiles[x, seg.GroundY] = TileType.Checkpoint;
                        checkpointX = cursor + seg.Width / 2;
                        break;
                }
            }

            if (seg.Type is 'P' or 'S')
                platformRanges.Add((cursor, segEnd - 1, seg.GroundY));

            cursor = segEnd;
        }

        // ── Fill empty space below platforms with pit hazards ───────
        for (int x = 0; x < totalWidth; x++)
        {
            bool foundGround = false;
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] != TileType.Empty)
                {
                    foundGround = true;
                    continue;
                }
                if (foundGround && y >= 17)
                    tiles[x, y] = theme.PitHazard;
            }
        }

        // ── Wall obstacles (higher difficulty) ─────────────────────
        if (levelNumber >= 5)
        {
            foreach (var (pStart, pEnd, pGroundY) in platformRanges)
            {
                int pWidth = pEnd - pStart + 1;
                if (pWidth < 4 || rng.Next(100) >= 30) continue;

                int wallX = pStart + rng.Next(1, pWidth - 1);
                int wallY = pGroundY - 1;

                int landX = wallX + 6;
                if (landX < totalWidth)
                {
                    bool canLand = false;
                    for (int ly = wallY - 3; ly <= wallY + 3; ly++)
                    {
                        if (ly >= 0 && ly < height && IsSolidAt(tiles, landX, ly))
                        { canLand = true; break; }
                    }
                    if (canLand)
                        tiles[wallX, wallY] = TileType.Wall;
                }
            }
        }

        checkpointX = Math.Clamp(checkpointX, 0, totalWidth - 1);
        int startGroundY = segments[0].GroundY;
        int endGroundY = segments[^1].GroundY;

        return new Level(tiles,
            (2, startGroundY - 1),
            (checkpointX, endGroundY - 1));
    }

    private static bool IsSolidAt(TileType[,] tiles, int x, int y)
    {
        if (x < 0 || x >= tiles.GetLength(0) || y < 0 || y >= tiles.GetLength(1))
            return false;
        var t = tiles[x, y];
        return t is TileType.Platform or TileType.Start or TileType.Checkpoint
            or TileType.Stone or TileType.Grass or TileType.Sand
            or TileType.Wood or TileType.Metal or TileType.Bridge
            or TileType.Wall;
    }
}
