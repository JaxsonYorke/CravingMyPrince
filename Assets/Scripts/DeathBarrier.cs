using UnityEngine;

public class DeathBarrier : MonoBehaviour
{

    [SerializeField] private GameLoopControler _gameLoopControler;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Princess")
        {
            _gameLoopControler.tryKillPrincess();
        }
        
        if (collision.gameObject.tag == "Monster")
        {
            _gameLoopControler.tryKillMonster();
        }
    }
}
