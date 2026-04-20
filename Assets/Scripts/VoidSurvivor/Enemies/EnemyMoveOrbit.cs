using UnityEngine;

namespace VoidSurvivor
{
    public sealed class EnemyMoveOrbit : EnemyMoveBase
    {
        protected override void Move(float deltaTime)
        {
            Vector2 forward = DirectionToTarget();
            Vector2 side = new Vector2(-forward.y, forward.x);
            float orbit = Config != null ? Config.WaveAmplitude : 1f;
            transform.position += (Vector3)((forward * MoveSpeed * 0.72f + side * orbit * MoveSpeed * 0.55f) * deltaTime);
        }
    }
}
