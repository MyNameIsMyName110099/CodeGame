namespace CodeGame.Level;

public enum TileType
{
    Empty,
    Platform,
    Spike,
    Checkpoint,
    Start,

    // Platform variants (solid ground, visual variety)
    Stone,
    Grass,
    Sand,
    Wood,
    Metal,
    Bridge,

    // Hazard variants (lethal, visual variety)
    Lava,
    Water,
    Thorns,
    Fire,

    // Special
    Crumble, // looks like a platform but character falls through
    Wall     // solid at character height, blocks walking — must jump over
}
