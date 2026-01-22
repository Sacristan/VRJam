using System;
using BNG;
using UnityEngine;

public class HandGraphics : MonoBehaviour
{
    [SerializeField] private HandPoser handPoser;
    [SerializeField] private HandPoseBlender poseBlender;
    [SerializeField] private AutoPoser autoPoser;

    public Transform Transform => transform;
    public HandPoser HandPoser => handPoser;
    public HandPoseBlender PoseBlender => poseBlender;
    public AutoPoser AutoPoser => autoPoser;

    private void OnValidate()
    {
        if(handPoser == null) handPoser = GetComponent<HandPoser>();
        if(poseBlender == null) poseBlender = GetComponent<HandPoseBlender>();
    }
}