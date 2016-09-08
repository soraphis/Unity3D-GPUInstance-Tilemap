using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Assets.GpuInstancingTilemap.Editor {
    [InitializeOnLoad]
    public class TilemapEditor {

        static TilemapEditor() {
            var tme = new TilemapEditor();
            SceneView.onSceneGUIDelegate += tme.OnScene;
        }


        enum WorkingMode {
            Disable, Select, Modify, Paint
        }

        private WorkingMode mode = WorkingMode.Disable;

        private GameObject toSelect;
        private Tilemap active;

        private Vector3 pos;

        private void RenderToolButtons() {
            // ButtonLeft
            // ButtonMid
            // ButtonRight

            GUI.enabled = Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Tilemap>() != null;
            active = GUI.enabled ? Selection.activeGameObject.GetComponent<Tilemap>() : null;

            var j = 30;
            var i = active != null ? j : j/3;
            var o = 10;
            var d = 2;

            if (GUI.Toggle(new Rect(o+i + 2*j + 3*d, 10, i, EditorGUIUtility.singleLineHeight), 3 == (int) mode, "P", "GUIEditor.BreadcrumbMid"))   mode = (WorkingMode) 3;
            if (GUI.Toggle(new Rect(o+2*j + 2*d, 10, i, EditorGUIUtility.singleLineHeight), 2 == (int) mode, "M", "GUIEditor.BreadcrumbMid"))     mode = (WorkingMode) 2;
            GUI.enabled = true;


            if (GUI.Toggle(new Rect(o, 10, j, EditorGUIUtility.singleLineHeight), 0 == (int) mode, "X", "GUIEditor.BreadcrumbLeft"))    mode = (WorkingMode) 0;
            if (GUI.Toggle(new Rect(o + j + d, 10, j, EditorGUIUtility.singleLineHeight), 1 == (int) mode, "S", "GUIEditor.BreadcrumbMid"))     mode = (WorkingMode) 1;


        }

        private void SelectTilemap(SceneView view) {
            if (Event.current.type != EventType.MouseDown || Event.current.button != 0) return;
            toSelect = null;
            var planes = GeometryUtility.CalculateFrustumPlanes(view.camera);

            var targets = GameObject.FindObjectsOfType<Tilemap>().Where(m => GeometryUtility.TestPlanesAABB(planes, m.TilemapBounds));
            // GUI.Label(new Rect(10, 10 + EditorGUIUtility.singleLineHeight, 80, EditorGUIUtility.singleLineHeight), "#:"+targets.Count());

            var plane = new Plane();
            foreach (var target in targets) {
                plane.SetNormalAndPosition(Vector3.forward, target.TilemapBounds.center);

                var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                float enter;
                plane.Raycast(ray, out enter);
                var hitpoint = ray.GetPoint(enter);

                if (target.TilemapBounds.Contains(hitpoint)) {
                    Selection.activeObject = target;
                    toSelect = target.gameObject;
                    break;
                }
            }
        }

        private int selectedTile = 0;

        private void OnScene(SceneView view) {
            if (EditorWindow.focusedWindow != view) {
                toSelect = null;
            }
            if (toSelect != null) {    Selection.activeObject = toSelect;  }
            
            using (HandleGUIDispose.BeginGUI()) {
                
                // editMode = GUI.Toggle(new Rect(10, 10, 80, EditorGUIUtility.singleLineHeight), editMode, "Edit Tilemap", "Button");
                RenderToolButtons();
                if (mode == WorkingMode.Disable) {
                    toSelect = null;
                    return;
                }
                if (mode == WorkingMode.Select) {
                    SelectTilemap(view);
                }
                if (active == null && (int)mode >= 2) {
                    mode = WorkingMode.Disable;
                }

                if (mode == WorkingMode.Paint) {

                    int sel_x = selectedTile%active.TilesetWidth;
                    int sel_y = selectedTile/active.TilesetWidth;

                    GUI.DrawTextureWithTexCoords(new Rect(10, 50, 128, 128), active.TilesetMaterial.mainTexture, new Rect(sel_x * 1f/16, 1- (1+sel_y) * 1/16f, 1/16f, 1/16f));
                    selectedTile = EditorGUI.IntField(new Rect(10 + 32, 50+8, 64, EditorGUIUtility.singleLineHeight), selectedTile);

                    if (GUI.Button(new Rect(10 - 8, 50 + 64 - 8, 16, 16), EditorGUIUtility.FindTexture( "node1 hex" ))) {
                        selectedTile = (active.TilesetWidth*active.TilesetHeight + selectedTile - 1)%(active.TilesetWidth*active.TilesetHeight);
                    }
                    if (GUI.Button(new Rect(10 - 8 +128, 50 + 64 - 8, 16, 16), EditorGUIUtility.FindTexture( "node1 hex" ))) {
                        selectedTile = (active.TilesetWidth*active.TilesetHeight + selectedTile + 1)%(active.TilesetWidth*active.TilesetHeight);
                    }
                    
                    if (GUI.Button(new Rect(10 + 64 - 8, 50-8, 16, 16), EditorGUIUtility.FindTexture( "node1 hex" ))) {
                        selectedTile = (active.TilesetWidth*active.TilesetHeight + selectedTile - active.TilesetWidth)%(active.TilesetWidth*active.TilesetHeight);
                    }

                    if (GUI.Button(new Rect(10 + 64 - 8, 50+120, 16, 16), EditorGUIUtility.FindTexture( "node1 hex" ))) {
                        selectedTile = (active.TilesetWidth*active.TilesetHeight + selectedTile + active.TilesetWidth)%(active.TilesetWidth*active.TilesetHeight);
                    }
                }
            }

            if (mode == WorkingMode.Paint) {
                var planes = GeometryUtility.CalculateFrustumPlanes(view.camera);
                var plane = new Plane(Vector3.forward, active.TilemapBounds.center);

                float enter;
                var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                plane.Raycast(ray, out enter);
                var hitpoint = ray.GetPoint(enter);

                if (! active.TilemapBounds.Contains(hitpoint)) return;

                var tilepos = (hitpoint - active.transform.position);
                var tile_x = Mathf.RoundToInt(tilepos.x);
                var tile_y = Mathf.RoundToInt(tilepos.y);

                if (Handles.Button(new Vector3(tile_x, tile_y, 0) + active.transform.position, Quaternion.identity, active.TileSize/2f, 1f, Handles.RectangleCap)) {
                    active.Tiles[tile_y*active.TileMapWidth + tile_x] = selectedTile;
                    active.OnValidate();
                    EditorUtility.SetDirty(active);
                }

            }

       

            if (mode == WorkingMode.Modify) {
            #region Modify

                var p = active.TilemapBounds.center;
                var d = active.TilemapBounds.extents;
                var dx = new Vector3(d.x, 0, 0);
                var dy = new Vector3(0, d.y, 0);

                var size = 0.25f;
                Handles.color = Color.yellow;
                EditorGUI.BeginChangeCheck();
                var pos = Handles.FreeMoveHandle(p + dx, Quaternion.identity, 0.25f, Vector3.right, Handles.RectangleCap);
                if (EditorGUI.EndChangeCheck()) {
                    var delta = pos - (p + dx);
                    if (delta.x > 0.75f*active.TileSize) {
                        var rows = active.Tiles.Count/active.TileMapWidth;
                        for (int i = 0; i < rows; ++i) {
                            active.Tiles.Insert(active.TileMapWidth*(i + 1) + i, 0);
                        }
                        active.TileMapWidth += 1;
                    } else
                        if (delta.x < -0.75f*active.TileSize) {
                            if (active.TileMapWidth < 2) return;

                            var rows = active.Tiles.Count/active.TileMapWidth;
                            for (int r = 1; r < rows; ++r) {
                                for (int i = 0; i < active.TileMapWidth; ++i) {
                                    active.Tiles[i - r + r*active.TileMapWidth] = active.Tiles[i + r*active.TileMapWidth];
                                }
                            }
                            active.Tiles.RemoveRange(active.Tiles.Count - 1 - rows, rows);

                            active.TileMapWidth -= 1;
                        }

                    active.OnValidate();
                    EditorUtility.SetDirty(active);
                }

                EditorGUI.BeginChangeCheck();
                pos = Handles.FreeMoveHandle(p + dy, Quaternion.identity, 0.25f, Vector3.up, Handles.RectangleCap);
                if (EditorGUI.EndChangeCheck()) {
                    var delta = pos - (p + dy);
                    if (delta.y > 0.75f*active.TileSize) active.Tiles.AddRange(new int[StepUp(active.TileMapWidth, active.Tiles.Count) - active.Tiles.Count]);
                    if (delta.y < -0.75f*active.TileSize) {
                        var n = StepDown(active.TileMapWidth, active.Tiles.Count);
                        active.Tiles.RemoveRange(n, active.Tiles.Count - n);
                    }

                    active.OnValidate();
                    EditorUtility.SetDirty(active);
                }

            #endregion
            }

            
        }

        /// <returns>the next multiple of a given number, larger than the given number</returns>
        private int StepUp(int multipleOf, int largerThan) { return largerThan + (multipleOf - largerThan%multipleOf); }

        private int StepDown(int multipleOf, int smallerThan) { return (smallerThan - 1)/multipleOf * multipleOf; }

    }


    class HandleGUIDispose : IDisposable {

        private static readonly HandleGUIDispose disposer = new HandleGUIDispose();
        private HandleGUIDispose() { }

        public static HandleGUIDispose BeginGUI() {
            Handles.BeginGUI();
            return disposer;
        }

        public void Dispose() {
            Handles.EndGUI();
        }
    }

/*

// [CustomEditor(typeof(Tilemap))]
public class TilemapEditor : EditorWindow {

    private Plane plane = new Plane(Vector3.forward, Vector3.zero);
    private bool select = false;

    private Object target;

    [MenuItem ("Window/My Window")]
    static void Init () {
        // Get existing open window or if none, make a new one:
        TilemapEditor window = (TilemapEditor)EditorWindow.GetWindow (typeof (TilemapEditor));
        window.Show();
    }

    void OnSelectionChange() {
        target = Selection.activeObject;
        this.Repaint();
    }

    void OnScene(SceneView sceneview) {
        Handles.BeginGUI();

            if (GUILayout.Button("Press Me"))
             Debug.Log("Got it to work.");

        Handles.EndGUI();
    }

    void OnGUI() {

        if(target == null || !(target is GameObject)) return;

        var go = (GameObject) target;
        var tilemap = go.GetComponent<Tilemap>();
        if(tilemap == null) return;

        GUI.Button(new Rect(10, 10, 100, 100), "test");

        if (@select) {
            Selection.activeObject = target;
            Event.current.Use();
            Debug.Log("tests123");
            if (Event.current.type != EventType.MouseUp) @select = false;
        }


        if (Event.current.type != EventType.MouseDown) return;
        plane.SetNormalAndPosition(Vector3.forward, tilemap.transform.position);

        float enter;
        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        plane.Raycast(ray, out enter);
        var hitpoint = ray.GetPoint(enter);

        var dim = new Vector3(tilemap.TileSize * tilemap.TileMapWidth, tilemap.TileSize * (float)tilemap.Tiles.Count / tilemap.TileMapWidth, 0);
        var start = tilemap.transform.position - new Vector3(tilemap.TileSize/2f, tilemap.TileSize/2f);

        var t = hitpoint - start;

        if (!(t.x > 0) || !(t.x < dim.x)) return;
        if (!(t.y > 0) || !(t.y < dim.y)) return;

        @select = true;
        Selection.activeObject = target;
        Event.current.Use();
        Debug.Log("tests");

    }
}
*/
}
