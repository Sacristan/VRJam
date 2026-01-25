using System;
using UnityEngine;

public class XRPlayer : MonoBehaviour
{
    public static XRPlayer Instance { get; private set; }
    
    private XRPlayerHands _hands;
    public XRPlayerHands Hands => gameObject.GetLazyComponentInChildren(ref _hands);

    private void Awake()
    {
        Instance = this;
    }
}