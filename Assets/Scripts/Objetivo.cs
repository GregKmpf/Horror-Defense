using UnityEngine;
using UnityEngine.Events;

// A base que os inimigos tentam alcançar. Um inimigo chega quando o collider dele entra no BoxCollider da base,
// um trigger nas portas do celeiro (OnTriggerEnter): ele some e a base perde o Base Damage do EnemyDefinition dele.
// A base fica na camada Base, que só colide com a camada Enemy. O inimigo tem um Rigidbody cinemático, então a
// base não precisa de um.
[RequireComponent(typeof(BoxCollider))]
public class Objetivo : MonoBehaviour
{
    [Header("Vida da base")]
    public int vidaMaxima = 100;
    public int vidaAtual;

    [Header("Eventos")]
    public UnityEvent aoReceberDano;
    public UnityEvent aoSerDestruido;

    public static Objetivo Instancia { get; private set; }

    void Awake()
    {
        Instancia = this;
        vidaAtual = vidaMaxima;
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnEnable()
    {
        Enemy.AnyReachedBase += AoInimigoChegar;
    }

    void OnDisable()
    {
        Enemy.AnyReachedBase -= AoInimigoChegar;
    }

    void OnTriggerEnter(Collider outro)
    {
        Enemy inimigo = outro.GetComponentInParent<Enemy>();
        if (inimigo != null && inimigo.IsAlive)
        {
            inimigo.ReachBase();
        }
    }

    void AoInimigoChegar(Enemy inimigo)
    {
        ReceberDano(inimigo.definition.baseDamage);
    }

    public void ReceberDano(int dano)
    {
        if (vidaAtual <= 0) return;

        vidaAtual -= dano;
        aoReceberDano?.Invoke();
        Debug.Log($"Base atacada! Vida: {vidaAtual}/{vidaMaxima}");

        if (vidaAtual <= 0)
        {
            vidaAtual = 0;
            aoSerDestruido?.Invoke();
            Debug.Log("GAME OVER: a base foi destruída!");
        }
    }

    // Desenha o trigger da base em vermelho na Scene View, onde os inimigos chegam
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        BoxCollider caixa = GetComponent<BoxCollider>();
        if (caixa != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(caixa.center, caixa.size);
        }
    }
}
