using UnityEngine;

public class Finger : MonoBehaviour
{
    public enum Type
    {
        Thumb,
        Index,
        Middle,
        Ring,
        Pinky
    }

    [SerializeField] private Type fingerType;

    public Type FingerType
    {
        get => fingerType;
        private set => fingerType = value;
    }

    public Collider Collider { get; private set; }

    public Finger Init(Type descriptorFingerType)
    {
        FingerType = descriptorFingerType;
        Collider = GetComponent<Collider>();
        return this;
    }
}