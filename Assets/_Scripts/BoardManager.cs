using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public const int WIDTH = 4;
    public const int HEIGHT = 4;

    public BoardSlot slotPrefab;
    public Transform boardGridParent; // Gắn object có GridLayoutGroup (4 cột)

    private BoardSlot[,] grid = new BoardSlot[WIDTH, HEIGHT];
    private MatchManager matchManager;

    public void SetMatchManager(MatchManager manager)
    {
        matchManager = manager;
        if (grid == null) return;

        foreach (BoardSlot slot in grid)
        {
            if (slot != null) slot.SetMatchManager(manager);
        }
    }

    public bool GenerateBoard()
    {
        if (boardGridParent == null || slotPrefab == null)
        {
            Debug.LogError("BoardManager needs a slot prefab and board grid parent.");
            return false;
        }

        if (grid != null)
        {
            bool alreadyGenerated = true;
            foreach (BoardSlot slot in grid)
            {
                if (slot == null)
                {
                    alreadyGenerated = false;
                    break;
                }
            }
            if (alreadyGenerated) return true;
        }

        grid = new BoardSlot[WIDTH, HEIGHT];
        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                BoardSlot newSlot = Instantiate(slotPrefab, boardGridParent);
                newSlot.name = $"Slot_{x}_{y}";
                newSlot.Init(x, y);
                newSlot.SetMatchManager(matchManager);
                grid[x, y] = newSlot;
            }
        }
        return true;
    }

    public void ResetBoard()
    {
        if (grid == null) return;

        foreach (BoardSlot slot in grid)
        {
            if (slot != null) slot.ClearSlot();
        }
    }

    public void SetBoard(MatchState state)
    {
        if (state == null || grid == null) return;

        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                BoardCellState cellState = state.GetCellState(x, y);
                if (cellState == BoardCellState.Occupied)
                    grid[x, y].PlaceCard(state.GetCardAt(x, y), state);
                else
                    grid[x, y].ClearSlot(cellState, state.IsPlacementBlockedNextTurn(x, y));
            }
        }
    }

    public List<BoardSlot> GetEmptySlots()
    {
        List<BoardSlot> emptySlots = new List<BoardSlot>();
        if (grid == null) return emptySlots;
        foreach (var slot in grid)
        {
            if (slot != null && slot.IsEmpty) emptySlots.Add(slot);
        }
        return emptySlots;
    }

    public int GetPlayerOwnedCell()
    {
        MatchState state = matchManager != null ? matchManager.State : null;
        if (state == null) return 0;

        int res = 0;
        for (int i = 0; i < WIDTH; i++)
        {
            for (int j = 0; j < HEIGHT; j++)
            {
                if (grid[i, j] == null) continue;
                CardInstance card = grid[i, j].placedCard;
                if (card == null) continue;
                if (state.GetCellOwner(card.X, card.Y) == PlayerSide.Player) res++;
            }
        }
        return res;
    }

    public int GetEnemyOwnedCell()
    {
        MatchState state = matchManager != null ? matchManager.State : null;
        if (state == null) return 0;

        int res = 0;
        for (int i = 0; i < WIDTH; i++)
        {
            for (int j = 0; j < HEIGHT; j++)
            {
                if (grid[i, j] == null) continue;
                CardInstance card = grid[i, j].placedCard;
                if (card == null) continue;
                if (state.GetCellOwner(card.X, card.Y) == PlayerSide.Enemy) res++;
            }
        }
        return res;
    }

}
