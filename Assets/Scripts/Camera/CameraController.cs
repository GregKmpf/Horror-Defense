using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Mapa (limite circular)")]
    [SerializeField] private Transform centroDoMapa;   // o Y dele = altura do chão (~30)
    [SerializeField] private float raioDoMapa = 80f;

    [Header("Auto-ajuste ao caminho (opcional)")]
    [SerializeField] private Transform caminho;        // objeto "Caminho" com os waypoints
    [SerializeField] private float margem = 15f;       // folga além do waypoint mais distante

    [Header("Inclinação")]
    [Range(45f, 90f)]
    [SerializeField] private float anguloX = 70f;      // 90 = totalmente de cima

    [Header("Zoom (altura Y no mundo)")]
    [SerializeField] private float alturaMinima = 40f; // zoom máximo (mais perto)
    [SerializeField] private float alturaMaxima = 80f; // zoom mínimo (mais longe)
    [SerializeField] private float passoDoZoom = 10f;
    [Range(0f, 1f)]
    [SerializeField] private float retornoAoCentro = 0.25f; // ao afastar, volta ao centro

    [Header("Suavização")]
    [SerializeField] private float suavizacao = 10f;

    private Camera cam;
    private Vector3 posicaoAlvo;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        transform.rotation = Quaternion.Euler(anguloX, 0f, 0f);

        AjustarAoCaminho();

        // Começa na visão geral, centralizada
        Vector3 pos = transform.position;
        if (centroDoMapa != null)
        {
            pos.x = centroDoMapa.position.x;
            pos.z = centroDoMapa.position.z - AvancoHorizontal(alturaMaxima);
        }
        pos.y = alturaMaxima;
        transform.position = pos;
        posicaoAlvo = pos;
    }

    private void Update()
    {
        Zoom();
        LimitarAoMapa();
        Aplicar();
    }

    // Quanto a câmera fica recuada em relação ao ponto do chão que enxerga
    private float AvancoHorizontal(float alturaDaCamera)
    {
        float chao = centroDoMapa != null ? centroDoMapa.position.y : 0f;
        return (alturaDaCamera - chao) / Mathf.Tan(anguloX * Mathf.Deg2Rad);
    }

    private void Zoom()
    {
        float scroll = LerScroll();
        if (scroll == 0f) return;

        // Raio da câmera passando pelo cursor do mouse
        Ray raio = cam.ScreenPointToRay(PosicaoDoMouse());
        Vector3 deslocamento = raio.direction * (scroll * passoDoZoom);

        // Respeita altura mínima e máxima, mantendo a direção do zoom
        if (!Mathf.Approximately(deslocamento.y, 0f))
        {
            float novoY = Mathf.Clamp(posicaoAlvo.y + deslocamento.y, alturaMinima, alturaMaxima);
            float fator = (novoY - posicaoAlvo.y) / deslocamento.y;
            deslocamento *= fator;
        }

        posicaoAlvo += deslocamento;

        // Ao afastar, volta aos poucos para o centro do mapa
        if (scroll < 0f && centroDoMapa != null && retornoAoCentro > 0f)
        {
            Vector3 c = centroDoMapa.position;
            float zCentrado = c.z - AvancoHorizontal(posicaoAlvo.y);
            posicaoAlvo.x = Mathf.Lerp(posicaoAlvo.x, c.x, retornoAoCentro);
            posicaoAlvo.z = Mathf.Lerp(posicaoAlvo.z, zCentrado, retornoAoCentro);
        }
    }

    // Limita o ponto do chão que a câmera está enxergando
    private void LimitarAoMapa()
    {
        if (centroDoMapa == null) return;

        Vector3 c = centroDoMapa.position;
        float avanco = AvancoHorizontal(posicaoAlvo.y);

        Vector2 olhado = new Vector2(posicaoAlvo.x - c.x, posicaoAlvo.z + avanco - c.z);
        if (olhado.magnitude > raioDoMapa)
        {
            olhado = olhado.normalized * raioDoMapa;
            posicaoAlvo.x = c.x + olhado.x;
            posicaoAlvo.z = c.z + olhado.y - avanco;
        }
    }

    private void Aplicar()
    {
        float t = 1f - Mathf.Exp(-suavizacao * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, posicaoAlvo, t);
    }

    // Centro e raio calculados a partir dos waypoints (filhos diretos de "Caminho")
    private void AjustarAoCaminho()
    {
        if (caminho == null || centroDoMapa == null || caminho.childCount == 0) return;

        Vector3 soma = Vector3.zero;
        foreach (Transform p in caminho) soma += p.position;
        Vector3 media = soma / caminho.childCount;

        centroDoMapa.position = new Vector3(media.x, centroDoMapa.position.y, media.z);

        float maior = 0f;
        foreach (Transform p in caminho)
        {
            Vector2 d = new Vector2(p.position.x - media.x, p.position.z - media.z);
            maior = Mathf.Max(maior, d.magnitude);
        }
        raioDoMapa = maior + margem;
    }

    // ---- Entrada: funciona com o Input System novo, o antigo ou os dois ----

    private float LerScroll()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return 0f;
        float y = Mouse.current.scroll.ReadValue().y;
#else
        float y = Input.mouseScrollDelta.y;
#endif
        // Só o sentido (-1, 0, 1), para o zoom ser igual no Windows e no Linux
        return Mathf.Approximately(y, 0f) ? 0f : Mathf.Sign(y);
    }

    private Vector3 PosicaoDoMouse()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
        return Input.mousePosition;
#endif
    }

    private void OnDrawGizmosSelected()
    {
        if (centroDoMapa == null) return;
        Gizmos.color = Color.red;
        Vector3 c = centroDoMapa.position;
        Vector3 anterior = c + new Vector3(raioDoMapa, 0f, 0f);
        for (int i = 1; i <= 64; i++)
        {
            float a = i * Mathf.PI * 2f / 64f;
            Vector3 atual = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * raioDoMapa;
            Gizmos.DrawLine(anterior, atual);
            anterior = atual;
        }
    }
}