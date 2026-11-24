using TMPro;
using UnityEngine;

// Conta os pontos da partida (dinheiro dos inimigos eliminados), salva no ranking e mostra a tela de fim.
// Fica na cena do jogo (pode ser no mesmo objeto do GameManager).
public class TelaFimDeJogo : MonoBehaviour
{
    [SerializeField] private GameObject painel;
    [SerializeField] private TMP_Text titulo;
    [SerializeField] private TMP_Text resumo;

    private GameManager jogo;
    private int pontos;

    private void Awake()
    {
        painel.SetActive(false);
    }

    private void OnEnable()
    {
        Enemy.AnyDied += AoInimigoMorrer;
    }

    private void OnDisable()
    {
        Enemy.AnyDied -= AoInimigoMorrer;
    }

    private void Start()
    {
        jogo = GameManager.Instance;
        if (jogo == null)
        {
            Debug.LogError("TelaFimDeJogo: GameManager não encontrado na cena.", this);
            return;
        }
        jogo.GameEnded += AoFimDeJogo;
    }

    private void OnDestroy()
    {
        if (jogo != null)
        {
            jogo.GameEnded -= AoFimDeJogo;
        }
    }

    // Conta o total ganho, não o saldo: gastar em torres não reduz a pontuação
    private void AoInimigoMorrer(Enemy inimigo)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            pontos += inimigo.definition.bounty;
        }
    }

    private void AoFimDeJogo(GameState resultado, GameEndReason motivo)
    {
        int posicao = Ranking.Salvar(jogo.Difficulty, pontos, jogo.TimeElapsed);

        titulo.text = resultado == GameState.Won ? "VITÓRIA" : "DERROTA";

        string motivoTexto = motivo == GameEndReason.BaseDestroyed ? "A base foi destruída."
                           : motivo == GameEndReason.TimeUp ? "O tempo acabou."
                           : "Você sobreviveu a todas as ondas.";

        string posicaoTexto = posicao > 0 ? $"Você ficou em {posicao}º no ranking!" : "Fora do top 5.";

        resumo.text = $"{motivoTexto}\n\n" +
                      $"Pontos: {pontos}\n" +
                      $"Tempo: {Ranking.FormatarTempo(jogo.TimeElapsed)}\n" +
                      $"{posicaoTexto}\n\n" +
                      $"{jogo.Difficulty.displayName.ToUpper()}\n{Ranking.Texto(jogo.Difficulty)}";

        painel.SetActive(true);
    }
}