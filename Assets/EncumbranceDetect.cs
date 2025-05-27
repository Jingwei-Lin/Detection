using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class EncumbranceDetect : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Threshold ratio for encumbrance detection (e.g., 0.5 = left is 50% of right)")]
    public float threshold = 0.6f;
    [Tooltip("Time window (seconds) to average sensor data")]
    public float sampleWindow = 2.0f;
    [Tooltip("Weight between linear and angular velocity (0-1)")]
    [Range(0, 1)] public float linearVelocityWeight = 0.7f;

    [Header("Debug UI")]
    public Text leftVelocityText;
    public Text rightVelocityText;
    public Text ratioText;
    public Text statusText;

    private List<float> leftVelocityData = new List<float>();
    private List<float> rightVelocityData = new List<float>();
    private float epsilon = 0.0001f; // Prevents division by zero

    void Update()
    {
        // Get combined velocity metrics
        float leftVelocity = GetControllerVelocityMetric(InputDeviceCharacteristics.Left);
        float rightVelocity = GetControllerVelocityMetric(InputDeviceCharacteristics.Right);

        UpdateBuffer(leftVelocity, ref leftVelocityData);
        UpdateBuffer(rightVelocity, ref rightVelocityData);

        if (leftVelocityData.Count == 0 || rightVelocityData.Count == 0) return;

        float leftAvg = leftVelocityData.Average();
        float rightAvg = rightVelocityData.Average();

        UpdateDebugUI(leftAvg, rightAvg);
        DetectEncumbrance(leftAvg, rightAvg);
    }

    private float GetControllerVelocityMetric(InputDeviceCharacteristics characteristics)
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(characteristics, devices);

        foreach (var device in devices)
        {
            if (device.isValid)
            {
                // Get both linear and angular velocity
                Vector3 linearVel = Vector3.zero;
                Vector3 angularVel = Vector3.zero;
                
                bool hasLinear = device.TryGetFeatureValue(CommonUsages.deviceVelocity, out linearVel);
                bool hasAngular = device.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out angularVel);

                // Combine velocities using weighted average
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

    private void DetectEncumbrance(float left, float right)
    {
        // Add epsilon to prevent NaN
        float safeLeft = left + epsilon;
        float safeRight = right + epsilon;
        float totalMovement = safeLeft + safeRight;

        if (totalMovement < 0.1f)
        {
            statusText.text = "Encumbered: Low Movement";
            return;
        }

        float ratio = safeLeft / safeRight;
        float invRatio = 1 / ratio;

        if (ratio < threshold)
        {
            statusText.text = $"Encumbered: LEFT (Ratio: {ratio:F2})";
            statusText.color = Color.red;
        }
        else if (invRatio < threshold)
        {
            statusText.text = $"Encumbered: RIGHT (Ratio: {invRatio:F2})";
            statusText.color = Color.red;
        }
        else
        {
            statusText.text = "Normal Movement";
            statusText.color = Color.green;
        }
    }

    // Modified buffer update with frame-rate independent sampling
    private void UpdateBuffer(float newValue, ref List<float> buffer)
    {
        buffer.Add(newValue);
        int maxSamples = Mathf.CeilToInt(sampleWindow / Time.fixedDeltaTime);
        while (buffer.Count > maxSamples) buffer.RemoveAt(0);
    }

    private void UpdateDebugUI(float left, float right)
    {
        if(leftVelocityText != null) 
            leftVelocityText.text = $"Left: {left:F2}";
        
        if(rightVelocityText != null)
            rightVelocityText.text = $"Right: {right:F2}";

        float ratio = (left + epsilon) / (right + epsilon);
        if(ratioText != null)
            ratioText.text = $"L/R Ratio: {ratio:F2}";
    }
}