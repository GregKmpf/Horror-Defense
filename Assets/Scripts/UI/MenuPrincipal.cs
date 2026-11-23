using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Menu principal. A interface (Canvas, painéis e botões) é montada na cena, pelo editor.
public class MenuPrincipal : MonoBehaviour
{
    [Header("Cena do jogo (precisa estar no Build Settings)")]
    [SerializeField] private string cenaDoJogo = "AbandonedFarm";

    [Header("Painéis")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelSobre;

    [Header("Efeito do título (opcional)")]
    [SerializeField] private TMP_Text titulo;
    private Color corBase;
    [SerializeField] private bool tremerTitulo = true;

    private void Awake()
    {
        // Se o jogador voltou do pause, o tempo pode ter ficado parado
        if (titulo != null)
        {
            corBase = titulo.color;
        }
        Time.timeScale = 1f;
        AudioListener.pause = false;

        MostrarPrincipal();
    }

    // Faz o título "falhar" de leve, como uma luz ruim
    private void Update()
    {
        if (!tremerTitulo || titulo == null)
        {
            return;
        }

        float ruido = Mathf.PerlinNoise(Time.time * 6f, 0f);
        Color cor = corBase;
        cor.a = Mathf.Lerp(0.6f, 1f, ruido);
        titulo.color = cor;
    }

    // ---------- Métodos chamados pelos botões (OnClick) ----------

    public void Jogar()
    {
        SceneManager.LoadScene(cenaDoJogo);
    }

    public void MostrarSobre()
    {
        AlternarPaineis(false);
    }

    public void MostrarPrincipal()
    {
        AlternarPaineis(true);
    }

    public void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Só um dos dois painéis fica visível por vez
    private void AlternarPaineis(bool principal)
    {
        if (painelPrincipal != null)
        {
            painelPrincipal.SetActive(principal);
        }
        if (painelSobre != null)
        {
            painelSobre.SetActive(!principal);
        }
    }
}
