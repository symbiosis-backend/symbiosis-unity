using UnityEngine;

namespace VoidSurvivor
{
    public sealed class EnemyMoveDelayThenChase : EnemyMoveBase
    {
        protected override void Move(float deltaTime)
        {
            float delay = Config != null ? Config.AttackDelay : 0.8f;
            if (Age < delay)
                return;

            transform.position += (Vector3)(DirectionToTarget() * MoveSpeed * 1.35f * deltaTime);
        }
    }
}
