using VECS;
using VECS.ECS;
using VECS.ECS.Transforms;

namespace COMP302
{
    public class RotatorSystem : SystemBase
    {
        private const float ROTATION_SPEED = 3;
        private EntityQuery _rotateObjects;
        public override void OnCreate(EntityManager entityManager)
        {
            _rotateObjects = new EntityQuery(entityManager)
                .WithAll(typeof(RotateObject), typeof(Rotation))
                .Build();
        }

        public override void OnUpdate(EntityManager entityManager)
        {
            if (_rotateObjects.HasEntities)
            {
                _rotateObjects.GetEntities().ForEach(entity =>
                {
                    var rotation = entityManager.GetComponent<Rotation>(entity);
                    rotation.Value.Y += Time.DeltaTime * float.DegreesToRadians(ROTATION_SPEED) % float.DegreesToRadians(360);
                    entityManager.SetComponent(entity, rotation);
                });
            }
        }
    }

    public struct RotateObject : IComponent
    {
        public static int ComponentId { get; set; }
        public readonly int Id => ComponentId;
    }
}
