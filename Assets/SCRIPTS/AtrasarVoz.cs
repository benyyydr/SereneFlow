using UnityEngine;
using System.Collections;

/// <summary>
/// Gere o atraso e a reprodução sequencial de efeitos de introdução (como ruídos) 
/// e das falas do robô assistente, sincronizando a ativação dos painéis de legendas.
/// </summary>
public class AtrasarVoz : MonoBehaviour
{
    [Header("Som de Intro (Opcional)")]
    [Tooltip("Som reproduzido antes do início da locução do robô (ex: som da máquina de ressonância).")]
    public AudioSource somIntro;
    
    [Tooltip("Tempo em segundos durante o qual o som de introdução será executado antes da voz iniciar.")]
    public float duracaoIntro = 10f;

    [Tooltip("Volume ajustado para o som de introdução.")]
    public float volumeIntro = 0.5f;

    [Header("Voz")]
    [Tooltip("Fonte de áudio que contém a fala do robô assistente.")]
    public AudioSource vozDoRobo;
    
    [Tooltip("Tempo de espera inicial (em segundos) antes de começar toda a sequência.")]
    public float tempoDeEspera = 2f;

    [Header("Painel de Legenda")]
    [Tooltip("Referência ao script do painel tridimensional de legendas que acompanha a fala.")]
    public PainelFala painelDaFala;

    [Header("Comportamento")]
    [Tooltip("Se ativo, inicia automaticamente a sequência de atraso no arranque (Start) do script.")]
    public bool dispararNoStart = true;

    /// <summary>
    /// Método de inicialização do Unity.
    /// </summary>
    void Start()
    {
        if (dispararNoStart)
            IniciarAtraso();
    }

    /// <summary>
    /// Inicia publicamente a corrotina de atraso e reprodução sequencial.
    /// </summary>
    public void IniciarAtraso()
    {
        StartCoroutine(RotinaAtraso());
    }

    /// <summary>
    /// Corrotina responsável por controlar os tempos de espera, áudio e legendas em sequência.
    /// </summary>
    private IEnumerator RotinaAtraso()
    {
        // Aguarda o tempo de espera inicial configurado
        yield return new WaitForSeconds(tempoDeEspera);

        // Se existir um som de introdução, configura o volume, reproduz-o e aguarda a sua duração
        if (somIntro != null)
        {
            somIntro.volume = volumeIntro;
            somIntro.Play();
            yield return new WaitForSeconds(duracaoIntro);
            somIntro.Stop(); // Interrompe o som de introdução para dar prioridade à voz
        }

        // Toca a fala do robô assistente
        if (vozDoRobo != null)
            vozDoRobo.Play();

        // Se houver painel de legendas e voz configurados, ativa e sincroniza as legendas
        if (painelDaFala != null && vozDoRobo != null)
            painelDaFala.MostrarComFala(vozDoRobo);
    }
}