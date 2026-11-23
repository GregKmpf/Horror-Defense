using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

// Menu de pause: ESC pausa/continua. A interface é o prefab "PauseMenu" (Assets/Resources/PauseMenu.prefab),
// montado no editor. Este script só tem a lógica e os métodos chamados pelos botões (OnClick).

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
        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;

        CriarSeNecessario(SceneManager.GetActiveScene());
    }

    private static void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        if (modo == LoadSceneMode.Single)
        {
            CriarSeNecessario(cena);
        }
    }

    private static void CriarSeNecessario(Scene cena)
    {
        if (cena.name == CenaDoMenu)
        {
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(CaminhoDoPrefab);
        if (prefab == null)
        {
            Debug.LogWarning("PauseMenu: prefab não encontrado em Assets/Resources/PauseMenu.prefab");
            return;
        }

        GarantirEventSystem(cena);

        // Quando este método roda, a cena ativa ainda pode ser a anterior. Por isso movemos o objeto
        // explicitamente para a cena que acabou de carregar; senão ele seria destruído junto com a antiga.
        GameObject instancia = Instantiate(prefab);
        SceneManager.MoveGameObjectToScene(instancia, cena);
    }

    private static void GarantirEventSystem(Scene cena)
    {
        foreach (GameObject raiz in cena.GetRootGameObjects())
        {
            if (raiz.GetComponentInChildren<EventSystem>(true) != null)
            {
                return;
            }
        }

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        SceneManager.MoveGameObjectToScene(go, cena);
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