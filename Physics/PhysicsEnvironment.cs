namespace Vulkano.Physics
{
    internal class PhysicsEnvironment
    {

        public const float PHYSICS_UPDATE_FREQUENCY = 200.0f;
        public const float PHYSICS_UPDATE_INTERVAL = 1.0f / PHYSICS_UPDATE_FREQUENCY;

        public const float GRAVITY = 40.0f;

        private readonly IList<IPhysicsObject> _physicsObjects = new List<IPhysicsObject>();

        private float _totalTime;
        private float _time = 0.0f;

        public event Action<float, float>? ConstantTimeTickHook;

        public void Update(float delta)
        {
            _time += delta;
            _totalTime += delta;
            while (_time >= PHYSICS_UPDATE_INTERVAL)
            {
                ConstantTimeTickHook?.Invoke(PHYSICS_UPDATE_INTERVAL, _time);
                foreach (IPhysicsObject physicsObject in _physicsObjects)
                {
                    physicsObject.Tick(PHYSICS_UPDATE_INTERVAL, _totalTime);
                }
                _time -= PHYSICS_UPDATE_INTERVAL;
            }
        }

        public void AddPhysicsObject(IPhysicsObject physicsObject)
        {
            _physicsObjects.Add(physicsObject);
        }

        public void RemovePhysicsObject(IPhysicsObject physicsObject)
        {
            _physicsObjects.Remove(physicsObject);
        }

    }
}
