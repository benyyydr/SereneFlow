using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Representa uma locução individual associada a uma chave identificadora.
/// </summary>
[System.Serializable]
public class FalaTematica
{
    [Tooltip("Identificador único ou texto transcrito da fala.")]
    public string chave;
    
    [Tooltip("Ficheiro de áudio com a locução correspondente.")]
    public AudioClip audio;
}

/// <summary>
/// Agrupa várias falas temáticas sob um mesmo tema emocional (ex: medo_barulho, medo_espaco).
/// Permite selecionar uma fala aleatoriamente para evitar repetições exaustivas.
/// </summary>
[System.Serializable]
public class GrupoFalas
{
    [Tooltip("Nome do tema que categoriza este grupo de falas.")]
    public string tema;
    
    [Tooltip("Lista de locuções temáticas pertencentes a este tema.")]
    public List<FalaTematica> falas = new List<FalaTematica>();

    /// <summary>
    /// Seleciona e devolve uma fala aleatória de dentro deste grupo.
    /// </summary>
    /// <returns>Uma FalaTematica aleatória, ou null se a lista estiver vazia.</returns>
    public FalaTematica ObterFalaAleatoria()
    {
        if (falas == null || falas.Count == 0) return null;
        return falas[Random.Range(0, falas.Count)];
    }
}

/// <summary>
/// Gestor central do banco de vozes do robô.
/// Organiza os grupos de falas num dicionário rápido para acesso em tempo de execução
/// com base no tema emocional detetado pela inteligência artificial.
/// </summary>
public class BancoDeFalas : MonoBehaviour
{
    [Header("Grupos de Falas por Tema")]
    [Tooltip("Lista de grupos de falas definidos diretamente no Inspetor do Unity.")]
    public List<GrupoFalas> grupos = new List<GrupoFalas>();

    // Dicionário para pesquisas rápidas O(1) em memória durante o jogo
    private Dictionary<string, GrupoFalas> dicionario;

    /// <summary>
    /// Inicialização do script. Converte a lista do Inspetor num dicionário para otimização de pesquisas.
    /// </summary>
    void Awake()
    {
        dicionario = new Dictionary<string, GrupoFalas>();
        foreach (var grupo in grupos)
        {
            if (!string.IsNullOrEmpty(grupo.tema))
            {
                dicionario[grupo.tema] = grupo;
            }
        }
    }

    /// <summary>
    /// Obtém uma fala temática aleatória com base num tema emocional.
    /// </summary>
    /// <param name="tema">O identificador do tema emocional a pesquisar.</param>
    /// <returns>Uma FalaTematica correspondente ao tema, ou null se o tema não existir.</returns>
    public FalaTematica ObterFala(string tema)
    {
        // Tenta encontrar o grupo correspondente ao tema no dicionário
        if (dicionario.TryGetValue(tema, out GrupoFalas grupo))
        {
            return grupo.ObterFalaAleatoria();
        }

        // Emite um aviso no console caso o tema enviado pelo Gemini não exista no banco de falas
        Debug.LogWarning("⚠️ Tema não encontrado no banco de falas: " + tema);
        return null;
    }
}