using UnityEngine;

namespace VoidSurvivor
{
    public abstract class EnemyMoveBase : MonoBehaviour
    {
        protected Transform Target { get; private set; }
        protected VoidEnemyConfig Config { get; private set; }
        protected float SpeedMultiplier { get; private set; } = 1f;
        protected float Age { get; private set; }

        public virtual void Initialize(Transform target, VoidEnemyConfig config, float speedMultiplier)
        {
            Target = target;
            Config = config;
            SpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            Age = 0f;
        }

        protected virtual void Update()
        {
            Age += Time.deltaTime;
            Move(Time.deltaTime);
        }

        protected abstract void Move(float deltaTime);

        protected Vector2 DirectionToTarget()
        {
            if (Target == null)
                return Vector2.down;

            Vector2 direction = Target.position - transform.position;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.down;
        }

        protected float MoveSpeed => Config != null ? Config.MoveSpeed * SpeedMultiplier : 2.5f;
    }
}
