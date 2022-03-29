using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour
{
    public float rotSpeed = 240.0f;

    private Transform parent;
    // Start is called before the first frame update
    void Start()
    {
        parent = transform.parent;
    }

    // Update is called once per frame
    void Update()
    {
        // Rotation input:
        float y = Input.GetAxis("Mouse X"),
              x = -Input.GetAxis("Mouse Y");

        Vector3 euler = new Vector3(x, y, 0) * rotSpeed * Time.deltaTime;
        //transform.eulerAngles += euler;

        parent.eulerAngles += new Vector3(0, euler.y, 0);

        transform.eulerAngles += new Vector3(euler.x, 0, 0);
    }
}
