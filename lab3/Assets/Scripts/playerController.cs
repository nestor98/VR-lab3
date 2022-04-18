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
    public bool desktopMode = true;


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
    public GameObject playGround;
    private Rect playableRect;
    public float maxTPRange;
    public GameObject teleportCircle;



    // Start is called before the first frame update
    void Start()
    {
        //throwForce = minThrowForce;
        cam = Camera.main;

        var bounds = playGround.GetComponent<BoxCollider>().bounds;
        playableRect = new Rect(bounds.min.x, bounds.min.z, bounds.size.x, bounds.size.z);
    }

    // Update is called once per frame
    void Update()
    {
        // Movement and looking inputs
        ApplyInputs();
        //if (controlMode == TELEPORT_PREVIEW) ApplyInputsNormal();

        if (!desktopMode && Input.anyKeyDown) Debug.Log(Input.inputString);

        // Handle ball interaction:
        UpdateBall();

        // tick down the collision cooldown:
        if (cooldown > 0.0f) cooldown -= Time.deltaTime;

        if (Input.GetKeyDown("space")) // Toggle cursor
            Cursor.visible = !Cursor.visible;
        
    }

    private void ApplyInputs()
    {
        Vector3 euler = transform.eulerAngles;
        if (desktopMode)
        {
            // Rotation input:
            float y = Input.GetAxis("Mouse X"),
                  x = -Input.GetAxis("Mouse Y");

            euler = new Vector3(x, y, 0) * rotSpeed * Time.deltaTime;
            transform.eulerAngles += euler;

            euler = transform.eulerAngles;
        }

        if (controlMode == ControlMode.NORMAL) {
            // Movement input:
            Vector3 movement = MovementInputs();
            if (movement.sqrMagnitude > 1e-6) {
                transform.eulerAngles = new Vector3(0, cam.transform.eulerAngles.y, 0); // Only move in xz plane
                transform.Translate(movement * moveSpeed * Time.deltaTime);
            }
        } else if (controlMode == ControlMode.TELEPORT_INSTANT) {
            if (Input.GetMouseButtonDown(1)) {
                transform.position = RaycastPosition();
            }
        } else if (controlMode == ControlMode.TELEPORT_PREVIEW) {
            bool rightClickPressed = Input.GetMouseButton(1);
            teleportCircle.SetActive(rightClickPressed);
            if (rightClickPressed) {
                Vector3 destination = RaycastPosition();
                // Keep y coordinate of the circle:
                teleportCircle.transform.position = new Vector3(destination.x, teleportCircle.transform.position.y, destination.z);
            } else if (Input.GetMouseButtonUp(1)) {
                // Only teleport when the user releases right click
                transform.position = RaycastPosition();
            }
        }
        
        // Restore latitude
        transform.eulerAngles = euler;
    }
    
    // Returns the Vector3 with the movement inputs
    private Vector3 MovementInputs()
    {
        float x = Input.GetAxis("Horizontal"), z = Input.GetAxis("Vertical");
        if (!desktopMode) (x, z) = (z, -x); // Swap because of weird controller
        return new Vector3(x, 0, z);
    }

    // Constrains the input position to the playground rectangle
    private Vector3 ConstrainToPlayground(Vector3 input)
    {
        return new Vector3(Mathf.Clamp(input.x, playableRect.x, playableRect.x + playableRect.width),
                           input.y,
                           Mathf.Clamp(input.z, playableRect.y, playableRect.y + playableRect.height));
    }

    private Vector3 RaycastPosition()
    {
        var hits = Physics.RaycastAll(transform.position, transform.forward, maxTPRange);
        foreach (RaycastHit hit in hits) {
            // Check if the hit is of the appropriate tag:
            if (hit.transform.gameObject.tag == playableTag)
            {
                return new Vector3(hit.point.x, transform.position.y, hit.point.z);
            }
        }
        Vector3 fwd = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        return ConstrainToPlayground(transform.position + fwd * maxTPRange);
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

    private bool GetInputKey()
    {
        if (desktopMode) return Input.GetKey("z");
        else return Input.anyKey;
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
            else if (isHolding)
            {
                Rigidbody body = item.GetComponent<Rigidbody>();
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                // The CameraParent becomes the parent of the ball too.
                item.transform.SetParent(transform);
                if (desktopMode)
                {
                    if (GetInputKey())
                    {
                        currentHeldDownTime = Mathf.Min(currentHeldDownTime + Time.deltaTime, maxForceTime);
                    }
                    else if (Input.GetKeyUp("z"))
                    {
                        float throwForce = minThrowForce + (maxThrowForce - minThrowForce) * currentHeldDownTime / maxForceTime;
                        print("Current throw force: " + throwForce);
                        // Throw
                        var cam = Camera.main;
                        item.GetComponent<Rigidbody>().AddForce(
                        cam.transform.forward * throwForce);
                        isHolding = false;
                        currentHeldDownTime = 0.0f;
                    }
                }
                else
                { // dont use keyup with phone
                    if (Input.anyKey)
                    {
                        float throwForce = minThrowForce + (maxThrowForce - minThrowForce) / 2.0f;
                        print("Current throw force: " + throwForce);
                        // Throw
                        var cam = Camera.main;
                        item.GetComponent<Rigidbody>().AddForce(
                        cam.transform.forward * throwForce);
                        isHolding = false;
                        currentHeldDownTime = 0.0f;
                    }
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
