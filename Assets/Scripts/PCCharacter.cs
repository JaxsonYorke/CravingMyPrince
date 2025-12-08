using UnityEngine;

public class PCCharacter : MonoBehaviour
{

    protected bool IsGrounded()
    {
        // Check for HeadPlatformCollider or Tilemap below the character
        // Use -1 as layerMask to check all layers regardless of collision matrix
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            new Vector2(this.gameObject.transform.position.x, this.gameObject.transform.position.y - this.gameObject.transform.localScale.y / 2 - 0.1f), // slightly below the character
            new Vector2(this.gameObject.transform.localScale.x / 2, 0.1f), // width, height of the box
            0f, // angle
            -1
        );

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject.name == "HeadPlatformCollider" ||
                hit.gameObject.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null)
            {
                return true;
            }
        }
        return false;
    }

    // This destroys the character and triggers game over
    public void Death()
    {
        Destroy(this.gameObject);
        Debug.LogFormat("{0} is dead", this.gameObject.name);
        GameObject.Find("GameLoopController").GetComponent<GameLoopControler>().GameOver();
    }


}