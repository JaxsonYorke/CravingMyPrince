namespace Assets.Scripts.Map {
    
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Assets.utils.enums;
using System.Collections;
    using UnityEngine.SceneManagement;

    public class MapController : MonoBehaviour {

    [SerializeField] private GameObject player;
    
    [HideInInspector] public List<MapLocation> locations;
    [HideInInspector] public List<MapLocation> availableLocations;
    [HideInInspector] public MapLocation currentLocation;

    [SerializeField] private MapLocation startingLocation;


    [SerializeField] public int moveSpeed;
    private bool _canMove = true;
    private InputAction _moveNorth;
    private InputAction _moveSouth;
    private InputAction _moveWest;
    private InputAction _moveEast;
    private InputAction _enter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        locations ??= new List<MapLocation>();
        availableLocations ??= new List<MapLocation>();

        if (SaveManager.CurrentSave == null || string.IsNullOrEmpty(SaveManager.CurrentSave.locationId))
        {
            SaveManager.CurrentSave.locationId = startingLocation.id;
        }

        // Gather all the mapLocations
        MapLocation[] mapLocations = FindObjectsByType<MapLocation>(FindObjectsSortMode.None);
        foreach (MapLocation location in mapLocations)        {
            locations.Add(location);
            print("Found location: " + location.id);
        }

        // Get the current location
        currentLocation = locations.Find(l => l.id == SaveManager.CurrentSave.locationId);

        if(currentLocation == null)
        {
            Debug.LogError($"Could not find current location with id {SaveManager.CurrentSave.locationId}, defaulting to first location on the map");
            currentLocation = locations[0];
        }
        // Place the player at the current location
        player.transform.position = currentLocation.transform.position;
    }

    [SerializeField] public InputActionAsset inputActions;


    void OnEnable()
    {
        var mapActionMap = inputActions.FindActionMap("Map");
        mapActionMap.Enable();

        _moveNorth = inputActions["Map/MoveNorth"];
        _moveSouth = inputActions["Map/MoveSouth"];
        _moveWest = inputActions["Map/MoveWest"];
        _moveEast = inputActions["Map/MoveEast"];
        _enter = inputActions["Map/Enter"];

        _moveNorth.performed += OnMoveNorth;
        _moveSouth.performed += OnMoveSouth;
        _moveWest.performed += OnMoveWest;
        _moveEast.performed += OnMoveEast;

        _enter.performed += OnEnter;
    }

    void OnDisable()
    {
        if (_moveNorth != null) _moveNorth.performed -= OnMoveNorth;
        if (_moveSouth != null) _moveSouth.performed -= OnMoveSouth;
        if (_moveWest != null) _moveWest.performed -= OnMoveWest;
        if (_moveEast != null) _moveEast.performed -= OnMoveEast;
        if (_enter != null) _enter.performed -= OnEnter;

        if (inputActions != null)
        {
            inputActions.FindActionMap("Map")?.Disable();
        }
    }

    private void OnMoveNorth(InputAction.CallbackContext context) => AttemptMove(context, MapDirection.North);
    private void OnMoveSouth(InputAction.CallbackContext context) => AttemptMove(context, MapDirection.South);
    private void OnMoveWest(InputAction.CallbackContext context) => AttemptMove(context, MapDirection.West);
    private void OnMoveEast(InputAction.CallbackContext context) => AttemptMove(context, MapDirection.East);

    private void OnEnter(InputAction.CallbackContext context)
    {
        if(_canMove)
        {
            currentLocation.sceneToLoad.Load();
        }
    }

    private void AttemptMove(InputAction.CallbackContext context, MapDirection direction)
    {
        if (!_canMove) return;
        if (currentLocation == null)
        {
            Debug.LogError("AttemptMove called before currentLocation was initialized.");
            return;
        }

        if (currentLocation.connectedLocations == null)
        {
            Debug.LogError($"currentLocation {currentLocation.name} has no connection cache. This usually means a stale MapController instance or a MapLocation Awake/setup issue.");
            return;
        }

        MapLocation newLocation = currentLocation.GetConnection(direction);
        if (newLocation != null)
        {
            if(newLocation.isLocked) {
                StartCoroutine(ShowLockedMessage());
                return;
            }else {
                StartCoroutine(MovePlayerToLocation(newLocation));
            }
        }
    }

    public IEnumerator MovePlayerToLocation(MapLocation location)
    {
        print("Moving player to " + location.name);
        currentLocation = location;
        SaveManager.CurrentSave.locationId = location.id;
        _canMove = false;
        print("CanMove" + _canMove);
        if(player.transform.position != location.transform.position)
        {
            float elapsedTime = 0f;
            Vector3 startingPos = player.transform.position;
            Vector3 targetPos = location.transform.position;
            while (elapsedTime < 1f)
            {
                player.transform.position = Vector3.Lerp(startingPos, targetPos, elapsedTime);
                elapsedTime += Time.deltaTime / 1f; // Adjust the divisor to change the speed
                yield return null;
            }
            player.transform.position = targetPos; // Ensure it ends exactly at the target position
        }
        _canMove = true;
    }

    public IEnumerator ShowLockedMessage()
    {
        // TODO: Show some UI message that the location is locked
        print("Location is locked!");
        yield return new WaitForSeconds(2f);
    }
}
}
