using UnityEngine;

namespace VoidSurvivor
{
    public sealed class EnemyMoveDash : EnemyMoveBase
    {
        protected override void Move(float deltaTime)
        {
            float interval = Config != null ? Config.DashInterval : 1.5f;
            float duration = Config != null ? Config.DashDuration : 0.22f;
            float multiplier = Config != null ? Config.DashMultiplier : 3.5f;
            float phase = Mathf.Repeat(Age, Mathf.Max(0.1f, interval));
            float speed = phase <= duration ? MoveSpeed * multiplier : MoveSpeed * 0.35f;
            transform.position += (Vector3)(DirectionToTarget() * speed * deltaTime);
        }
    }
}
