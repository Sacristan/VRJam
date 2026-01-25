using System;
using BNG;
using UnityEngine;

public class HandGraphics : MonoBehaviour
{
    [SerializeField] private HandPoser handPoser;
    [SerializeField] private HandPoseBlender poseBlender;
    [SerializeField] private AutoPoser autoPoser;
    [SerializeField] private Transform center;
    
    public Transform Transform => transform;
    public HandPoser HandPoser => handPoser;
    public HandPoseBlender PoseBlender => poseBlender;
    public AutoPoser AutoPoser => autoPoser;
    public Transform Center => transform;

    private void OnValidate()
    {
        if (handPoser == null) handPoser = GetComponent<HandPoser>();
        if (poseBlender == null) poseBlender = GetComponent<HandPoseBlender>();
    }

    public void SetParent(Transform parent)
    {
        Transform.SetParent(parent);
    }

    public void MoveCenterTo(Vector3 targetCenterPos)
    {
        Vector3 posDiff = targetCenterPos - Center.position;
        Transform.position += posDiff;
    }
}