using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    private int combo;

    private static BoardManager _instance = null;
    public static BoardManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BoardManager>();
            }

            return _instance;
        }        
    }

    [Header("Board")]
    public Vector2Int size;
    public Vector2 offsetTile;
    public Vector2 offsetBoard;

    [Header("Tile")]
    public List<Sprite> tileTypes = new List<Sprite>();
    public GameObject tilePrefab;
    
    private Vector2 startPos;
    private Vector2 endPos;
    private TileController[,] tiles;

    public bool IsSwapping { get; set; }
    public bool IsProcessing { get; set; }
    public bool IsAnimating
    {
        get
        {
            return IsProcessing || IsSwapping;
        }
    }
    public bool IsAllTrue(List<bool> list)
    {
        foreach(bool status in list)
        {
            if (!status) return false;
        }

        return true;
    }


    private void Start()
    {
        Vector2 tileSize = tilePrefab.GetComponent<SpriteRenderer>().size;
        CreateBoard(tileSize);

        IsProcessing = false;
        IsSwapping = false;
    }

    private void CreateBoard(Vector2 tileSize)
    {
        tiles = new TileController[size.x, size.y];
        Vector2 totalSize = (tileSize + offsetTile) * (size - Vector2.one);

        startPos = (Vector2)transform.position - (totalSize / 2) + offsetBoard;
        endPos = startPos + totalSize;

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {

                TileController newTile = Instantiate(
                    tilePrefab,
                    new Vector2(
                        startPos.x + (tileSize.x + offsetTile.x) * x,
                        startPos.y + (tileSize.y + offsetTile.y) * y),
                    tilePrefab.transform.rotation, transform).GetComponent<TileController>();
                
                tiles[x, y] = newTile;

                List<int> possibleId = GetStartingPossibleList(x, y);
                int newID = possibleId[Random.Range(0, possibleId.Count)];

                newTile.GivePower();
                newTile.ChangeId(newID, x, y);                
            }
        }
    }    

    private List<int> GetStartingPossibleList(int x, int y)
    {
        List<int> possibleId = new List<int>();

        for (int i=0; i< tileTypes.Count; i++)
        {
            possibleId.Add(i);
        }

        if (x > 1 && tiles[x-1, y].id == tiles[x - 2, y].id)
        {
            possibleId.Remove(tiles[x - 1, y].id);
        }

        if (y > 1 && tiles[x, y-1].id == tiles[x, y - 2].id)
        {
            possibleId.Remove(tiles[x, y - 1].id);
        }

        return possibleId;
    }

    public Vector2Int GetTileIndex(TileController tile)
    {
        for (int x =0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (tile == tiles[x, y]) return new Vector2Int(x, y);
            }
        }

        return new Vector2Int(-1, -1);
    }

    public Vector2 GetIndexPos(Vector2Int index)
    {
        Vector2 tileSize = tilePrefab.GetComponent<SpriteRenderer>().size;
        return new Vector2(
            startPos.x + ((tileSize.x + offsetTile.x) * index.x),
            startPos.y + ((tileSize.y + offsetTile.y) * index.y)
            );
    }   
    
    #region Swapping
    public IEnumerator SwapTilePosition(TileController a, TileController b, System.Action onCompleted)
    {
        IsSwapping = true;

        Vector2Int indexA = GetTileIndex(a);
        Vector2Int indexB = GetTileIndex(b);

        tiles[indexA.x, indexA.y] = b;
        tiles[indexB.x, indexB.y] = a;

        a.ChangeId(a.id, indexB.x, indexB.y);
        b.ChangeId(b.id, indexA.x, indexA.y);

        bool isRoutineACompleted = false;
        bool isRoutineBCompleted = false;

        StartCoroutine(
            a.MoveTilePosition(GetIndexPos(indexB), () => { isRoutineACompleted = true; }
            ));

        StartCoroutine(
            b.MoveTilePosition(GetIndexPos(indexA), () => { isRoutineBCompleted = true; }
            ));

        yield return new WaitUntil(() =>
        {
            return isRoutineACompleted && isRoutineBCompleted;
        });

        onCompleted?.Invoke();
        IsSwapping = false;
    }
    #endregion

    #region Match

    public List<TileController> GetAllMatches()
    {
        List<TileController> matchingTiles = new List<TileController>();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                List<TileController> tileMatched = tiles[x, y].GetAllMatches();

                if (tileMatched == null || tileMatched.Count == 0) continue;

                foreach (TileController item in tileMatched)
                {

                    // kalau tile yang ke match punya power, maka satu baris searah dengan directionya akan ditambahkan kedalam matchingTiles
                    if (item.HasPower)
                    {
                        List<TileController> additionalTiles = new List<TileController>();
                        if (item.tilePower == TilePower.Horizontal)
                        {
                            additionalTiles.AddRange(item.GetLineTiles(new Vector2[2] { Vector2.left, Vector2.right }));
                        } else if (item.tilePower == TilePower.Vertical)
                        {
                            additionalTiles.AddRange(item.GetLineTiles(new Vector2[2] { Vector2.up, Vector2.down }));
                        }

                        if (additionalTiles.Count > 0)
                        {
                            matchingTiles.AddRange(additionalTiles);
                        }
                    }

                    if (!matchingTiles.Contains(item))
                    {
                        matchingTiles.Add(item);
                    }


                }
            }
        }

        return matchingTiles;
    }

    public void Process()
    {
        IsProcessing = true;
        ProcessMatch();
        combo = 0;
    }

    private void ProcessMatch()
    {
        List<TileController> matchingTiles = GetAllMatches();

        if (matchingTiles == null || matchingTiles.Count == 0)
        {
            IsProcessing = false;
            return;
        }

        combo++;

        ScoreManager.Instance.IncrementCurrentScore(matchingTiles.Count, combo);
        StartCoroutine(ClearMatches(matchingTiles, ProcessDrop));
    }

    private IEnumerator ClearMatches(List<TileController> matchingTiles, System.Action onCompleted)
    {
        List<bool> isCompleted = new List<bool>();

        for(int i = 0; i < matchingTiles.Count; i++)
        {
            isCompleted.Add(false);
        }

        for(int i = 0; i < matchingTiles.Count; i++)
        {
            int index = i;
            StartCoroutine(matchingTiles[i].SetDestroyed(() =>
            {
                isCompleted[index] = true;
            }));
        }

        yield return new WaitUntil(() =>
        {
            return IsAllTrue(isCompleted);
        });

        onCompleted?.Invoke();
    }

    private void ProcessDrop()
    {
        Dictionary<TileController, int> droppingTiles = GetAllDrop();
        StartCoroutine(DropTiles(droppingTiles, ProcessDestroyAndFill));
    }

    private Dictionary<TileController, int> GetAllDrop()
    {
        Dictionary<TileController, int> droppingTiles = new Dictionary<TileController, int>();

        for (int x = 0; x < size.x; x++)
        {
            for (int y= 0; y< size.y; y++)
            {
                if (tiles[x, y].IsDestroyed)
                {
                    for (int i = y + 1; i < size.y; i++)
                    {                        
                        if (tiles[x, i].IsDestroyed)
                        {
                            continue;
                        }

                        if (droppingTiles.ContainsKey(tiles[x, i]))
                        {
                            droppingTiles[tiles[x, i]]++;
                        } else
                        {
                            droppingTiles.Add(tiles[x, i], 1);
                        }
                    }
                }
            }
        }

        return droppingTiles;
    }

    private IEnumerator DropTiles(Dictionary<TileController, int> droppingTiles, System.Action onCompleted)
    {
        foreach (KeyValuePair<TileController, int> pair in droppingTiles) {
            Vector2Int tileIndex = GetTileIndex(pair.Key);

            TileController temp = pair.Key;
            tiles[tileIndex.x, tileIndex.y] = tiles[tileIndex.x, tileIndex.y - pair.Value];
            tiles[tileIndex.x, tileIndex.y - pair.Value] = temp;

            temp.ChangeId(temp.id, tileIndex.x, tileIndex.y - pair.Value);
        }

        yield return null;

        onCompleted?.Invoke();
    }
    #endregion

    #region Tile Power

    #endregion

    #region Destroy & Fill

    private void ProcessDestroyAndFill()
    {
        List<TileController> destroyedTiles = GetAllDestroyed();
        StartCoroutine(DestroyedAndFillTiles(destroyedTiles, ProcessReposition));

    }

    private List<TileController> GetAllDestroyed()
    {
        List<TileController> destroyedTiles = new List<TileController>();

        for(int x = 0; x < size.x; x++)
        {
            for (int y=0; y < size.y; y++)
            {
                if (tiles[x, y].IsDestroyed)
                {
                    destroyedTiles.Add(tiles[x, y]);
                }
            }
        }

        return destroyedTiles;
    }

    private IEnumerator DestroyedAndFillTiles(List<TileController> destroyedTiles, System.Action onCompleted)
    {        
        List<int> highestIndex = new List<int>();

        for (int i = 0; i< size.x; i++)
        {
            highestIndex.Add(size.y - 1);
        }

        float spawnHeight = endPos.y + tilePrefab.GetComponent<SpriteRenderer>().size.y + offsetTile.y;

        foreach(TileController tile in destroyedTiles)
        {
            Vector2Int tileIndex = GetTileIndex(tile);
            Vector2Int targetIndex = new Vector2Int(tileIndex.x, highestIndex[tileIndex.x]);
            highestIndex[tileIndex.x]--;

            tile.transform.position = new Vector2(tile.transform.position.x, spawnHeight);
            tile.GenerateRandomTiles(targetIndex.x, targetIndex.y);
        }

        yield return null;

        onCompleted?.Invoke();
    }

    #endregion

    #region Reposition

    private void ProcessReposition()
    {
        StartCoroutine(RepositionTiles(ProcessMatch));
    }

    private IEnumerator RepositionTiles(System.Action onCompleted)
    {
        List<bool> isCompleted = new List<bool>();

        int i = 0;
        for(int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2 targetPos = GetIndexPos(new Vector2Int(x, y));                
                if ((Vector2)tiles[x,y].transform.position == targetPos)
                {
                    continue;
                }

                isCompleted.Add(false);

                int index = i;
                StartCoroutine(tiles[x, y].MoveTilePosition(targetPos, () =>
                 {
                     isCompleted[index] = true;
                 }));

                //increment
                i++;
            }
        }

        yield return new WaitUntil(() =>
        {
            return IsAllTrue(isCompleted);
        });
        onCompleted?.Invoke();
    }

    #endregion

   
}
