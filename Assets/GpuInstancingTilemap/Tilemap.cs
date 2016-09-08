using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngineInternal;

[ExecuteInEditMode]
public class Tilemap : MonoBehaviour {

    public Material TilesetMaterial;
    [SerializeField] private Mesh instanceMesh;
    [SerializeField] private int tilesetWidth = 16;
    [SerializeField] private int tilesetHeight = 16;

    [SerializeField] private int tileSize = 1;
    [SerializeField] public int TileMapWidth = 3;
    public List<int> Tiles;

    private Bounds tilemapBounds;

    public int TileSize { get { return tileSize; } }
    public Bounds TilemapBounds { get { return tilemapBounds; } }
    public int TilesetWidth { get { return tilesetWidth; } }
    public int TilesetHeight { get { return tilesetHeight; } }

    public int GetTile(int x, int y) { return Tiles[y*TileMapWidth + x]; }

    public void OnValidate() {
        var dim = new Vector3(tileSize * TileMapWidth, tileSize * (float)Tiles.Count / TileMapWidth, 0);
        tilemapBounds = new Bounds(this.transform.position - new Vector3(tileSize/2f, tileSize/2f) + dim /2, dim);

        UpdateTilemap();
    }

	void Start () {
        float t_x = 1f/tilesetWidth;
        float t_y = 1f/tilesetHeight;
        
        TilesetMaterial.mainTextureScale = new Vector2(1f/tilesetWidth, 1f/tilesetHeight); 
        UpdateTilemap();
	}


    void UpdateTilemap() {
    }


	// Update is called once per frame
	void Update () {
         for (int y = 0; y < ((Tiles.Count + TileMapWidth-1)/TileMapWidth); ++y) {
            for (int x = 0; x < TileMapWidth; ++x) {
                if(TileMapWidth * y + x >= Tiles.Count) break;

                var props = new MaterialPropertyBlock();

                var t = GetTile(x, y);
                int t_x = t%tilesetWidth;
                int t_y = t/tilesetWidth;

                props.SetVector("_TilePosition", new Vector4(t_x*1f/tilesetWidth, 1- ((t_y)*1f/tilesetHeight), 0 , 0));
                
                Graphics.DrawMesh(instanceMesh, this.transform.position + Vector3.right * tileSize * x + Vector3.up * tileSize * y,Quaternion.identity, TilesetMaterial, 0, null, 0, props);
            }
        }
	}

}
