using UnityEngine;
using System.Collections.Generic;

public class GenMap : MonoBehaviour
{
    // Singleton pattern
    public static GenMap Instance;

    public List<PathSample> splineSample;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }
}
[System.Serializable]
public class PathSample
{
    public Vector3 position = Vector3.zero;
    public Vector3 up = Vector3.up;
    public Vector3 forward = Vector3.forward;
    public float size = 1f;
    public double percent = 0.0;

    public Quaternion rotation
    {
        get
        {
            if (up == forward)
            {
                if (up == Vector3.up) return Quaternion.LookRotation(Vector3.up, Vector3.back);
                else return Quaternion.LookRotation(forward, Vector3.up);
            }
            return Quaternion.LookRotation(forward, up);
        }
    }

    public Vector3 right
    {
        get
        {
            if (up == forward)
            {
                if (up == Vector3.up) return Vector3.right;
                else return Vector3.Cross(Vector3.up, forward).normalized;
            }
            return Vector3.Cross(up, forward).normalized;
        }
    }


}


