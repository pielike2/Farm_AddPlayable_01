namespace Utility.SensorSystem
{
    public interface ISensorTarget : IUnityObject
    {
        HashId SenseId { get; }
        bool IsColliderActive { get; }
    }
}