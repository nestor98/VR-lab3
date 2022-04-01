using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerController : MonoBehaviour
{
    // Move in xz with WASD, look around with the mouse.
    // Press SPACE to show/hide the cursor
    // Press 1..4 to choose between the four rooms
    public enum ControlMode {
        NORMAL,
        TELEPORT_INSTANT,
        TELEPORT_PREVIEW
    };

    public ControlMode controlMode = ControlMode.TELEPORT_INSTANT;


    public float moveSpeed = 4.0f;
    public float rotSpeed = 240.0f;

    // Basketballs interaction:
    public float minThrowForce = 20.0f;
    public float maxThrowForce = 50.0f; // Throwing force
    public float maxForceTime = 1.5f; // 1.5 seconds

    private float currentHeldDownTime = 0.0f;
    //private float throwForce;

    private bool isHolding = false; // Is the player holding a ball?
    private GameObject item = null; // Ball GameObject
    private Vector3 objectPos; // Ball position
    private float distance; // Distance between player and ball

    public float collision_cd = 0.05f;
    private float cooldown = 0.0f;

    private Camera cam;

    // Teleport stuff:
    public string playableTag;
    public float maxTPRange;




    // Start is called before the first frame update
    void Start()
    {
        //throwForce = minThrowForce;
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        // Movement and looking inputs
        ApplyInputs();
        //if (controlMode == TELEPORT_PREVIEW) ApplyInputsNormal();

        // Handle ball interaction:
        UpdateBall();

        // tick down the collision cooldown:
        if (cooldown > 0.0f) cooldown -= Time.deltaTime;

        if (Input.GetKeyDown("space")) // Toggle cursor
            Cursor.visible = !Cursor.visible;
        
    }

    private void ApplyInputs()
    {

        // Rotation input:
        float y = Input.GetAxis("Mouse X"),
              x = -Input.GetAxis("Mouse Y");

        Vector3 euler = new Vector3(x, y, 0) * rotSpeed * Time.deltaTime;
        transform.eulerAngles += euler;

        euler = transform.eulerAngles;

        if (controlMode == ControlMode.NORMAL)
        {
            // Movement input:
            Vector3 movement = MovementInputs();
            if (movement.sqrMagnitude > 1e-6)
            {
                transform.eulerAngles = new Vector3(0, cam.transform.eulerAngles.y, 0); // Only move in xz plane
                transform.Translate(movement * moveSpeed * Time.deltaTime);
            }
        }
        else if (controlMode == ControlMode.TELEPORT_INSTANT)
        {
            // Movement input:
            Vector3 position;
            if (TeleportPosition(out position))
            {
                transform.position = position;
            }
        }
        else if (controlMode == ControlMode.TELEPORT_INSTANT)
        {
            // Movement input:
            Vector3 position;
            if (TeleportPositionPreview(out position))
            {
                transform.position = position;
            }
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

    // Returns the Vector3 with the position for the TP
    private bool TeleportPosition(out Vector3 newPos)
    {
        bool moved = Input.GetMouseButtonDown(1);
        if (moved) {
            newPos = RaycastPosition();
        }
        else newPos = new Vector3();
        return moved;
    }
    // Returns the Vector3 with the position for the TP
    private bool TeleportPositionPreview(out Vector3 newPos)
    {
        bool moved = Input.GetMouseButtonUp(1);

        if (moved)
        {
            newPos = RaycastPosition();
        }
        else
        {
            return false;
        }
        return moved;
    }

    private Vector3 RaycastPosition()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, maxTPRange))
        {
            return new Vector3(hit.point.x, transform.position.y, hit.point.z);
        }
        else
        {
            Vector3 fwd = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            return transform.position + fwd * maxTPRange;
        }
    }

    // Triggered by the physics events:
    void OnCollisionEnter(Collision obj)
    {
        if (obj.gameObject.tag == "Spheres" && cooldown <= 0.0f)
        {
            cooldown = collision_cd;
            item = obj.gameObject;
            item.transform.position = Camera.main.transform.position
                                      + Camera.main.transform.forward * 0.75f;
            isHolding = true;
            item.GetComponent<Rigidbody>().useGravity = false;
            item.GetComponent<Rigidbody>().detectCollisions = true;
        }
    }

    private void UpdateBall()
    {
        // Catch the ball
        if (item != null)
        {
            distance = Vector3.Distance(item.transform.position,
                                        transform.position);
            if (distance >= 1.5f)
            {
                isHolding = false;
            }
            if (isHolding == true)
            {
                Rigidbody body = item.GetComponent<Rigidbody>();
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                // The CameraParent becomes the parent of the ball too.
                item.transform.SetParent(transform);
                if (Input.GetKey("z"))
                {
                    currentHeldDownTime = Mathf.Min(currentHeldDownTime + Time.deltaTime, maxForceTime);
                }
                else if (Input.GetKeyUp("z")) {
                    float throwForce = minThrowForce + (maxThrowForce - minThrowForce) * currentHeldDownTime / maxForceTime;
                    print("Current throw force: " +  throwForce);
                    // Throw
                    var cam = Camera.main;
                    item.GetComponent<Rigidbody>().AddForce(
                    cam.transform.forward * throwForce);
                    isHolding = false;
                    currentHeldDownTime = 0.0f;
                }
            }
            else
            {
                objectPos = item.transform.position;
                item.transform.SetParent(null);
                item.GetComponent<Rigidbody>().useGravity = true;
                item.transform.position = objectPos;
            }
        }
    }
}
