using UnityEngine;

public class XRPlayer : MonoBehaviour
{
    private XRPlayerHands _hands;
    public XRPlayerHands Hands => gameObject.GetLazyComponentInChildren(ref _hands);
}