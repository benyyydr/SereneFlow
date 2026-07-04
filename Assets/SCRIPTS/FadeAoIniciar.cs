using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Faz o fade-in (de preto para visível) ao entrar nesta scene.
/// Cada scene tem a sua própria Image — não depende de nenhum
/// objeto persistente entre scenes.
/// </summary>
public class FadeAoIniciar : MonoBehaviour
{
    [Header("Painel de Fade desta Scene")]
    [Tooltip("Painel de imagem UI preto que cobre o ecrã para fazer o efeito de desvanecimento.")]
    public Image ecraEscuro;
    
    [Tooltip("Tempo em segundos que demora o ecrã a ficar totalmente visível.")]
    public float tempoFade = 0.5f;

    /// <summary>
    /// Método de inicialização. Configura o painel como preto e inicia o desvanecimento.
    /// </summary>
    void Start()
    {
        // Se o componente de Imagem não for associado manualmente, tenta obtê-lo no próprio objeto
        if (ecraEscuro == null) ecraEscuro = GetComponent<Image>();

        if (ecraEscuro != null)
        {
            // Força o ecrã a começar totalmente preto (Alpha = 1) para esconder o carregamento de objetos
            Color c = ecraEscuro.color;
            c.a = 1f;
            ecraEscuro.color = c;

            // Inicia o processo de transição para visível
            StartCoroutine(RotinaFadeOut());
        }
        else
        {
            Debug.LogWarning("[FadeAoIniciar] Nenhuma imagem de ecrã escuro foi encontrada!");
        }
    }

    /// <summary>
    /// Corrotina responsável por interpolar a opacidade do ecrã de preto (1) para transparente (0).
    /// </summary>
    private IEnumerator RotinaFadeOut()
    {
        float tempo = 0f;
        Color corInicial = ecraEscuro.color;
        Color corFinal = new Color(0f, 0f, 0f, 0f); // Preto totalmente transparente

        // Interpola a cor frame a frame ao longo do tempo de fade configurado
        while (tempo < tempoFade)
        {
            tempo += Time.deltaTime;
            ecraEscuro.color = Color.Lerp(corInicial, corFinal, tempo / tempoFade);
            yield return null;
        }

        // Garante que o ecrã termina com transparência total
        ecraEscuro.color = corFinal;
    }
}