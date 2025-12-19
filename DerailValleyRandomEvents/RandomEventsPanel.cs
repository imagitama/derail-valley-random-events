using DV.OriginShift;
using UnityEngine;
using UnityModManagerNet;
using DerailValleyModToolbar;
using System.Linq;

namespace DerailValleyRandomEvents;

public class RandomEventsPanel : MonoBehaviour, IModToolbarPanel
{
    private UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private Obstacle? _overrideObstacle;
    private SpawnedEvent? _lastResult;
    private string? _selectedType;
    private bool _showDropdown = false;
    private bool _ignoreBiome = false;
    // spawner
    private GameObject? _spawner;
    private bool _overrideTransform = false;
    private Vector3? newScale = null;
    private Vector3? newRotation = null;
    private string _scaleXText = "1.0";
    private string _scaleYText = "1.0";
    private string _scaleZText = "1.0";
    private string _rotationXText = "1.0";
    private string _rotationYText = "1.0";
    private string _rotationZText = "1.0";
    // event editor
    private string _minSpawnCountText = "";
    private string _maxSpawnCountText = "";
    private string _spawnHeightFromGroundText = "";
    private string _verticalSpawnGapText = "";
    private string _horizontalSpawnGapText = "";
    private string _minScaleText = "";
    private string _maxScaleText = "";
    private string _minMassText = "";
    private string _maxMassText = "";
    private string _dragText = "";
    private string _angularDragText = "";
    private string _dynamicFrictionText = "";
    private string _staticFrictionText = "";
    private string _bouncinessText = "";
    private string _gravityText = "";
    private string _derailThresholdText = "";
    private string _explodeThresholdText = "";
    private string _explodeForceText = "";
    private string _explodeRadiusText = "";
    private string _explodeUpwardsText = "";
    private string _jitterAmountText = "";
    // settings
    private bool _showOverrideForm = false;
    private bool _showSpawnerStuff = false;

    void OnDestroy()
    {
        Logger.Log($"[InGameWindow] Destroy");
        Object.Destroy(_spawner);
    }

    void SpawnAtSpawner()
    {
        if (_spawner == null)
        {
            CreateSpawner();
            var playerLocalPos = PlayerUtils.GetPlayerLocalPosition();

            if (playerLocalPos == null)
                return;

            MoveSpawnerToLocalPos((Vector3)playerLocalPos);
            MakeSpawnerLookAtCamera();
        }

        Logger.Log($"[InGameWindow] Spawn at spawner={_spawner}");

        Vector3 pos = _spawner!.transform.position;
        Spawn(pos);
    }

    void ForceNormalSpawnEvent()
    {
        Logger.Log("[InGameWindow] Spawn normal event");

        if (PlayerManager.Car == null)
            return;

        Main.randomEventsManager.EmitObstacleEventAhead(new EventRequest());
    }

    void ForceCustomSpawnEvent(string? overrideType = null)
    {
        Logger.Log("[InGameWindow] Spawn custom event");

        if (PlayerManager.Car == null)
            return;

        var result = Main.randomEventsManager.EmitObstacleEventAhead(new EventRequest()
        {
            obstacleType = overrideType ?? _selectedType,
            obstacle = _overrideObstacle,
            ignoreBiome = _ignoreBiome,
            ignoreNearbyCheck = true,
            ignoreBuiltUpAreaCheck = true,
            forceEverythingInPool = true
        });

        UpdateLastMessage(result);
    }

    void UpdateLastMessage(SpawnedEvent? result)
    {
        _lastResult = result;
    }

    void Spawn(Vector3? position = null, Quaternion? rotation = null, bool behind = false)
    {
        Logger.Log($"[InGameWindow] Spawn type={_selectedType} pos={position} rot={rotation} behind={behind}");

        if (PlayerManager.Car == null)
            return;

        var result = Main.randomEventsManager.EmitObstacleEventAhead(new EventRequest()
        {
            intendedPos = position,
            intendedRot = rotation,
            obstacle = _overrideObstacle,
            obstacleType = _selectedType,
            ignoreBiome = _ignoreBiome,
            ignoreNearbyCheck = true,
            ignoreBuiltUpAreaCheck = true,
            flipDirection = behind,
            forceEverythingInPool = true
        });

        UpdateLastMessage(result);
    }

