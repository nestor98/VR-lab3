using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour
{
    // Move in xz with WASD, look around with the mouse.
    // Press SPACE to show/hide the cursor
    // Press 1..4 to choose between the four rooms

    public float rotSpeed = 240.0f;
    public float moveSpeed = 4.0f;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        ApplyInputs();


        if (Input.GetKeyDown("space")) // Toggle cursor
            Cursor.visible = !Cursor.visible;
        
    }

    private void ApplyInputs()
    {
        // Rotation input:
        float y = Input.GetAxis("Mouse X"),
              x = -Input.GetAxis("Mouse Y");

        Vector3 euler = new Vector3(x, y, 0) * rotSpeed * Time.deltaTime;
        euler += transform.eulerAngles;

        // Movement input:
        Vector3 movement = MovementInputs();
        if (movement.sqrMagnitude > 1e-6)
        {
            transform.eulerAngles = new Vector3(0, euler.y, 0); // Only move in xz plane
            transform.Translate(movement * moveSpeed * Time.deltaTime);
        }

        // Restore latitude
        transform.eulerAngles = euler;
    }
    
    // Returns the Vector3 with the movement inputs
    private Vector3 MovementInputs()
    {
        float x = Input.GetAxis("Horizontal"), z = Input.GetAxis("Vertical");
        return new Vector3(x, 0, z);
    }


}
