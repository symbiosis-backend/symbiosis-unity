using UnityEngine;

namespace VoidSurvivor
{
    public sealed class EnemyMoveDown : EnemyMoveBase
    {
        protected override void Move(float deltaTime)
        {
            transform.position += Vector3.down * MoveSpeed * deltaTime;
        }
    }
}
