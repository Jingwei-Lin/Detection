using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public class HandVisibilityDetect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Text leftHandText;
    [SerializeField] private Text rightHandText;
    [SerializeField] private Text debug;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.1f;

    private XRHandSubsystem handSubsystem;
    private float timer;

    void Start() => InitializeHandTracking();

    void Update()
    {
        if (handSubsystem == null) return;

        timer += Time.deltaTime;
        if (timer < updateInterval) return;

        UpdateHandStatus(handSubsystem.leftHand, leftHandText, "Left Hand");
        UpdateHandStatus(handSubsystem.rightHand, rightHandText, "Right Hand");
        timer = 0;
    }

    private void InitializeHandTracking()
    {
        handSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?
        .GetLoadedSubsystem<XRHandSubsystem>();

        if (handSubsystem != null)
        {
            handSubsystem.Start();
            Debug.Log("Hand tracking initialized");
            debug.text = "Hand tracking initialized";
        }
        else
        {
            Debug.LogError("XRHandSubsystem not found!");
            debug.text = "XRHandSubsystem not found!";
        }

        Invoke(nameof(InitializeHandTracking), 2f);
    }

    private void UpdateHandStatus(XRHand hand, Text statusText, string handName)
    {
        if (!hand.isTracked)
        {
            Debug.LogWarning($"{handName} not tracked");
            statusText.text = $"{handName}: Not Tracked";
            return;
        }

        int totalJoints = 0;
        int obstructedJoints = 0;

        foreach (XRHandJointID jointID in System.Enum.GetValues(typeof(XRHandJointID)))
        {
            if (jointID == XRHandJointID.Invalid) continue;

            var joint = hand.GetJoint(jointID);
            Debug.Log($"{jointID}: {joint.trackingState}");
            totalJoints++;

            // Detect obstructions using HighFidelityPose state
            bool isObstructed = (joint.trackingState & XRHandJointTrackingState.HighFidelityPose) == 0;
            if (isObstructed) obstructedJoints++;
            
            debug.text = $"{joint.trackingState}\n" + 
                        $"{XRHandJointTrackingState.HighFidelityPose}";
        }

        float obstructionPercent = (float)obstructedJoints / totalJoints * 100;
        statusText.text = $"{handName}:\n" +
                        $"{100 - obstructionPercent:F0}% Visible\n" +
                        $"{obstructionPercent:F0}% Obstructed";
    }

    void OnDestroy()
    {
        if (handSubsystem != null)
            handSubsystem.Stop();
    }
}
