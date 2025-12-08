using UnityEngine;

public class DeathBarrier : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "Princess")
        {
            collision.gameObject.GetComponent<Princess>().Death();
        }
        
        if (collision.gameObject.name == "Monster")
        {
            collision.gameObject.GetComponent<Monster>().Death();
        }
    }
}