    void MakeSpawnerLookAtCamera()
    {
        var cam = PlayerManager.ActiveCamera;
        if (cam == null || _spawner == null)
            return;

        var localPos = cam.transform.position;

        Vector3 flatTarget = new Vector3(localPos.x, _spawner.transform.position.y, localPos.z);

        _spawner.transform.LookAt(flatTarget);
    }

    void CreateSpawner()
    {
        Logger.Log("[InGameWindow] Create spawner...");

        var arrow = MeshUtils.CreateArrow();
        arrow.gameObject.name = "DerailValleyRandomEvents_Spawner";

        var safeParent = WorldMover.OriginShiftParent;

        arrow.transform.SetParent(safeParent);

        _spawner = arrow;
    }

    void MoveSpawnerToLocalPos(Vector3 localPos, bool? withOffset = true)
    {
        if (_spawner == null)
            CreateSpawner();

        Logger.Log($"[InGameWindow] Move spawner to localPos={localPos}");

        var localPosHigher = new Vector3(localPos.x, localPos.y + (withOffset == true ? 2f : 0), localPos.z);

        _spawner!.transform.position = localPosHigher;
    }

    void ClearAllObstacles()
    {
        Logger.Log($"[InGameWindow] Clear all obstacles");

        ObstacleSpawner.ClearAllObstacles();
    }

    void ExplodeAllObstacles()
    {
        Logger.Log($"[InGameWindow] Explode all obstacles");

        foreach (var comp in ObstacleSpawner.GetAllObstacles().ToList())
            comp.Explode();
    }

