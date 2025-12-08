using UnityEngine;

[RequireComponent(typeof(Transform))]
public class CameraControler : MonoBehaviour
{
    public GameObject Monster;
    public GameObject Princess;
    private Transform princessTransform;
    private Transform monsterTransform;
    private Transform cameraTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        princessTransform = Princess.GetComponent<Transform>();
        monsterTransform = Monster.GetComponent<Transform>();
        cameraTransform = GetComponent<Transform>();
    }

    // Update is called once per frame after all Update functions have been called
    // This is to make sure the camera moves after the characters have moved
    void LateUpdate()
    {
        if(Princess == null || Monster == null){
            return;
        }
        // Move the camera to the average position of the princess and monster
        // Keep the z position of the camera at -10 to avoid clipping issues
        cameraTransform.position = new Vector3((princessTransform.position.x + monsterTransform.position.x) / 2, (princessTransform.position.y + monsterTransform.position.y) / 2, -10);
    }
}
