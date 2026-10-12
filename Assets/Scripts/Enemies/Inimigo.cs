using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Inimigo : MonoBehaviour
{
    [Header("Atributos")]
    public float velocidade = 3.5f;
    public int vida = 50;
    public int dano = 10;

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = velocidade;

        if (Objetivo.Instancia != null)
        {
            agent.SetDestination(Objetivo.Instancia.transform.position);
        }
        else
        {
            Debug.LogWarning("Nenhum Objetivo encontrado na cena!");
        }
    }

    public void ReceberDano(int valor)
    {
        vida -= valor;
        if (vida <= 0)
        {
            Morrer();
        }
    }

    void Morrer()
    {
        Destroy(gameObject);
    }
}