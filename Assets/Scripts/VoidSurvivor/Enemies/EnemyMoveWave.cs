using UnityEngine;

namespace VoidSurvivor
{
    public sealed class EnemyMoveWave : EnemyMoveBase
    {
        protected override void Move(float deltaTime)
        {
            Vector2 forward = DirectionToTarget();
            Vector2 side = new Vector2(-forward.y, forward.x);
            float amplitude = Config != null ? Config.WaveAmplitude : 1f;
            float frequency = Config != null ? Config.WaveFrequency : 3f;
            Vector2 wave = side * Mathf.Sin(Age * frequency) * amplitude;
            transform.position += (Vector3)((forward * MoveSpeed + wave) * deltaTime);
        }
    }
}
