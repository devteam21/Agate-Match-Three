using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour
{
    public int id;

    private BoardManager board;
    private SpriteRenderer render;
    private static readonly Color selectedColor = new Color(0.5f, 0.5f, 0.5f);
    private static readonly Color normalColor = Color.white;
    private static TileController previousSelected = null;
    private bool isSelected = false;
    private static readonly float moveDuration = 0.5f;

    private void Awake()
    {
        board = BoardManager.Instance;
        render = GetComponent<SpriteRenderer>();
    }

    public void ChangeId(int id, int x, int y)
    {
        render.sprite = board.tileTypes[id];
        this.id = id;

        name = "TILE_" + id + " (" + x + ", " + y + ")";
    }

    
    private void OnMouseDown()
    {
        // Non Selectable conditions
        if (render.sprite == null || board.IsAnimating)
        {
            return;
        }
        
        // Already selected this tile?
        if (isSelected)
        {
            Deselect();
        }
        else
        {
            // if nothing selected yet
            if (previousSelected == null)
            {
                Select();
            }
            else
            {
                // is this an adjacent tile?
                if (GetAllAdjacentTiles().Contains(previousSelected))
                {
                    TileController otherTile = previousSelected;
                    previousSelected.Deselect();

                    // swap tile
                    SwapTile(otherTile, () => {
                    SwapTile(otherTile);
                });
            }
                // if not adjacent then change selected
                else
                {
                    previousSelected.Deselect();
                    Select();
                }
            }
        }
    }
    
    public void SwapTile(TileController otherTile, System.Action onCompleted = null)
    {
        StartCoroutine(board.SwapTilePosition(this, otherTile, onCompleted));
    }

    #region Select & Deselect

    private void Select()
    {
        isSelected = true;
        render.color = selectedColor;
        previousSelected = this;
    }

    private void Deselect()
    {
        isSelected = false;
        render.color = normalColor;
        previousSelected = null;
    }

    #endregion

    public IEnumerator MoveTilePosition(Vector2 targetPosition, System.Action onCompleted)
    {
        Vector2 startPosition = transform.position;
        float time = 0.0f;

        // run animation on next frame for safety reason
        yield return new WaitForEndOfFrame();

        while (time < moveDuration)
        {
            transform.position = Vector2.Lerp(startPosition, targetPosition, time / moveDuration);
            time += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        transform.position = targetPosition;

        onCompleted?.Invoke();
    }

    private static readonly Vector2[] adjacentDirection = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
    
    #region Adjacent

    private TileController GetAdjacent(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, render.size.x);
        if (hit)
        {
            return hit.collider.GetComponent<TileController>();
        }
        return null;
    }

    public List<TileController> GetAllAdjacentTiles()
    {
        List<TileController> adjacentTiles = new List<TileController>();

        for (int i = 0; i < adjacentDirection.Length; i++)
        {
            adjacentTiles.Add(GetAdjacent(adjacentDirection[i]));
        }

        return adjacentTiles;
    }

    #endregion
}