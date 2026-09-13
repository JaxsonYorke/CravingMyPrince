#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using Assets.utils.dataStructures;
using MapDirection = Assets.utils.enums.MapDirection;
using System.Collections.Generic;
using TMPro;


public class MapLocation : MonoBehaviour
{
    [SerializeField] public string id;
    [SerializeField] public string locationName;
    [SerializeField] public bool isLocked;

    [SerializeField] public SceneReference sceneToLoad;

    [SerializeField]
    public List<MapLocationConnection> connections;

    public DirectionalConnections<MapLocation> connectedLocations;

    public void UnlockLocation()
    {
        isLocked = false;
    }

    public void Awake()
    {
        connectedLocations = new DirectionalConnections<MapLocation>();
        foreach (var connection in connections)
        {
            if (connection.target == null)
            {
                Debug.LogWarning($"MapLocation {name} has a null connection in direction {connection.direction}");
                continue;
            }
            connectedLocations.AddConnection(connection.direction, connection.target);
        }
    }
    void Start()
    {
        var textComponent = GetComponentInChildren<TextMeshPro>();
        if (textComponent != null)
        {
            textComponent.text = locationName;
        } else
        {
            Debug.LogWarning($"MapLocation {name} does not have a TextMeshProUGUI component in its children to display the location name.");
        }
    }

    public MapLocation GetConnection(MapDirection direction)
    {
        return connectedLocations.GetConnection(direction);
    }


    private void OnValidate()
    {
        var used = new HashSet<MapDirection>();
        foreach (var c in connections)
        {
            if (!used.Add(c.direction))
                Debug.LogWarning(
                    $"Duplicate direction {c.direction} on {name}",
                    this);
        }

        #if UNITY_EDITOR
        if(string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
        }
        #endif
    }


#region gizmos
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (connections == null) return;

        foreach (var c in connections)
        {
            if (c.target == null) continue;

            Vector3 start = transform.position + (c.direction switch
            {
                MapDirection.North => new Vector3(0, 0.75f, 0),
                MapDirection.South => new Vector3(0, -0.75f, 0),
                MapDirection.West => new Vector3(-0.75f, 0, 0),
                MapDirection.East => new Vector3(0.75f, 0, 0),
                _ => Vector3.zero
            });
            Vector3 end = c.target.transform.position - (c.direction switch
            {
                MapDirection.North => new Vector3(0, 0.75f, 0),
                MapDirection.South => new Vector3(0, -0.75f, 0),
                MapDirection.West => new Vector3(-0.75f, 0, 0),
                MapDirection.East => new Vector3(0.75f, 0, 0),
                _ => Vector3.zero
            });
            Vector3 mid = Vector3.Lerp(start, end, 0.5f);


            if(start == end) throw new System.Exception($"MapLocation {name} has a connection to {c.target.name} but they are in the same position, so no line will be drawn. Please move one of them.");


            Handles.DrawAAPolyLine(4f, start, end);
            Handles.Label(mid, c.direction.ToString(), new GUIStyle
            {
                normal = new GUIStyleState { textColor = Color.white },
                fontStyle = FontStyle.Bold,
                fontSize = 18
            });
            if(c.target.isLocked)
            {
                Handles.Label(mid - new Vector3(0, 0.3f, 0), "LOCKED", new GUIStyle
                {
                    normal = new GUIStyleState { textColor = Color.red },
                    fontStyle = FontStyle.Bold,
                    fontSize = 14
                });  
                Handles.color = Color.gray;
            } else
            {
                Handles.color = GetColorForDirection(c.direction);
            }

            DrawArrowHeadHandles(start, end);
        }
    }

    private Color GetColorForDirection(MapDirection dir)
    {
        return dir switch
        {
            MapDirection.North => Color.green,
            MapDirection.South => Color.red,
            MapDirection.West => Color.blue,
            MapDirection.East => Color.yellow,
            _ => Color.white
        };
    }

    private void DrawArrowHeadHandles(Vector3 from, Vector3 to)
    {
        Vector3 dir = (to - from).normalized;
        float size = 0.3f;

        Handles.ConeHandleCap(
            0,
            to - dir * size,
            Quaternion.LookRotation(dir),
            size,
            EventType.Repaint
        );
    }
#endif
#endregion

}


[System.Serializable]
public class MapLocationConnection
{
    public MapLocation target;
    public MapDirection direction;
}