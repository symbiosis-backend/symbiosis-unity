using UnityEngine;

namespace VoidSurvivor
{
    public sealed class EnemyMoveChase : EnemyMoveBase
    {
        protected override void Move(float deltaTime)
        {
            transform.position += (Vector3)(DirectionToTarget() * MoveSpeed * deltaTime);
        }
    }
}
