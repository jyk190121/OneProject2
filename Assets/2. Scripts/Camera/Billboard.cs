using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        // UI가 항상 카메라를 바라보게 함
        transform.LookAt(transform.position + cam.forward);
    }
}