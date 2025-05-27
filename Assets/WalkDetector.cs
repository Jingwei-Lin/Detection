using UnityEngine;
using UnityEngine.UI;

public class WalkDetector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float positionThreshold = 0.001f;
    [SerializeField] private float bobThreshold = 0.001f;
    [SerializeField] private float pitchThreshold = 1f;
    [SerializeField] private float smoothingFactor = 0.1f;

    [Header("UI")]
    [SerializeField] private Text debugText;
    [SerializeField] private Text isWalkingText;

    private Vector3 previousPosition;
    private float previousXRotation;
    private float smoothedBob;

    void Start()
    {
        previousPosition = Camera.main.transform.localPosition;
        previousXRotation = Camera.main.transform.localEulerAngles.x;
    }

    void Update()
    {
        Vector3 currentPosition = Camera.main.transform.localPosition;
        Vector3 headRotation = Camera.main.transform.localEulerAngles;

        // Horizontal movement (XZ plane)
        float horizontalMovement = Vector2.Distance(
            new Vector2(currentPosition.x, currentPosition.z),
            new Vector2(previousPosition.x, previousPosition.z)
        );

        // Vertical head bob (with smoothing)
        float rawBob = Mathf.Abs(currentPosition.y - previousPosition.y);
        smoothedBob = smoothingFactor * smoothedBob + (1 - smoothingFactor) * rawBob;

        // Pitch change (head tilt)
        float pitchChange = Mathf.DeltaAngle(previousXRotation, headRotation.x);

        // Debug output
        debugText.text = $"Horizontal: {horizontalMovement:F3}\n" +
                       $"Vertical: {smoothedBob:F3}\n" +
                       $"Rotation: {pitchChange:F3}°";


        // Detection logic
        bool isWalking = horizontalMovement >= positionThreshold && 
                       (smoothedBob >= bobThreshold || Mathf.Abs(pitchChange) >= pitchChange);

        isWalkingText.text = $"\nWalking: {(isWalking ? "YES" : "NO")}";
        isWalkingText.color = isWalking ? Color.green : Color.red;
        // Update previous values
        previousPosition = currentPosition;
        previousXRotation = headRotation.x;
    }
}