using UnityEngine;
using UnityEngine.InputSystem;

public class CameraZoom : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField] private float zoomStep = 2f;        // quanto a altura muda por clique do scroll
    [SerializeField] private float smoothing = 5f;       // menor = mais suave e lento

    [Header("Limites (a partir da altura inicial da câmera)")]
    [SerializeField] private float maxZoomIn = 15f;      // quanto pode descer
    [SerializeField] private float maxZoomOut = 8f;      // quanto pode subir

    [Header("Avanço máximo por clique (evita deslizar demais para o lado)")]
    [SerializeField] private float maxRayStretch = 2.5f;

    private Camera cam;
    private Vector3 targetPosition;
    private float minY;
    private float maxY;

    private void Start()
    {
        cam = GetComponent<Camera>();
        targetPosition = transform.position;
        minY = transform.position.y - maxZoomIn;
        maxY = transform.position.y + maxZoomOut;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        float scrollY = Mouse.current.scroll.ReadValue().y;
        float direction = Mathf.Abs(scrollY) > 0.01f ? Mathf.Sign(scrollY) : 0f;

        if (direction != 0f)
        {
            Vector3 rayDir = cam.ScreenPointToRay(Mouse.current.position.ReadValue()).direction;

            // Só funciona se o raio aponta para baixo (sempre verdade com a câmera inclinada)
            if (rayDir.y < -0.05f)
            {
                // Altura desejada: scroll para frente desce (aproxima), para trás sobe
                float newY = Mathf.Clamp(targetPosition.y - direction * zoomStep, minY, maxY);
                float dy = newY - targetPosition.y;

                if (Mathf.Abs(dy) > 0.0001f)
                {
                    // Distância ao longo do raio que produz essa mudança de altura
                    float dist = dy / rayDir.y;
                    float maxDist = zoomStep * maxRayStretch;
                    dist = Mathf.Clamp(dist, -maxDist, maxDist);

                    targetPosition += rayDir * dist;
                }
            }
        }

        float lerpFactor = Mathf.Clamp01(smoothing * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, lerpFactor);
    }
}