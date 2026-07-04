using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Controla a exibição, sincronização e efeitos de transparência (Fade-In/Fade-Out)
/// do painel tridimensional de legendas que acompanha as falas do robô.
/// </summary>
public class PainelFala : MonoBehaviour
{
    [Header("Arraste os componentes aqui")]
    [Tooltip("O plano 3D que serve de fundo translúcido para o painel (efeito de vidro).")]
    public Renderer planoRenderer;
    
    [Tooltip("Componente TextMeshPro que renderiza as legendas em VR.")]
    public TextMeshPro textoTMP;
    
    [Tooltip("Referência ao script de efeito máquina de escrever associado.")]
    public TextoAnimado textoAnimado;

    [Header("Configuração")]
    [Tooltip("Fonte de áudio interna padrão associada a este painel (opcional).")]
    public AudioSource fala;
    
    [Tooltip("Duração das transições de opacidade (fade) do painel.")]
    public float tempoFade = 0.5f;

    /// <summary>
    /// Configurações iniciais. Tenta obter referências esquecidas e oculta o painel.
    /// </summary>
    void Awake()
    {
        // Fallback automático caso as referências não tenham sido arrastadas no Inspetor
        if (planoRenderer == null) planoRenderer = GetComponent<Renderer>();
        if (textoTMP == null) textoTMP = GetComponentInChildren<TextMeshPro>();
        if (textoAnimado == null) textoAnimado = GetComponentInChildren<TextoAnimado>();

        // Começa totalmente invisível
        SetAlpha(0f);
    }

    /// <summary>
    /// Exibe o painel e inicia a sincronização das legendas com um áudio externo.
    /// </summary>
    /// <param name="audioExterno">O AudioSource cuja reprodução será monitorizada.</param>
    public void MostrarComFala(AudioSource audioExterno)
    {
        StopAllCoroutines(); // Cancela animações de fade anteriores para evitar conflitos
        StartCoroutine(SincronizarComAudio(audioExterno));
    }

    /// <summary>
    /// Corrotina que sincroniza a exibição do painel e a escrita do texto com a duração do áudio.
    /// </summary>
    private IEnumerator SincronizarComAudio(AudioSource audio)
    {
        if (audio == null) yield break;

        // Aguarda até que a faixa de áudio comece fisicamente a ser reproduzida
        yield return new WaitUntil(() => audio.isPlaying);

        // Inicia o efeito de máquina de escrever sincronizado com o áudio
        if (textoAnimado != null)
            textoAnimado.Iniciar(audio);

        // Faz o fade-in para tornar o painel visível
        yield return StartCoroutine(FadeIn());
        
        // Aguarda em repouso enquanto o robô estiver a falar
        yield return new WaitWhile(() => audio.isPlaying);

        // Para o efeito de máquina de escrever
        if (textoAnimado != null)
            textoAnimado.Parar();

        // Faz o fade-out do painel e das letras
        yield return StartCoroutine(FadeOut());
    }

    /// <summary>
    /// Aplica um valor de opacidade (alpha) ao shader do vidro e ao texto do TextMeshPro.
    /// </summary>
    /// <param name="alpha">Valor entre 0 (invisível) e 1 (totalmente visível).</param>
    private void SetAlpha(float alpha)
    {
        // Controla a transparência do material de Vidro (Glassmorphism)
        if (planoRenderer != null && planoRenderer.material != null)
        {
            // Tenta definir a propriedade global de opacidade do nosso shader HLSL customizado
            if(planoRenderer.material.HasProperty("_GlobalAlpha"))
                planoRenderer.material.SetFloat("_GlobalAlpha", alpha);
        }

        // Controla a transparência do texto do TextMeshPro
        if (textoTMP != null)
        {
            Color cor = textoTMP.color;
            cor.a = alpha;
            textoTMP.color = cor;
        }
    }

    /// <summary>
    /// Transição suave para tornar o painel e o texto visíveis.
    /// </summary>
    private IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < tempoFade)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(t / tempoFade));
            yield return null;
        }
        SetAlpha(1f);
    }

    /// <summary>
    /// Transição suave para ocultar o painel e o texto.
    /// </summary>
    private IEnumerator FadeOut()
    {
        float t = tempoFade;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            SetAlpha(Mathf.Clamp01(t / tempoFade));
            yield return null;
        }
        SetAlpha(0f);
    }

    /// <summary>
    /// Sobrecarga para iniciar o painel utilizando a locução interna configurada.
    /// </summary>
    public void MostrarComFala()
    {
        if (fala == null) { Debug.LogError("[PainelFala] Fala é null em: " + gameObject.name); return; }
        MostrarComFala(fala);
    }
}