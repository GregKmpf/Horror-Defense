using UnityEngine;
using UnityEngine.Events;

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

    // Desenha uma esfera vermelha na Scene View para você ver a posição
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }

    void OnTriggerEnter(Collider outro)
{
    if (outro.CompareTag("Inimigo"))
    {
        ReceberDano(10);
        Destroy(outro.gameObject); // inimigo some ao chegar
    }
}
}