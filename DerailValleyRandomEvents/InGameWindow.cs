using DV.OriginShift;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public class InGameWindow : MonoBehaviour
{
    private UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private bool showGui = false;
    private Rect buttonRect = new Rect(20, 30, 20, 20); // TODO: avoid conflict with other mods (currently just DV Utilities mod)
    private Rect windowRect = new Rect(20, 30, 0, 0);
    private Rect scrollRect;
    private Vector2 scrollPosition;
    private bool isClickToSpawnEnabled = false;
    private ObstacleType _selectedType = ObstacleType.Rockslide;
    private bool _showDropdown = false;

    public void Show()
    {

    }

    void OnGUI()
    {
        if (PlayerManager.PlayerTransform == null)
        {
            showGui = false;
            return;
        }

        if (!VRManager.IsVREnabled() && ScreenspaceMouse.Instance && !ScreenspaceMouse.Instance.on) return;

        if (GUI.Button(buttonRect, "RE", new GUIStyle(GUI.skin.button) { fontSize = 10, clipping = TextClipping.Overflow })) showGui = !showGui;

        if (showGui)
        {
            float scale = 1.5f;
            Vector2 pivot = Vector2.zero; // top-left corner

            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            windowRect = GUILayout.Window(555, windowRect, Window, "Random Events");

            GUI.matrix = oldMatrix;
        }

        if (isClickToSpawnEnabled && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            SpawnFromCamera(Event.current.mousePosition);
        }
    }

    void SpawnFromCamera(Vector2 mousePos)
    {
        Logger.Log($"[InGameWindow] Spawn at camera");

        var cam = PlayerManager.ActiveCamera;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(mousePos);

        var mask = LayerMask.GetMask("Terrain");

        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, mask))
        {
            Logger.Log($"[InGameWindow] Raycast success: point={hit.point} transform={hit.transform}");

            Vector3 globalPos = hit.point;
            Vector3 localPos = globalPos - OriginShift.currentMove;
            Spawn(localPos);
        }
        else
        {
            Logger.Log($"[InGameWindow] Raycast failed");
        }
    }

    void Spawn(Vector3 localPos)
    {
        Logger.Log($"[InGameWindow] Spawn at {localPos}");

        var obstacle = Main.RandomEventsManager.obstacles[_selectedType];

        var prefab = Main.RandomEventsManager.GetRandomObstaclePrefab(obstacle);

        Main.RandomEventsManager.EmitObstacleEventAtPos(localPos, obstacle, prefab);
    }

    void SpawnAhead()
    {
        Logger.Log($"[InGameWindow] Spawn ahead");

        Main.RandomEventsManager.EmitObstacleEventAhead(overrideType: _selectedType, forceForwards: true);
    }

    void ClearAll()
    {
        Logger.Log($"[InGameWindow] Clear all");

        ObstactleSpawner.ClearAllObstacles();
    }


    void Window(int windowId)
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(270 + GUI.skin.verticalScrollbar.fixedWidth), GUILayout.Height(scrollRect.height + GUI.skin.box.margin.vertical), GUILayout.MaxHeight(Screen.height - 130));
        GUILayout.BeginVertical();

        GUILayout.Label("");

        GUILayout.Label("Use the UnityModManager settings to configure the mod (ctrl+F10)");

        GUILayout.Label("");

        if (GUILayout.Button($"Selected: {_selectedType}"))
            _showDropdown = !_showDropdown;

        if (_showDropdown)
        {
            var names = System.Enum.GetNames(typeof(ObstacleType));

            for (int i = 0; i < names.Length; i++)
            {
                if (GUILayout.Button(names[i]))
                {
                    _selectedType = (ObstacleType)i;
                    _showDropdown = false;
                }
            }
        }

        GUILayout.Label("");

        isClickToSpawnEnabled = GUILayout.Toggle(isClickToSpawnEnabled, "Click to spawn");

        GUILayout.Label("");

        if (GUILayout.Button("Force event ahead (must be in car)"))
        {
            SpawnAhead();
        }

        GUILayout.Label("");

        if (GUILayout.Button("Clear All"))
        {
            ClearAll();
        }

        GUILayout.Label("");

        Main.RandomEventsManager.ShouldDrawDebugStuff = GUILayout.Toggle(Main.RandomEventsManager.ShouldDrawDebugStuff, "Show debug stuff");

        GUILayout.Label("");

        DrawOverrideForm();

        GUILayout.EndVertical();
        if (Event.current.type == EventType.Repaint)
        {
            scrollRect = GUILayoutUtility.GetLastRect();
        }
        GUILayout.EndScrollView();
    }

    void DrawOverrideForm()
    {
        var isEnabled = Main.RandomEventsManager.OverrideObstacle != null;

        var nowEnabled = GUILayout.Toggle(isEnabled, "Override obstacles");

        if (nowEnabled != isEnabled)
        {
            if (nowEnabled)
                Main.RandomEventsManager.OverrideObstacle = Main.RandomEventsManager.obstacles[_selectedType];
            else
                Main.RandomEventsManager.OverrideObstacle = null;
        }

        var o = Main.RandomEventsManager.OverrideObstacle;

        if (o == null)
            return;

        GUILayout.Label("Fall Time (ms):");
        o.FallTimeMs = int.Parse(GUILayout.TextField(o.FallTimeMs.ToString(), GUILayout.Width(100)));

        GUILayout.Label("Spawn Count:");
        o.SpawnCount = int.Parse(GUILayout.TextField(o.SpawnCount.ToString(), GUILayout.Width(100)));

        GUILayout.Label("Spawn Height:");
        o.SpawnHeightFromGround = float.Parse(GUILayout.TextField(o.SpawnHeightFromGround.ToString("0.###"), GUILayout.Width(100)));

        GUILayout.Label("Spawn Gap:");
        o.SpawnGap = float.Parse(GUILayout.TextField(o.SpawnGap.ToString("0.###"), GUILayout.Width(100)));

        GUILayout.Label("Scale Min / Max:");
        o.MinScale = float.Parse(GUILayout.TextField(o.MinScale.ToString("0.###"), GUILayout.Width(100)));
        o.MaxScale = float.Parse(GUILayout.TextField(o.MaxScale.ToString("0.###"), GUILayout.Width(100)));

        GUILayout.Label("Mass Min / Max:");
        o.MinMass = float.Parse(GUILayout.TextField(o.MinMass.ToString("0.###"), GUILayout.Width(100)));
        o.MaxMass = float.Parse(GUILayout.TextField(o.MaxMass.ToString("0.###"), GUILayout.Width(100)));
    }
}