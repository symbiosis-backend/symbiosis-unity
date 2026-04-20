using UnityEngine;

namespace VoidSurvivor
{
    public sealed class EnemyMoveZigZag : EnemyMoveBase
    {
        protected override void Move(float deltaTime)
        {
            Vector2 forward = DirectionToTarget();
            Vector2 side = new Vector2(-forward.y, forward.x);
            float frequency = Config != null ? Config.WaveFrequency : 3f;
            float direction = Mathf.Sign(Mathf.Sin(Age * frequency));
            transform.position += (Vector3)((forward * MoveSpeed + side * direction * MoveSpeed * 0.55f) * deltaTime);
        }
    }
}
