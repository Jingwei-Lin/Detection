using UnityEngine;

public class BodyTrack : MonoBehaviour
{
    [SerializeField] private float hipSpeedThreshold = 0.5f; // m/s
    private OVRSkeleton _skeleton;
    private Vector3 _previousHipPosition;

    void Start()
    {
        _skeleton = FindObjectOfType<OVRSkeleton>();
    }

    void Update()
    {
        if (!_skeleton.IsInitialized) return;

        var hipBone = _skeleton.Bones[(int)OVRSkeleton.BoneId.Body_Hips];
        float speed = (hipBone.Transform.position - _previousHipPosition).magnitude / Time.deltaTime;
        _previousHipPosition = hipBone.Transform.position;

        if (speed > hipSpeedThreshold)
        {
            Debug.Log($"Walking detected. Speed: {speed:F2} m/s");
        }
    }
}