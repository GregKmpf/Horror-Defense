using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// Guarda as 5 melhores partidas de cada dificuldade no PlayerPrefs.
// Ordem: mais pontos primeiro; empate decidido pelo menor tempo.
public static class Ranking
{
    private const int MaxEntradas = 5;

    private struct Entrada
    {
        public int pontos;
        public float tempo;
    }

    private static string Chave(DifficultyDefinition dificuldade, int posicao)
    {
        return $"ranking_{dificuldade.name}_{posicao}";
    }

    private static List<Entrada> Carregar(DifficultyDefinition dificuldade)
    {
        List<Entrada> lista = new List<Entrada>();
        for (int i = 0; i < MaxEntradas; i++)
        {
            string[] partes = PlayerPrefs.GetString(Chave(dificuldade, i), "").Split('|');
            if (partes.Length == 2
                && int.TryParse(partes[0], out int pontos)
                && float.TryParse(partes[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float tempo))
            {
                lista.Add(new Entrada { pontos = pontos, tempo = tempo });
            }
        }
        return lista;
    }

    // Retorna a posição (1 a 5) que a partida ocupou, ou -1 se ficou fora do top 5
    public static int Salvar(DifficultyDefinition dificuldade, int pontos, float tempo)
    {
        List<Entrada> lista = Carregar(dificuldade);
        Entrada nova = new Entrada { pontos = pontos, tempo = tempo };
        lista.Add(nova);
        lista.Sort((a, b) => a.pontos != b.pontos ? b.pontos.CompareTo(a.pontos) : a.tempo.CompareTo(b.tempo));

        int posicao = lista.FindIndex(e => e.pontos == nova.pontos && e.tempo == nova.tempo);

        for (int i = 0; i < MaxEntradas && i < lista.Count; i++)
        {
            string texto = lista[i].pontos + "|" + lista[i].tempo.ToString(CultureInfo.InvariantCulture);
            PlayerPrefs.SetString(Chave(dificuldade, i), texto);
        }
        PlayerPrefs.Save();

        return posicao < MaxEntradas ? posicao + 1 : -1;
    }

    // Texto pronto para mostrar na tela
    public static string Texto(DifficultyDefinition dificuldade)
    {
        List<Entrada> lista = Carregar(dificuldade);
        if (lista.Count == 0)
        {
            return "Sem registros";
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < lista.Count; i++)
        {
            sb.AppendLine($"{i + 1}.  {lista[i].pontos} pts   {FormatarTempo(lista[i].tempo)}");
        }
        return sb.ToString();
    }

    public static string FormatarTempo(float segundos)
    {
        int total = Mathf.FloorToInt(segundos);
        return $"{total / 60}:{total % 60:D2}";
    }
}