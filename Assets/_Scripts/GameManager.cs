using UnityEngine;

/// <summary>
/// Scene-level entry point and compatibility facade for existing Unity events.
/// Match rules and state live in MatchManager.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [SerializeField] private MatchManager matchManager;

    public MatchManager Match => matchManager;
    private void Start()
    {
        if (matchManager == null) matchManager = GetComponent<MatchManager>();
        if (matchManager == null) matchManager = FindObjectOfType<MatchManager>();

        if (matchManager == null)
        {
            Debug.LogError("GameManager needs a MatchManager reference.");
            return;
        }

        SetupGame();
    }

    public void SetupGame()
    {
        if (matchManager == null)
        {
            Debug.LogError("Cannot start the match because MatchManager is missing.");
            return;
        }

        matchManager.SetupMatch();
    }

    // Keep these entry points for existing scene events and external integrations.
    public void SelectHandCard(CardInstance card)
    {
        if (matchManager != null) matchManager.SelectHandCard(card);
    }

    public void TryPlaceCardOnBoard(BoardSlot targetSlot)
    {
        if (matchManager != null) matchManager.TryPlaceCardOnBoard(targetSlot);
    }

    public void OnEndTurnButtonClicked()
    {
        if (matchManager != null) matchManager.OnEndTurnButtonClicked();
    }
}
