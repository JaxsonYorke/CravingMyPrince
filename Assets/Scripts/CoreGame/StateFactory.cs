using UnityEngine;

public static class StateFactory
{
    public static State InitState(GameObject character)
    {
        if (character.name == "Princess")
            return new PrincessState();
        else if (character.name == "Monster")
            return new MonsterState();
        else return new State();
    }
}



public class State
{
    private bool isMoving;
    private bool isJumping;
    private bool isFalling;
    private bool isGrounded;
    private bool isDead;
    public enum HitWall {none, left, right};
    private HitWall wallHit;

    public State()
    {
        isMoving = false;
        isJumping = false;
        isFalling = false;
        isDead = false;
        isGrounded = false;
        wallHit = HitWall.none;
    }

    public State GetState(){return this;}

    public bool IsMoving { get => isMoving; set => isMoving = value; }
    public bool IsJumping { get => isJumping; set => isJumping = value; }
    public bool IsFalling { get => isFalling; set => isFalling = value; }
    public bool IsInAir { get => (isJumping || isFalling) && !isGrounded; } //! If you add a way the character is in the air this should also be changed to include that
    public bool IsGrounded { get => isGrounded; set => isGrounded = value; }
    public bool IsDead { get => isDead; set => isDead = value; }
    public HitWall WallHit { get => wallHit; set => wallHit = value; }


}



public class MonsterState : State
{
    private bool isCarrying;
    private bool princessOnTop;

    public MonsterState() : base()
    {
        isCarrying = false;
        princessOnTop = false;
    }


    public bool IsCarrying { get => isCarrying; set => isCarrying = value; }
    public bool PrincessOnTop { get => princessOnTop; set => princessOnTop = value; }
}

public class PrincessState : State
{
    private bool isOnMonster;

    public PrincessState() : base()
    {
        isOnMonster = false;
    }

    public bool IsOnMonster { get => isOnMonster; set => isOnMonster = value; }
}