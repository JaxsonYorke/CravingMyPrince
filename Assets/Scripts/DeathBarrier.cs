using UnityEngine;
using UnityEngine.Serialization;

public class DeathBarrier : MonoBehaviour
{

    [FormerlySerializedAs("_gameLoopControler")] [SerializeField] private GameLoopControler gameLoopControler;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        switch (collision.gameObject.tag)
        {
            case "Princess":
                gameLoopControler.tryKillPrincess();
                break;
            case "Monster":
                gameLoopControler.tryKillMonster();
                break;
        }
    }
}
