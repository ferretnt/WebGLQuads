using UnityEngine;

public class SimpleCubeRotate : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log($"System graphics API: {SystemInfo.graphicsDeviceType}");
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0.0f, 360.0f * Time.deltaTime, 0.0f);     
    }
}
