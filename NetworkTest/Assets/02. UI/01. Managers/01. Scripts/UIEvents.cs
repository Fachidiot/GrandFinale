using System;

public static class UIEvents
{
    /// <summary>
    /// Called when the player's focus on an interactable object changes.
    /// The string parameter is the interaction text to display, or empty/null to hide it.
    /// </summary>
    public static event Action<string> OnInteractableFocusChanged;

    /// <summary>
    /// Called when the local player's components have been initialized.
    /// Passes the local player's WeaponController to the UI.
    /// </summary>
    public static event Action<WeaponController> OnPlayerInitialized;

    // Helper methods to invoke the events safely
    public static void InteractableFocusChanged(string text)
    {
        OnInteractableFocusChanged?.Invoke(text);
    }

    public static void PlayerInitialized(WeaponController weaponController)
    {
        OnPlayerInitialized?.Invoke(weaponController);
    }

    public static void FireInteractState(string text)
    {
        OnInteractableFocusChanged?.Invoke(text);
    }
}
