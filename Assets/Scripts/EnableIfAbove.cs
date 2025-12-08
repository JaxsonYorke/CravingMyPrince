using UnityEngine;

public class EnableIfAbove : MonoBehaviour
{
    public GameObject targetObject;
    public BoxCollider2D targetCollider;
    public GameObject selfObject;

    void Update()
    {
        if (targetObject != null)
        {
            // Check if the target object (princess) is above the platform collider
            float platformTopY = selfObject.transform.position.y + targetCollider.offset.y * 2 + targetObject.transform.localScale.y /2;
            if (targetObject.transform.position.y > platformTopY)
            {
                selfObject.GetComponent<BoxCollider2D>().enabled = true;
                selfObject.layer = LayerMask.NameToLayer("Default");
            }
            else
            {
                selfObject.GetComponent<BoxCollider2D>().enabled = false;
                selfObject.layer = LayerMask.NameToLayer("Monster");
            }
        }
    }
}
