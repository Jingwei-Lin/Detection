using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class WalkEncumberDetection : MonoBehaviour
{
    [Header("Walk Detection Settings")]
    [SerializeField] private float positionThreshold = 0.005f;
    [SerializeField] private float bobThreshold = 0.0008f;
    [SerializeField] private float pitchThreshold = 1f;
    [SerializeField] private float smoothingFactor = 0.1f;
    [SerializeField] private float sampleWindowWalk = 1.0f; // New setting for walking

    [Header("Encumbrance Settings")]
    [Tooltip("Threshold ratio for encumbrance detection")]
    [SerializeField] private float encumbranceThreshold = 0.6f;
    [SerializeField] private float sampleWindow = 2.0f;
    [Range(0, 1)] [SerializeField] private float linearVelocityWeight = 0.7f;

    [Header("UI References")]
    [SerializeField] private Text debugTextWalk;
    [SerializeField] private Text debugTextEncumber;
    [SerializeField] private Text isWalkingText;
    [SerializeField] private Text encumbranceStatusText;

    // Walk Detection Variables
    private Vector3 previousPosition;
    private float previousXRotation;
    private float smoothedBob;
    private List<float> horizontalMovementData = new List<float>();
    private List<float> bobData = new List<float>();

    // Encumbrance Variables
    private List<float> leftVelocityData = new List<float>();
    private List<float> rightVelocityData = new List<float>();
    private readonly float epsilon = 0.0001f;

    void Start()
    {
        previousPosition = Camera.main.transform.localPosition;
        previousXRotation = Camera.main.transform.localEulerAngles.x;
    }

    void Update()
    {
        DetectWalking();
        DetectEncumbrance();
    }

    private void DetectWalking()
    {
        Vector3 currentPosition = Camera.main.transform.localPosition;
        Vector3 headRotation = Camera.main.transform.localEulerAngles;

        // Horizontal movement
        float horizontalMovement = Vector2.Distance(
            new Vector2(currentPosition.x, currentPosition.z),
            new Vector2(previousPosition.x, previousPosition.z)
        );

        // Head bob (smoothed)
        float rawBob = Mathf.Abs(currentPosition.y - previousPosition.y);
        smoothedBob = smoothingFactor * smoothedBob + (1 - smoothingFactor) * rawBob;

        // Pitch change
        float pitchChange = Mathf.DeltaAngle(previousXRotation, headRotation.x);

        // Add current values to buffers
        UpdateBuffer(sampleWindowWalk, horizontalMovement, ref horizontalMovementData);
        UpdateBuffer(sampleWindowWalk, smoothedBob, ref bobData);

        // Use averaged values instead of instantaneous values
        float avgHorizontal = horizontalMovementData.Average();
        float avgBob = bobData.Average();

        // detect walking
        bool isWalking = avgHorizontal >= positionThreshold && (avgBob >= bobThreshold || Mathf.Abs(pitchChange) >= pitchThreshold);

        // Update walk UI
        debugTextWalk.text = $"Horizontal: {avgHorizontal:F4}\nVertical: {avgBob:F4}\nRotation: {pitchChange:F3}°\n";
        
        isWalkingText.text = $"Walking: {(isWalking ? "YES" : "NO")}";
        isWalkingText.color = isWalking ? Color.green : Color.red;

        // Update tracking variables
        previousPosition = currentPosition;
        previousXRotation = headRotation.x;
    }

    private void DetectEncumbrance()
    {
        float leftVelocity = GetControllerVelocityMetric(InputDeviceCharacteristics.Left);
        float rightVelocity = GetControllerVelocityMetric(InputDeviceCharacteristics.Right);

        UpdateBuffer(sampleWindow, leftVelocity, ref leftVelocityData);
        UpdateBuffer(sampleWindow, rightVelocity, ref rightVelocityData);

        if (leftVelocityData.Count == 0 || rightVelocityData.Count == 0) return;

        float leftAvg = leftVelocityData.Average();
        float rightAvg = rightVelocityData.Average();

        // Update encumbrance UI
        debugTextEncumber.text = $"Left: {leftAvg:F2}\nRight: {rightAvg:F2}\nL/R Ratio: {(leftAvg + epsilon) / (rightAvg + epsilon):F2}\n";


        // Encumbrance logic
        float safeLeft = leftAvg + epsilon;
        float safeRight = rightAvg + epsilon;
        float totalMovement = safeLeft + safeRight;

        if (totalMovement < 0.1f)
        {
            encumbranceStatusText.text = "Low Movement";
            encumbranceStatusText.color = Color.red;
            return;
        }

        float ratio = safeLeft / safeRight;
        float invRatio = 1 / ratio;

        if (ratio < encumbranceThreshold)
        {
            encumbranceStatusText.text = $"Encumbered:\nLEFT (Ratio: {ratio:F2})";
            encumbranceStatusText.color = Color.red;
        }
        else if (invRatio < encumbranceThreshold)
        {
            encumbranceStatusText.text = $"Encumbered:\nRIGHT (Ratio: {invRatio:F2})";
            encumbranceStatusText.color = Color.red;
        }
        else
        {
            encumbranceStatusText.text = "Normal Movement";
            encumbranceStatusText.color = Color.green;
        }
    }

    // Helper methods
    private float GetControllerVelocityMetric(InputDeviceCharacteristics characteristics)
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(characteristics, devices);

        foreach (var device in devices)
        {
            if (device.isValid)
            {
                Vector3 linearVel, angularVel;
                bool hasLinear = device.TryGetFeatureValue(CommonUsages.deviceVelocity, out linearVel);
                bool hasAngular = device.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out angularVel);

                if (hasLinear || hasAngular)
                {
                    float linearMag = linearVel.magnitude;
                    float angularMag = angularVel.magnitude;
                    return (linearMag * linearVelocityWeight) + (angularMag * (1 - linearVelocityWeight));
                }
            }
        }
        return 0f;
    }

    private void UpdateBuffer(float window, float newValue, ref List<float> buffer)
    {
        buffer.Add(newValue);
        int maxSamples = Mathf.CeilToInt(window / Time.fixedDeltaTime); // Fixed: Use deltaTime instead of fixedDeltaTime
        while (buffer.Count > maxSamples) buffer.RemoveAt(0);
    }
}
