using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TilePower { None, Horizontal, Vertical, Explode }

public class TileController : MonoBehaviour
{
    public int id;
    
    public float powerChance = 0.05f;

    private BoardManager board;
    private SpriteRenderer sprRenderer;

    private static readonly Color SelectedColor = new Color(0.5f, 0.5f, 0.5f);
    private static readonly Color normalColor = Color.white;
    private static readonly float moveDuration = 0.5f;
    private static readonly float destroyBigDuration = 0.1f;
    private static readonly float destroySmallDuration = 0.4f;

    private static readonly Vector2 sizeBig = Vector2.one * 1.2f;
    private static readonly Vector2 sizeSmall = Vector2.zero;
    private static readonly Vector2 sizeNormal = Vector2.one;
    private static readonly Vector2[] adjacentDirection = new Vector2[]
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };
    private static TileController previousSelected = null;

    private GameFlowManager game;
    public TilePower tilePower;

    [SerializeField] private List<GameObject> _powers;

    private bool isSelected = false;

    public bool IsDestroyed { get; private set; }

    public bool HasPower { get { return (tilePower != TilePower.None); } }

    private void Start()
    {
        IsDestroyed = false;        
    }

    private void Awake()
    {
        board = BoardManager.Instance;
        sprRenderer = GetComponent<SpriteRenderer>();
        game = GameFlowManager.Instance;
    }

    private void OnMouseDown()
    {
        if (sprRenderer.sprite == null || board.IsAnimating || game.IsGameOver) return;

        SoundManager.Instance.PlayTap();

        if (isSelected) {
            Deselect();
        }
        else {

            if (previousSelected == null) {
                Select();
            }
            else {
                if (GetAllAdjacentTiles().Contains(previousSelected)) {
                    TileController otherTile = previousSelected;
                    previousSelected.Deselect();

                    SwapTile(otherTile, () => {
                        if (board.GetAllMatches().Count > 0) {
                            // match found
                            board.Process();
                        }
                        else {
                            SoundManager.Instance.PlayWrong();
                            SwapTile(otherTile);
                        }
                    });

                }
                else {
                    previousSelected.Deselect();
                    Select();
                }
            }

        }
    }

    public void GivePower()
    {
        foreach (GameObject item in _powers)
        {
            item.SetActive(false);
        }
        float rand = Random.Range(0f, 1f);
        if (rand < powerChance)
        {
            tilePower = (TilePower)Random.Range(1, 3);
            _powers.Find(e => e.name.Contains(tilePower.ToString())).SetActive(true);            
        }
        else
        {
            tilePower = TilePower.None;            
        }  
    }

    public void ChangeId(int id, int x, int y)
    {
        sprRenderer.sprite = board.tileTypes[id];
        this.id = id;

        name = "TILE_" + id + "("+x+","+y+")";
    }

    public void SwapTile(TileController otherTile, System.Action onCompleted = null)
    {
        StartCoroutine(board.SwapTilePosition(this, otherTile, onCompleted));
    }

    public void GenerateRandomTiles(int x, int y)
    {
        transform.localScale = sizeNormal;
        IsDestroyed = false;
        GivePower();
        ChangeId(Random.Range(0, board.tileTypes.Count), x, y);        
    }

    public IEnumerator MoveTilePosition(Vector2 targetPos, System.Action onCompleted)
    {
        Vector2 startPos = transform.position;
        float time = 0.0f;

        yield return new WaitForEndOfFrame();

        while(time < moveDuration) {
            transform.position = Vector2.Lerp(startPos, targetPos, time / moveDuration);
            time += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        transform.position = targetPos;

        onCompleted?.Invoke();
    }

    public IEnumerator SetDestroyed(System.Action onCompleted)
    {
        IsDestroyed = true;
        id = -1;
        name = "TILE_NULL";

        Vector2 startSize = transform.localScale;
        float time = 0.0f;
        while (time < destroyBigDuration)
        {
            transform.localScale = Vector2.Lerp(startSize, sizeSmall, time / destroySmallDuration);
            time += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        transform.localScale = sizeSmall;

        sprRenderer.sprite = null;
        onCompleted?.Invoke();
    }

    #region Select & Deselect

    private void Select()
    {
        isSelected = true;
        sprRenderer.color = SelectedColor;
        previousSelected = this;
    }

    private void Deselect()
    {
        isSelected = false;
        sprRenderer.color = normalColor;
        previousSelected = null;
    }
    #endregion

    #region Adjacent
    private TileController GetAdjacent(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, sprRenderer.size.x);

        if (hit)
        {
            return hit.collider.GetComponent<TileController>();
        }

        return null;
    }

    public List<TileController> GetAllAdjacentTiles()
    {
        List<TileController> adjacentTiles = new List<TileController>();

        for (int i =0; i < adjacentDirection.Length; i++)
        {
            adjacentTiles.Add(GetAdjacent(adjacentDirection[i]));
        }

        return adjacentTiles;
    }
    #endregion

    #region Check Match

    private List<TileController> GetMatch(Vector2 castDir)
    {
        List<TileController> matchingTiles = new List<TileController>();
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, sprRenderer.size.x);

        while (hit)
        {
            TileController otherTile = hit.collider.GetComponent<TileController>();
            if ((otherTile.id != id || otherTile.IsDestroyed))
            {
                break;
            }

            matchingTiles.Add(otherTile);
            hit = Physics2D.Raycast(otherTile.transform.position, castDir, sprRenderer.size.x);

        }

        return matchingTiles;
    }

    private List<TileController> GetOneLineMatch(Vector2[] paths)
    {
        List<TileController> matchingTiles = new List<TileController>();

        for (int i = 0; i < paths.Length; i++)
        {
            matchingTiles.AddRange(GetMatch(paths[i]));
        }

        if (matchingTiles.Count >= 2)
        {
            return matchingTiles;
        }

        return null;
    }

    public List<TileController> GetAllMatches()
    {
        if (IsDestroyed)
        {
            return null;
        }

        List<TileController> matchingTiles = new List<TileController>();

        List<TileController> horizontalMatches = GetOneLineMatch(new Vector2[2] { Vector2.up, Vector2.down });
        List<TileController> verticalMatches = GetOneLineMatch(new Vector2[2] { Vector2.left, Vector2.right });

        if (horizontalMatches != null)
        {
            matchingTiles.AddRange(horizontalMatches);
        }

        if (verticalMatches != null)
        {
            matchingTiles.AddRange(verticalMatches);
        }

        if (matchingTiles!= null && matchingTiles.Count >= 2)
        {
            matchingTiles.Add(this);
        }

        return matchingTiles;
    }

    #endregion

    #region Power


    // melakukan Raycasting mirip seperti GetAllMatches namun tanpa batasan harus memiliki id yang sama

    public List<TileController> GetLineTiles(Vector2[] paths) {
        TilePower casterPower = tilePower;
        List<TileController> tilesInline = new List<TileController>();

        for (int i = 0; i < paths.Length; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, paths[i], sprRenderer.size.x);
            
            while (hit)
            {                
                TileController otherTile = hit.collider.GetComponent<TileController>();

                if (otherTile.IsDestroyed) break;

                if (otherTile.HasPower && otherTile.tilePower != casterPower)
                {
                    List<TileController> additionalTiles = new List<TileController>();
                    

                    // jika tile yang di hit memiliki power (chain reaction) maka akan memanggil fungsi ini lagi dari perspektif tile yang di hit(otherTile)
                    if (otherTile.tilePower == TilePower.Horizontal)
                    {
                        additionalTiles.AddRange(otherTile.GetLineTiles(new Vector2[2] { Vector2.left, Vector2.right }));
                    } else if (otherTile.tilePower == TilePower.Vertical) {
                        additionalTiles.AddRange(otherTile.GetLineTiles(new Vector2[2] { Vector2.up, Vector2.down }));
                    }

                    tilesInline.AddRange(additionalTiles);

                }

                tilesInline.Add(otherTile);
                hit = Physics2D.Raycast(otherTile.transform.position, paths[i], sprRenderer.size.x);
            }
        }

        return tilesInline;
    }

    #endregion
}
