using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

// Menu de pause: ESC pausa/continua. A interface é o prefab "PauseMenu" (Assets/Resources/PauseMenu.prefab),
// montado no editor. Este script só tem a lógica e os métodos chamados pelos botões (OnClick).
//
// O prefab é criado automaticamente em toda cena de jogo (qualquer uma que não seja a do menu). Assim não
// mexemos no arquivo da cena AbandonedFarm, que os colegas também editam, e evitamos conflito no Git.
// Por isso, não coloque o prefab manualmente em nenhuma cena.
public class PauseMenu : MonoBehaviour
{
    private const string CenaDoMenu = "StartMenu";
    private const string CaminhoDoPrefab = "PauseMenu";   // Assets/Resources/PauseMenu.prefab

    // Outros scripts (câmera, construção de torres) podem consultar: if (PauseMenu.Pausado) return;
    public static bool Pausado { get; private set; }

    [Header("Painel que aparece quando o jogo está pausado")]
    [SerializeField] private GameObject painel;

    // ---------- Instalação automática ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Instalar()
    {
        // Roda uma vez, na primeira cena. Para as próximas, escutamos o evento de carregamento.
        // O "-=" antes do "+=" evita inscrição duplicada quando o domain reload está desligado.
        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;

        CriarSeNecessario(SceneManager.GetActiveScene().name);
    }

    private static void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        if (modo == LoadSceneMode.Single)
        {
            CriarSeNecessario(cena.name);
        }
    }

    private static void CriarSeNecessario(string nomeDaCena)
    {
        if (nomeDaCena == CenaDoMenu)
        {
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(CaminhoDoPrefab);
        if (prefab == null)
        {
            Debug.LogWarning("PauseMenu: prefab não encontrado em Assets/Resources/PauseMenu.prefab");
            return;
        }

        GarantirEventSystem();
        Instantiate(prefab);
    }

    // Sem um EventSystem os botões não recebem clique. O projeto usa só o Input System novo,
    // então o módulo de entrada precisa ser o InputSystemUIInputModule.
    private static void GarantirEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    // ---------- Ciclo de vida ----------

    private void Awake()
    {
        Pausado = false;
        painel.SetActive(false);
    }

    // Update continua rodando mesmo com o tempo parado, por isso o ESC funciona para despausar
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Pausado)
            {
                Continuar();
            }
            else
            {
                Pausar();
            }
        }
    }

    // Garante que nenhuma cena nova comece congelada
    private void OnDestroy()
    {
        if (Pausado)
        {
            Retomar();
        }
    }

    // ---------- Ações (os botões chamam os métodos públicos pelo OnClick) ----------

    private void Pausar()
    {
        Pausado = true;
        Time.timeScale = 0f;          // inimigos, torres e projéteis usam Time.deltaTime, então param
        AudioListener.pause = true;   // pausa o áudio (para quando houver sons)
        painel.SetActive(true);
    }

    public void Continuar()
    {
        Retomar();
    }

    public void Reiniciar()
    {
        Retomar();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void VoltarAoMenu()
    {
        Retomar();
        SceneManager.LoadScene(CenaDoMenu);
    }

    private void Retomar()
    {
        Pausado = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (painel != null)
        {
            painel.SetActive(false);
        }
    }
}
