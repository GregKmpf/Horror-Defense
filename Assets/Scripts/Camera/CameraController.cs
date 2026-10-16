using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Referência")]
    [SerializeField] private Transform cam;          // Main Camera (filha do rig)

    [Header("Movimento (WASD / setas)")]
    [SerializeField] private float panSpeed = 15f;
    [SerializeField] private float fastMultiplier = 2f;   // segurando Shift

    [Header("Rotação (Q / E)")]
    [SerializeField] private float rotateSpeed = 90f;     // graus por segundo

    [Header("Inclinação e zoom (roda do mouse)")]
    [SerializeField] private float pitch = 50f;           // ângulo de cima para baixo
    [SerializeField] private float startZoom = 15f;
    [SerializeField] private float zoomStep = 2f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 30f;
    [SerializeField] private float zoomSmooth = 10f;

    [Header("Limites do mapa (X, Z)")]
    [SerializeField] private Vector2 minXZ = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 maxXZ = new Vector2(50f, 50f);

    private float targetZoom;
    private float currentZoom;

    void Start()
    {
        if (cam == null)
        {
            Debug.LogError("CameraController: arraste a Main Camera para o campo Cam.");
            enabled = false;
            return;
        }

        targetZoom = currentZoom = Mathf.Clamp(startZoom, minZoom, maxZoom);
        ApplyCameraTransform();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (kb != null)
        {
            Move(kb);
            Rotate(kb);
        }
        if (mouse != null) Zoom(mouse);

        ApplyCameraTransform();
    }

    void Move(Keyboard kb)
    {
        float h = 0f, v = 0f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1f;

        float speed = panSpeed * (kb.leftShiftKey.isPressed ? fastMultiplier : 1f);
        Vector3 dir = transform.right * h + transform.forward * v;
        transform.position += dir.normalized * speed * Time.deltaTime;

        // Mantém o rig dentro dos limites do mapa
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minXZ.x, maxXZ.x);
        p.z = Mathf.Clamp(p.z, minXZ.y, maxXZ.y);
        transform.position = p;
    }

    void Rotate(Keyboard kb)
    {
        float r = 0f;
        if (kb.eKey.isPressed) r += 1f;
        if (kb.qKey.isPressed) r -= 1f;
        transform.Rotate(Vector3.up, r * rotateSpeed * Time.deltaTime, Space.World);
    }

    void Zoom(Mouse mouse)
    {
        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0f)
            targetZoom = Mathf.Clamp(targetZoom - Mathf.Sign(scroll) * zoomStep, minZoom, maxZoom);

        currentZoom = Mathf.Lerp(currentZoom, targetZoom, zoomSmooth * Time.deltaTime);
    }

    // A câmera fica "atrás" do rig, inclinada, sempre olhando para o ponto do rig
    void ApplyCameraTransform()
    {
        cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        cam.localPosition = -(cam.localRotation * Vector3.forward) * currentZoom;
    }
}