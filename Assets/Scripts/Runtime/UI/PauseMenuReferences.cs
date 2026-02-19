using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References to pause menu UI elements created by UISetup.ExecutePauseMenuUI().
/// Story 16.2: Used by PauseMenuUI controller to wire button callbacks and show/hide panels.
/// </summary>
public class PauseMenuReferences
{
    public Canvas PauseMenuCanvas;
    public GameObject PausePanel;
    public Text HeaderText;
    public Button ContinueButton;
    public Button SettingsButton;
    public Button ReturnToMenuButton;
    public Button ExitButton;
    public GameObject ConfirmationPopup;
    public Button ConfirmYesButton;
    public Button ConfirmNoButton;
}