    void MoveSpawnerToCameraTarget()
    {
        Logger.Log($"[InGameWindow] Move spawner to camera target");

        Camera cam = PlayerManager.ActiveCamera;
        if (cam == null)
            return;

        Vector3 origin = cam.transform.position;
        Vector3 direction = cam.transform.forward;

        int mask = 1 << (int)DVLayer.Terrain;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, 50f, mask))
        {
            Logger.Log($"[InGameWindow] Raycast hit at {hit.point} on {hit.transform?.name}");

            Vector3 localPos = hit.point;

            MoveSpawnerToLocalPos(localPos);
        }
        else
        {
            float fallbackDistance = 50f;
            Vector3 airPoint = origin + direction * fallbackDistance;

            Logger.Log($"[InGameWindow] Raycast missed - using the air pos={airPoint}");

            MoveSpawnerToLocalPos(airPoint, withOffset: false);
        }

        MakeSpawnerLookAtCamera();
    }

    void MoveSpawnerOffset(Vector3 localOffset)
    {
        if (!_spawner)
            return;

        var localPos = _spawner!.transform.position;
        var newLocalPos = localPos + localOffset;

        Logger.Log($"[InGameWindow] Move spawner from={localPos} offset={localOffset} newPos={newLocalPos}");

        MoveSpawnerToLocalPos(newLocalPos, withOffset: false);
    }

    void RotateSpawnerOffset(Vector3 localOffset)
    {
        if (!_spawner)
            return;

        var currentRot = _spawner!.transform.rotation;
        var delta = Quaternion.Euler(localOffset);
        var newRot = currentRot * delta;
        _spawner.transform.rotation = newRot;
    }

    void MoveSpawnerToClosestTrack()
    {
        if (!_spawner)
            CreateSpawner();

        Vector3? playerGlobalPos = PlayerUtils.GetPlayerGlobalPosition();

        if (playerGlobalPos == null)
        {
            Logger.Log("[InGameWindow] No player global pos");
            return;
        }

        var (closestTrack, closestPoint) = RailTrack.GetClosest((Vector3)playerGlobalPos);

        Logger.Log($"[InGameWindow] Closest track={closestTrack} point={closestPoint}");

        if (closestTrack == null || closestPoint == null)
        {
            Logger.Log("[InGameWindow] Need closest data");
            return;
        }

        var newLocalPos = ((Vector3)closestPoint.Value.position) - OriginShift.currentMove;

        Logger.Log($"[InGameWindow] Move spawner to local={newLocalPos}");

        _spawner!.transform.position = newLocalPos;

        // TODO: face correctly
    }

    private bool _showCustomSpawnSettings = false;

    public void Window(Rect rect)
    {
        // handle unload
        if (Main.randomEventsManager == null)
            return;

        if (_lastResult != null)
            GUILayout.Label($"Spawned {_lastResult.count}x {_lastResult.obstacle.Type} {_lastResult.distance}m away (pos {_lastResult.position})");

        var bold = new GUIStyle(GUI.skin.label);
        bold.fontStyle = FontStyle.Bold;

        GUILayout.Label("Global Settings", bold);

        GUILayout.Label("More options in mod settings (ctrl+F10)");

        var newEnabled = GUILayout.Toggle(Main.settings.RandomSpawningEnabled, "Random spawning enabled");

        if (newEnabled != Main.settings.RandomSpawningEnabled)
        {
            Logger.Log($"[InGameWindow] Toggled random spawning {Main.settings.RandomSpawningEnabled} => {newEnabled}");
            Main.settings.RandomSpawningEnabled = newEnabled;
            Main.settings.Save(Main.ModEntry);
        }

        if (Main.settings.RandomSpawningEnabled)
        {
            var warningLabel = new GUIStyle(GUI.skin.label);
            warningLabel.fontSize *= 4;

            GUILayout.Label("Random events are ENABLED - use at your own risk!!!", warningLabel);
        }

        if (GUILayout.Button($"<b>Custom Spawn Settings {(_showCustomSpawnSettings ? "▼" : "▶")}</b>", GUI.skin.label)) _showCustomSpawnSettings = !_showCustomSpawnSettings;
        if (_showCustomSpawnSettings)
            DrawCustomSpawnSettings(rect.width);

        if (GUILayout.Button($"<b>Spawner {(_showSpawnerStuff ? "▼" : "▶")}</b>", GUI.skin.label)) _showSpawnerStuff = !_showSpawnerStuff;
        if (_showSpawnerStuff)
            DrawSpawner();

        if (GUILayout.Button($"<b>Override Obstacle {(_showOverrideForm ? "▼" : "▶")}</b>", GUI.skin.label)) _showOverrideForm = !_showOverrideForm;
        if (_showOverrideForm)
            DrawOverrideForm();

        GUILayout.Label("Controls", bold);

        if (GUILayout.Button("Clear All Obstacles"))
        {
            ClearAllObstacles();
        }

        if (GUILayout.Button("Explode All Obstacles"))
        {
            ExplodeAllObstacles();
        }

        Main.randomEventsManager.PreventAllObstacleDerailment = GUILayout.Toggle(Main.randomEventsManager.PreventAllObstacleDerailment, "Prevent obstacles from ever derailing");
    }

    void DrawMoreSpawning()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{Main.settings.ObstacleSpawnDistance:F2}m", GUILayout.Width(60));
        Main.settings.ObstacleSpawnDistance = GUILayout.HorizontalSlider(Main.settings.ObstacleSpawnDistance, 5f, 500f);
        if (GUILayout.Button("Default", GUILayout.Width(60)))
        {
            Main.settings.ObstacleSpawnDistance = Settings.DefaultObstacleSpawnDistance;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn Ahead"))
        {
            Spawn();
        }
        if (GUILayout.Button("Spawn Behind"))
        {
            Spawn(behind: true);
        }
        GUILayout.EndHorizontal();

        if (_lastResult != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Spawn At Last Pos: {_lastResult.position}"))
            {
                Quaternion? rotation = _spawner != null ? _spawner.transform.rotation : null;
                Spawn(position: _lastResult.position, rotation);
            }
            GUILayout.EndHorizontal();
        }
    }

    const float columnWidth = 150f;

    void DrawCustomSpawnSettings(float panelWidth)
    {
        if (GUILayout.Button("Force Normal Spawn"))
        {
            ForceNormalSpawnEvent();
        }

        _ignoreBiome = GUILayout.Toggle(_ignoreBiome, "Ignore biome when randomly spawning");

        if (GUILayout.Button($"Override Type: {_selectedType}"))
            _showDropdown = !_showDropdown;

        if (_showDropdown)
        {
            var names = ObstacleRegistry.Obstacles.Select(x => x.Type).ToList();
            int columns = Mathf.Max(1, Mathf.FloorToInt(panelWidth / columnWidth));

            GUILayout.BeginVertical();

            for (int i = 0; i < names.Count; i++)
            {
                if (i % columns == 0)
                    GUILayout.BeginHorizontal();

                var name = names[i];
                if (GUILayout.Button(name, GUILayout.Width(columnWidth)))
                {
                    if (_selectedType == name)
                        ClearSelectedType();
                    else
                    {
                        _selectedType = name;
                        _overrideObstacle = ObstacleRegistry.GetObstacleByType(_selectedType).Clone();
                    }

                    _showDropdown = false;
                    HydrateEditor();
                }

                if ((i + 1) % columns == 0)
                    GUILayout.EndHorizontal();
            }

            if (names.Count % columns != 0)
                GUILayout.EndHorizontal();

            GUILayout.EndVertical();


            if (GUILayout.Button("CLEAR"))
                ClearSelectedType();
        }

        DrawTransformControls();

        if (GUILayout.Button("Force Custom Spawn"))
        {
            ForceCustomSpawnEvent();
        }

        GUILayout.Label("Distance:");

        DrawMoreSpawning();
    }

    private bool _preventAllObstacleDerailment = true;

    void ClearSelectedType()
    {
        _selectedType = null;
        _overrideObstacle = null;
    }

    void DrawSpawner()
    {
        if (GUILayout.Button("Move Spawner To Camera"))
            MoveSpawnerToCameraTarget();

        GUILayout.Label("Move");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Up"))
            MoveSpawnerOffset(new Vector3(0, 0.5f, 0));

        if (GUILayout.Button("Down"))
            MoveSpawnerOffset(new Vector3(0, -0.5f, 0));

        if (GUILayout.Button("Left"))
            MoveSpawnerOffset(new Vector3(0.5f, 0, 0));

        if (GUILayout.Button("Right"))
            MoveSpawnerOffset(new Vector3(-0.5f, 0, 0));

        if (GUILayout.Button("Forward"))
            MoveSpawnerOffset(new Vector3(0, 0, 0.5f));

        if (GUILayout.Button("Back"))
            MoveSpawnerOffset(new Vector3(0, 0, -0.5f));
        GUILayout.EndHorizontal();

        GUILayout.Label("Rotate");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Right"))
            RotateSpawnerOffset(new Vector3(0, 45f, 0));

        if (GUILayout.Button("Left"))
            RotateSpawnerOffset(new Vector3(0, -45f, 0));

        if (GUILayout.Button("R. Left"))
            RotateSpawnerOffset(new Vector3(-45f, 0, 0));

        if (GUILayout.Button("R. Right"))
            RotateSpawnerOffset(new Vector3(45f, 0, 0));

        if (GUILayout.Button("Up"))
            RotateSpawnerOffset(new Vector3(0, 0, -45f));

        if (GUILayout.Button("Down"))
            RotateSpawnerOffset(new Vector3(0, 0, 45f));
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Spawn At Spawner"))
            SpawnAtSpawner();
    }

    void DrawTransformControls()
    {
        var nowEnabled = GUILayout.Toggle(_overrideTransform, "Scale or rotate");

        if (nowEnabled != _overrideTransform)
        {
            if (nowEnabled)
            {
                newScale = new Vector3(1, 1, 1);
                newRotation = new Vector3(1, 1, 1);
            }
            else
            {
                newScale = null;
                newRotation = null;

                ObstacleSpawner.ScaleMultiplier = null;
                ObstacleSpawner.RotationMultiplier = null;
            }

            _overrideTransform = nowEnabled;
        }

        if (!_overrideTransform || newScale == null || newRotation == null)
            return;

        var scale = (Vector3)newScale;

        GUILayout.Label("Scale XYZ (multiplier)");
        GUILayout.BeginHorizontal();
        _scaleXText = GUILayout.TextField(_scaleXText, GUILayout.Width(100));
        _scaleYText = GUILayout.TextField(_scaleYText, GUILayout.Width(100));
        _scaleZText = GUILayout.TextField(_scaleZText, GUILayout.Width(100));
        GUILayout.EndHorizontal();

        if (float.TryParse(_scaleXText, out float scaleX))
        {
            scale.x = scaleX;
        }
        if (float.TryParse(_scaleYText, out float scaleY))
        {
            scale.y = scaleY;
        }
        if (float.TryParse(_scaleZText, out float scaleZ))
        {
            scale.z = scaleZ;
        }

        newScale = scale;

        var rotation = (Vector3)newRotation;

        GUILayout.Label("Rotation XYZ (degrees):");
        GUILayout.BeginHorizontal();
        _rotationXText = GUILayout.TextField(_rotationXText, GUILayout.Width(100));
        _rotationYText = GUILayout.TextField(_rotationYText, GUILayout.Width(100));
        _rotationZText = GUILayout.TextField(_rotationZText, GUILayout.Width(100));
        GUILayout.EndHorizontal();

        if (float.TryParse(_rotationXText, out float rotationX))
        {
            rotation.x = rotationX;
        }
        if (float.TryParse(_rotationYText, out float rotationY))
        {
            rotation.y = rotationY;
        }
        if (float.TryParse(_rotationZText, out float rotationZ))
        {
            rotation.z = rotationZ;
        }

        newRotation = rotation;

        ObstacleSpawner.ScaleMultiplier = newScale;
        ObstacleSpawner.RotationMultiplier = newRotation;
    }

    void HydrateEditor()
    {
        var obstacle = _overrideObstacle;

        Logger.Log($"[InGameWindow] Hydrate editor type={_selectedType}");

        if (obstacle == null)
            return;

        _minSpawnCountText = obstacle.MinSpawnCount.ToString();
        _maxSpawnCountText = obstacle.MaxSpawnCount.ToString();
        _spawnHeightFromGroundText = obstacle.SpawnHeightFromGround.ToString();
        _verticalSpawnGapText = obstacle.VerticalSpawnGap.ToString();
        _horizontalSpawnGapText = obstacle.HorizontalSpawnGap.ToString();
        _minScaleText = obstacle.MinScale.ToString();
        _maxScaleText = obstacle.MaxScale.ToString();
        _minMassText = obstacle.MinMass.ToString();
        _maxMassText = obstacle.MaxMass.ToString();
        _dragText = obstacle.Drag.ToString();
        _angularDragText = obstacle.AngularDrag.ToString();
        _dynamicFrictionText = obstacle.DynamicFriction.ToString();
        _staticFrictionText = obstacle.StaticFriction.ToString();
        _bouncinessText = obstacle.Bounciness.ToString();
        _gravityText = obstacle.Gravity.ToString();
        _derailThresholdText = obstacle.DerailThreshold.ToString();
        _explodeThresholdText = obstacle.ExplodeThreshold.ToString();
        _explodeForceText = obstacle.ExplodeForce.ToString();
        _explodeRadiusText = obstacle.ExplodeRadius.ToString();
        _explodeUpwardsText = obstacle.ExplodeUpwards.ToString();
        _jitterAmountText = obstacle.JitterAmount.ToString();
    }

    void DrawOverrideForm()
    {
        var isEnabled = _overrideObstacle != null;

        if (!isEnabled)
            GUILayout.Label("You must select an obstacle type to override");

        var obstacleRef = _overrideObstacle;
        if (obstacleRef == null)
            return;

        GUILayout.Label($"Selected type: {_selectedType}");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Spawn Count Min / Max:");
        _minSpawnCountText = GUILayout.TextField(_minSpawnCountText, GUILayout.Width(100));
        if (int.TryParse(_minSpawnCountText, out int minSpawnCountResult))
        {
            obstacleRef.MinSpawnCount = minSpawnCountResult;
        }
        _maxSpawnCountText = GUILayout.TextField(_maxSpawnCountText, GUILayout.Width(100));
        if (int.TryParse(_maxSpawnCountText, out int maxSpawnCountResult))
        {
            obstacleRef.MaxSpawnCount = maxSpawnCountResult;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Spawn Height (meters):");
        _spawnHeightFromGroundText = GUILayout.TextField(_spawnHeightFromGroundText, GUILayout.Width(100));
        if (float.TryParse(_spawnHeightFromGroundText, out float spawnHeightFromGroundResult))
        {
            obstacleRef.SpawnHeightFromGround = spawnHeightFromGroundResult;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Vertical spawn gap (meters):");
        _verticalSpawnGapText = GUILayout.TextField(_verticalSpawnGapText, GUILayout.Width(100));
        if (float.TryParse(_verticalSpawnGapText, out float verticalSpawnGapNum))
        {
            obstacleRef.VerticalSpawnGap = verticalSpawnGapNum;
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        GUILayout.Label("Horizontal spawn gap (meters):");
        _horizontalSpawnGapText = GUILayout.TextField(_horizontalSpawnGapText, GUILayout.Width(100));
        if (float.TryParse(_horizontalSpawnGapText, out float horizontalSpawnGapNum))
        {
            obstacleRef.HorizontalSpawnGap = horizontalSpawnGapNum;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Scale Min / Max (multiplier):");
        _minScaleText = GUILayout.TextField(_minScaleText, GUILayout.Width(100));
        if (float.TryParse(_minScaleText, out float minScaleResult))
        {
            obstacleRef.MinScale = minScaleResult;
        }
        _maxScaleText = GUILayout.TextField(_maxScaleText, GUILayout.Width(100));
        if (float.TryParse(_maxScaleText, out float maxScaleResult))
        {
            obstacleRef.MaxScale = maxScaleResult;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Mass Min / Max:");
        _minMassText = GUILayout.TextField(_minMassText, GUILayout.Width(100));
        if (float.TryParse(_minMassText, out float minMassResult))
        {
            obstacleRef.MinMass = minMassResult;
        }
        _maxMassText = GUILayout.TextField(_maxMassText, GUILayout.Width(100));
        if (float.TryParse(_maxMassText, out float maxMassResult))
        {
            obstacleRef.MaxMass = maxMassResult;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Drag (rock = 0):");
        _dragText = GUILayout.TextField(_dragText, GUILayout.Width(100));
        if (float.TryParse(_dragText, out float dragResult))
        {
            obstacleRef.Drag = dragResult;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Angular Drag (spinning drag, rock = 0):");
        _angularDragText = GUILayout.TextField(_angularDragText, GUILayout.Width(100));
        if (float.TryParse(_angularDragText, out float angularDragResult))
        {
            obstacleRef.AngularDrag = angularDragResult;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Dynamic Friction:");
        _dynamicFrictionText = GUILayout.TextField(_dynamicFrictionText, GUILayout.Width(100));
        if (float.TryParse(_dynamicFrictionText, out float dynamicFrictionResult))
        {
            obstacleRef.DynamicFriction = dynamicFrictionResult;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Static Friction:");
        _staticFrictionText = GUILayout.TextField(_staticFrictionText, GUILayout.Width(100));
        if (float.TryParse(_staticFrictionText, out float staticFrictionResult))
        {
            obstacleRef.StaticFriction = staticFrictionResult;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Bounciness:");
        _bouncinessText = GUILayout.TextField(_bouncinessText, GUILayout.Width(100));
        if (float.TryParse(_bouncinessText, out float bouncinessResult))
        {
            obstacleRef.Bounciness = bouncinessResult;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Gravity (1 = normal):");
        _gravityText = GUILayout.TextField(_gravityText, GUILayout.Width(100));
        if (float.TryParse(_gravityText, out float gravityResult))
        {
            obstacleRef.Gravity = gravityResult;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Derail Threshold (0 to disable, 100k+ good):");
        _derailThresholdText = GUILayout.TextField(_derailThresholdText, GUILayout.Width(100));
        if (float.TryParse(_derailThresholdText, out float derailThresholdResult))
        {
            obstacleRef.DerailThreshold = derailThresholdResult;
        }
        GUILayout.EndHorizontal();

        // EXPLOSION

        GUILayout.BeginHorizontal();
        GUILayout.Label("Explode Threshold (0 to disable):");
        _explodeThresholdText = GUILayout.TextField(_explodeThresholdText, GUILayout.Width(100));
        if (float.TryParse(_explodeThresholdText, out float explodeThresholdNum))
        {
            obstacleRef.ExplodeThreshold = explodeThresholdNum;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Explode Force:");
        _explodeForceText = GUILayout.TextField(_explodeForceText, GUILayout.Width(100));
        if (float.TryParse(_explodeForceText, out float explodeForceNum))
        {
            obstacleRef.ExplodeForce = explodeForceNum == 0 ? null : explodeForceNum;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Explode Radius:");
        _explodeRadiusText = GUILayout.TextField(_explodeRadiusText, GUILayout.Width(100));
        if (float.TryParse(_explodeRadiusText, out float explodeRadiusNum))
        {
            obstacleRef.ExplodeRadius = explodeRadiusNum == 0 ? null : explodeRadiusNum;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Explode Upwards:");
        _explodeUpwardsText = GUILayout.TextField(_explodeUpwardsText, GUILayout.Width(100));
        if (float.TryParse(_explodeUpwardsText, out float explodeUpwardsNum))
        {
            obstacleRef.ExplodeUpwards = explodeUpwardsNum == 0 ? null : explodeUpwardsNum;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Jitter (meters):");
        _jitterAmountText = GUILayout.TextField(_jitterAmountText, GUILayout.Width(100));
        if (float.TryParse(_jitterAmountText, out float jitterAmountNum))
        {
            obstacleRef.JitterAmount = jitterAmountNum;
        }
        GUILayout.EndHorizontal();
    }
}