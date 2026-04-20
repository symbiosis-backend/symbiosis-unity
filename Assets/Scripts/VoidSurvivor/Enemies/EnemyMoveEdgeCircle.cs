using UnityEngine;

namespace VoidSurvivor
{
    public sealed class EnemyMoveEdgeCircle : EnemyMoveBase
    {
        protected override void Move(float deltaTime)
        {
            Vector2 forward = DirectionToTarget();
            Vector2 side = new Vector2(-forward.y, forward.x) * Mathf.Sin(Age * 2.5f);
            transform.position += (Vector3)((forward * MoveSpeed * 0.8f + side * MoveSpeed * 0.7f) * deltaTime);
        }
    }
}
