using UnityEngine;

/// <summary>
/// Initial game state showing the main menu overlay.
/// Story 16.1: Replaces direct transition to MarketOpenState in GameRunner.Start.
/// Enter shows main menu, plays title music, hides gameplay UI.
/// Exit hides main menu, shows gameplay UI.
/// </summary>
public class MainMenuState : IGameState
{
    /// <summary>Static flag for UI to check if main menu is active.</summary>
    public static bool IsActive { get; private set; }

    /// <summary>Main menu canvas GameObject — set by GameRunner before first transition.</summary>
    public static GameObject MainMenuCanvasGo;

    /// <summary>Settings canvas GameObject — hidden on exit.</summary>
    public static GameObject SettingsCanvasGo;

    /// <summary>Gameplay canvas GameObjects to hide/show — set by GameRunner.</summary>
    public static GameObject[] GameplayCanvases;

    public void Enter(RunContext ctx)
    {
        IsActive = true;

        // Show main menu overlay
        if (MainMenuCanvasGo != null) MainMenuCanvasGo.SetActive(true);

        // Hide gameplay UI
        if (GameplayCanvases != null)
        {
            for (int i = 0; i < GameplayCanvases.Length; i++)
            {
                if (GameplayCanvases[i] != null)
                    GameplayCanvases[i].SetActive(false);
            }
        }

        // Play title screen music via MusicManager
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayTitleMusic();
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[GameState] MainMenuState entered — main menu visible, title music playing");
        #endif
    }

    public void Update(RunContext ctx)
    {
        // Main menu is event-driven via button clicks — no per-frame logic needed
    }

    public void Exit(RunContext ctx)
    {
        IsActive = false;

        // Hide main menu overlay
        if (MainMenuCanvasGo != null) MainMenuCanvasGo.SetActive(false);

        // Hide settings panel if open
        if (SettingsCanvasGo != null) SettingsCanvasGo.SetActive(false);

        // Show gameplay UI
        if (GameplayCanvases != null)
        {
            for (int i = 0; i < GameplayCanvases.Length; i++)
            {
                if (GameplayCanvases[i] != null)
                    GameplayCanvases[i].SetActive(true);
            }
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[GameState] MainMenuState exited — gameplay UI visible");
        #endif
    }
}
