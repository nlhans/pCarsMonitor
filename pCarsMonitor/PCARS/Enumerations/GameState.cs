using System.ComponentModel;

namespace pCarsTelemetry.Enumerations
{
    public enum GameState
    {
        [Description("Waiting for game to start...")]
        GAME_EXITED = 0,
        [Description("In Menus")]
        GAME_FRONT_END,
        [Description("In Session")]
        GAME_INGAME_PLAYING,
        [Description("Game Paused")]
        GAME_INGAME_PAUSED,
        //-------------
        GAME_MAX
    }
}
