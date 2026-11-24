using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Moves the camera around the map while playing. The camera orbits a point on the ground, the focus, and always
// looks at it:
//   WASD or arrows       move across the map, faster while holding Shift
//   Middle mouse drag    drag the map
//   Right mouse drag     turn around the focus and tilt. A right click without dragging still closes the build
//                        menu (SlotPicker)
//   Q / E                turn around the focus
//   Scroll               zoom towards the cursor
//   Home                 back to the starting view
// The starting view is wherever the camera is placed in the scene. The focus follows the height of the ground and
// stays inside a circle (Bounds Center and Bounds Radius), so the camera doesn't leave the map.
// Keys and the mouse are ignored while the game is paused or over, and the mouse is ignored over the UI. It runs on
// unscaled time, so the game speed doesn't change how fast the camera moves.
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Move")]
    [Tooltip("Keyboard speed, in camera distances per second, so it feels the same at any zoom")]
    public float moveSpeed = 0.9f;
    [Tooltip("Speed multiplier while holding Shift")]
    public float fastMultiplier = 2.5f;

    [Header("Turn")]
    [Tooltip("Degrees per second with Q and E")]
    public float keyTurnSpeed = 90f;
    [Tooltip("Degrees per pixel while dragging with the right mouse button")]
    public float dragTurnSpeed = 0.25f;
    [Tooltip("How far the camera tilts, in degrees below the horizon")]
    public float minPitch = 25f;
    public float maxPitch = 85f;

    [Header("Zoom")]
    [Tooltip("How much each step of the scroll wheel changes the distance to the focus, 0.15 = 15%")]
    public float zoomPerStep = 0.15f;
    public float minDistance = 15f;
    public float maxDistance = 80f;

    [Header("Limits")]
    [Tooltip("The focus stays within Bounds Radius of this point, measured on the ground. Only X and Z matter")]
    public Vector3 boundsCenter;
    [Tooltip("0 for no limit")]
    public float boundsRadius;
    [Tooltip("What the focus follows and the camera stays above")]
    public LayerMask groundLayer;
    [Tooltip("The camera never gets closer than this to the ground")]
    public float minHeightAboveGround = 4f;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera catches up with the controls, higher is snappier")]
    public float sharpness = 10f;
    [Tooltip("How quickly the focus follows the height of the ground, lower keeps the camera steadier over bumps")]
    public float heightSharpness = 3f;

    const float RayHeight = 500f;
    const float MaxDeltaTime = 0.1f;

    Camera cam;

    // Where the controls want the camera, and where it is on its way there
    Vector3 targetFocus, focus;
    float targetYaw, yaw;
    float targetPitch, pitch;
    float targetDistance, distance;

    Vector3 startFocus;
    float startYaw, startPitch, startDistance;

    bool dragging;
    bool turning;
    Vector2 lastMousePosition;

    static bool InputBlocked =>
        PauseMenu.Pausado || (GameManager.Instance != null && !GameManager.Instance.IsPlaying);

    void Reset()
    {
        groundLayer = LayerMask.GetMask("Ground");
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }

        // Take the starting view from how the camera is placed: the focus is where it looks at the ground
        Vector3 forward = transform.forward;
        startYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        startPitch = Mathf.Clamp(Mathf.Asin(-forward.y) * Mathf.Rad2Deg, minPitch, maxPitch);
        if (Physics.Raycast(transform.position, forward, out RaycastHit hit, 1000f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            startFocus = hit.point;
            startDistance = hit.distance;
        }
        else
        {
            startDistance = (minDistance + maxDistance) / 2f;
            startFocus = transform.position + forward * startDistance;
        }
        startDistance = Mathf.Clamp(startDistance, minDistance, maxDistance);
        startFocus = ClampToBounds(startFocus);

        targetFocus = focus = startFocus;
        targetYaw = yaw = startYaw;
        targetPitch = pitch = startPitch;
        targetDistance = distance = startDistance;
        Place();
    }

    void LateUpdate()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, MaxDeltaTime);

        if (InputBlocked)
        {
            dragging = false;
            turning = false;
        }
        else
        {
            HandleKeyboard(dt);
            HandleMouse();
        }

        if (TryGetGroundHeight(targetFocus, out float ground))
        {
            targetFocus.y = ground;
        }

        float follow = 1f - Mathf.Exp(-sharpness * dt);
        float followHeight = 1f - Mathf.Exp(-heightSharpness * dt);
        focus.x = Mathf.Lerp(focus.x, targetFocus.x, follow);
        focus.z = Mathf.Lerp(focus.z, targetFocus.z, follow);
        focus.y = Mathf.Lerp(focus.y, targetFocus.y, followHeight);
        yaw = Mathf.Lerp(yaw, targetYaw, follow);
        pitch = Mathf.Lerp(pitch, targetPitch, follow);
        distance = Mathf.Lerp(distance, targetDistance, follow);
        Place();
    }

    // Goes back to the starting view
    public void ResetView()
    {
        targetFocus = startFocus;
        // Turn back the short way, however many times the player went around
        targetYaw = startYaw + Mathf.Round((targetYaw - startYaw) / 360f) * 360f;
        targetPitch = startPitch;
        targetDistance = startDistance;
    }

    void HandleKeyboard(float dt)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.homeKey.wasPressedThisFrame)
        {
            ResetView();
        }

        Vector3 move = Vector3.zero;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.z += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.z -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
        if (move != Vector3.zero)
        {
            float speed = moveSpeed * targetDistance * (keyboard.shiftKey.isPressed ? fastMultiplier : 1f);
            // Along the ground, relative to where the camera faces
            targetFocus = ClampToBounds(targetFocus + Quaternion.Euler(0f, targetYaw, 0f) * move.normalized * speed * dt);
        }

        float turn = 0f;
        if (keyboard.qKey.isPressed) turn -= 1f;
        if (keyboard.eKey.isPressed) turn += 1f;
        targetYaw += turn * keyTurnSpeed * dt;
    }

    void HandleMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 position = mouse.position.ReadValue();
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // Drags start off the UI but carry on over it
        if (mouse.middleButton.wasPressedThisFrame && !overUI) dragging = true;
        if (!mouse.middleButton.isPressed) dragging = false;
        if (mouse.rightButton.wasPressedThisFrame && !overUI) turning = true;
        if (!mouse.rightButton.isPressed) turning = false;

        if (dragging)
        {
            Drag(lastMousePosition, position);
        }
        if (turning)
        {
            Vector2 delta = position - lastMousePosition;
            targetYaw += delta.x * dragTurnSpeed;
            targetPitch = Mathf.Clamp(targetPitch - delta.y * dragTurnSpeed, minPitch, maxPitch);
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (!overUI && Mathf.Abs(scroll) > 0.01f)
        {
            Zoom(Mathf.Sign(scroll), position);
        }

        lastMousePosition = position;
    }

    // Moves the map so the ground under the cursor stays under it. It moves the camera straight away instead of
    // easing in, or the map would slide behind the cursor
    void Drag(Vector2 from, Vector2 to)
    {
        Plane ground = new Plane(Vector3.up, focus);
        if (!RayOnPlane(cam.ScreenPointToRay(from), ground, out Vector3 a) || !RayOnPlane(cam.ScreenPointToRay(to), ground, out Vector3 b))
        {
            return;
        }

        // Near the horizon a tiny drag is a long way on the ground
        Vector3 shift = Vector3.ClampMagnitude(a - b, targetDistance);
        Vector3 moved = ClampToBounds(targetFocus + shift);
        shift = moved - targetFocus;
        targetFocus = moved;
        focus.x += shift.x;
        focus.z += shift.z;
    }

    // Scales the view around the ground under the cursor, which keeps that point under the cursor
    void Zoom(float direction, Vector2 screenPosition)
    {
        float newDistance = Mathf.Clamp(targetDistance * (1f - direction * zoomPerStep), minDistance, maxDistance);
        if (Mathf.Approximately(newDistance, targetDistance))
        {
            return;
        }

        // Where the cursor will point once the camera gets where it's going
        Quaternion rotation = Quaternion.Euler(targetPitch, targetYaw, 0f);
        Vector3 local = Quaternion.Inverse(transform.rotation) * cam.ScreenPointToRay(screenPosition).direction;
        Ray ray = new Ray(targetFocus - rotation * Vector3.forward * targetDistance, rotation * local);

        Vector3 point;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
        }
        else if (!RayOnPlane(ray, new Plane(Vector3.up, targetFocus), out point))
        {
            point = targetFocus;
        }

        Vector3 shift = (point - targetFocus) * (1f - newDistance / targetDistance);
        shift.y = 0f;
        // Pointing near the horizon would throw the camera far across the map
        targetFocus = ClampToBounds(targetFocus + Vector3.ClampMagnitude(shift, targetDistance * zoomPerStep * 2f));
        targetDistance = newDistance;
    }

    void Place()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 position = focus - rotation * Vector3.forward * distance;

        if (TryGetGroundHeight(position, out float ground) && position.y < ground + minHeightAboveGround)
        {
            position.y = ground + minHeightAboveGround;
        }

        transform.SetPositionAndRotation(position, rotation);
    }

    Vector3 ClampToBounds(Vector3 point)
    {
        if (boundsRadius <= 0f)
        {
            return point;
        }

        Vector3 offset = point - boundsCenter;
        offset.y = 0f;
        if (offset.sqrMagnitude <= boundsRadius * boundsRadius)
        {
            return point;
        }

        Vector3 clamped = boundsCenter + offset.normalized * boundsRadius;
        clamped.y = point.y;
        return clamped;
    }

    bool TryGetGroundHeight(Vector3 position, out float height)
    {
        if (Physics.Raycast(new Vector3(position.x, RayHeight, position.z), Vector3.down, out RaycastHit hit, RayHeight * 2f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            height = hit.point.y;
            return true;
        }

        height = position.y;
        return false;
    }

    static bool RayOnPlane(Ray ray, Plane plane, out Vector3 point)
    {
        if (plane.Raycast(ray, out float enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }

        point = default;
        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (boundsRadius <= 0f)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Vector3 previous = boundsCenter + Vector3.forward * boundsRadius;
        for (int i = 1; i <= 64; i++)
        {
            float angle = i * Mathf.PI * 2f / 64f;
            Vector3 next = boundsCenter + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * boundsRadius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
