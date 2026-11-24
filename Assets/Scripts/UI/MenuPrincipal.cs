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
    [SerializeField] private GameObject painelDificuldade;
    [SerializeField] private GameObject painelRanking;
    [SerializeField] private TMP_Text textoRanking;

    [Header("Dificuldades (assets da pasta Difficulties)")]
    [SerializeField] private DifficultyDefinition dificuldadeFacil;
    [SerializeField] private DifficultyDefinition dificuldadeDificil;

    [Header("Efeito do título (opcional)")]
    [SerializeField] private TMP_Text titulo;
    private Color corBase;
    [SerializeField] private bool tremerTitulo = true;

    private enum Painel
    {
        Principal,
        Sobre,
        Dificuldade,
        Ranking,
    }

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

    // Botão JOGAR: abre a tela de escolha de dificuldade
    public void MostrarDificuldade()
    {
        Mostrar(Painel.Dificuldade);
    }

    // Botões FÁCIL e DIFÍCIL
    public void IniciarFacil()
    {
        Iniciar(dificuldadeFacil);
    }

    public void IniciarDificil()
    {
        Iniciar(dificuldadeDificil);
    }

    public void MostrarRanking()
    {
        // O texto é montado só aqui, para sempre mostrar os dados mais recentes
        textoRanking.text =
            $"{dificuldadeFacil.displayName.ToUpper()}\n{Ranking.Texto(dificuldadeFacil)}\n" +
            $"{dificuldadeDificil.displayName.ToUpper()}\n{Ranking.Texto(dificuldadeDificil)}";
        Mostrar(Painel.Ranking);
    }

    public void MostrarSobre()
    {
        Mostrar(Painel.Sobre);
    }

    public void MostrarPrincipal()
    {
        Mostrar(Painel.Principal);
    }

    public void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Internos ----------

    private void Iniciar(DifficultyDefinition dificuldade)
    {
        if (dificuldade == null)
        {
            Debug.LogError("MenuPrincipal: dificuldade não configurada no Inspector.", this);
            return;
        }

        // O GameManager lê isso ao iniciar a cena do jogo
        GameManager.SelectedDifficulty = dificuldade;
        SceneManager.LoadScene(cenaDoJogo);
    }

    // Só um painel fica visível por vez
    private void Mostrar(Painel painel)
    {
        if (painelPrincipal != null)
        {
            painelPrincipal.SetActive(painel == Painel.Principal);
        }
        if (painelSobre != null)
        {
            painelSobre.SetActive(painel == Painel.Sobre);
        }
        if (painelDificuldade != null)
        {
            painelDificuldade.SetActive(painel == Painel.Dificuldade);
        }
        if (painelRanking != null)
        {
            painelRanking.SetActive(painel == Painel.Ranking);
        }
    }
}