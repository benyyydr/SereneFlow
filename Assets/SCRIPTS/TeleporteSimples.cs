using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Gere o teletransporte do jogador. Suporta tanto a mudança física para outra cena
/// como o reposicionamento local dentro do mesmo cenário, integrando efeitos de fade preto
/// e bloqueios baseados no término de falas do robô.
/// </summary>
public class TeleporteSimples : MonoBehaviour
{
    // Modos de teletransporte suportados
    public enum ModoTeleporte { MudarScene, TeleporteLocal }

    [Header("Modo")]
    [Tooltip("Define se a interação carrega uma nova cena ou move o jogador localmente.")]
    public ModoTeleporte modo = ModoTeleporte.MudarScene;

    [Header("Scene de Destino (so se Modo = MudarScene)")]
    [Tooltip("Nome exato da cena que será carregada (deve estar nas Build Settings).")]
    public string nomeSceneDestino;

    [Header("Teleporte Local (so se Modo = TeleporteLocal)")]
    [Tooltip("Referência ao objeto do jogador (XR Origin).")]
    public GameObject jogador;
    
    [Tooltip("Ponto de destino para onde o jogador será teletransportado.")]
    public Transform destino;
    
    [Tooltip("Objeto do robô assistente para ser movido em conjunto.")]
    public GameObject roboAssistente;
    
    [Tooltip("Ponto de destino para onde o robô assistente será reposicionado.")]
    public Transform destinoDoRobo;

    [Header("Efeitos Visuais (Image desta scene)")]
    [Tooltip("Painel de imagem UI preto utilizado para o efeito de fade nesta cena.")]
    public Image ecraEscuro;
    
    [Tooltip("Duração do fade de transição de opacidade.")]
    public float tempoFade = 0.5f;

    [Header("Bloqueio por Voz (Opcional)")]
    [Tooltip("Áudio que bloqueia o teletransporte (jogador tem de o ouvir antes de poder teleportar).")]
    public AudioSource vozDoRobo;

    [Header("Voz na Chegada - so usado em TeleporteLocal")]
    [Tooltip("Script de áudio que será executado automaticamente ao chegar ao destino local.")]
    public AtrasarVoz vozChegada;

    // Estado de controlo interno
    private Behaviour componenteVR;
    private bool jaComecouAFalar = false;
    private bool podeTeleportar  = false;

    /// <summary>
    /// Configurações iniciais do botão de teletransporte virtual.
    /// </summary>
    void Start()
    {
        componenteVR = (Behaviour)GetComponent("XRSimpleInteractable");

        // Se não houver voz a bloquear, destranca o teletransporte imediatamente
        if (vozDoRobo == null)
            podeTeleportar = true;
        else
            if (componenteVR != null) componenteVR.enabled = false;
    }

    /// <summary>
    /// Monitoriza o áudio de bloqueio para destrancar a interação quando o som terminar.
    /// </summary>
    void Update()
    {
        if (podeTeleportar || vozDoRobo == null) return;

        if (vozDoRobo.isPlaying && !jaComecouAFalar)
            jaComecouAFalar = true;

        if (jaComecouAFalar && !vozDoRobo.isPlaying)
        {
            podeTeleportar = true;
            if (componenteVR != null) componenteVR.enabled = true;
        }
    }

    /// <summary>
    /// Evento público chamado pela interação VR para iniciar a transição.
    /// </summary>
    public void FazerTeleporte()
    {
        if (!podeTeleportar) return;
        StartCoroutine(RotinaDeTeleporte());
    }

    /// <summary>
    /// Corrotina que executa o escurecimento do ecrã, move o jogador e restaura a visualização.
    /// </summary>
    private IEnumerator RotinaDeTeleporte()
    {
        // Escurece o ecrã para preto
        yield return StartCoroutine(FazerFade(1f));

        if (modo == ModoTeleporte.MudarScene)
            ExecutarMudancaScene();
        else
            yield return StartCoroutine(ExecutarTeleporteLocal());

        // Se mudou de cena, a imagem de fade desta cena deixa de existir.
        // O fade-in de entrada é tratado pelo script FadeAoIniciar da nova cena.
        // Só realizamos o fade-in local aqui se nos mantivermos na mesma cena.
        if (modo == ModoTeleporte.TeleporteLocal)
        {
            yield return StartCoroutine(FazerFade(0f));

            // Ativa o som de chegada configurado para o destino local
            if (vozChegada != null)
                vozChegada.IniciarAtraso();
        }
    }

    /// <summary>
    /// Carrega a nova cena do Unity.
    /// </summary>
    private void ExecutarMudancaScene()
    {
        if (string.IsNullOrEmpty(nomeSceneDestino))
        {
            Debug.LogError("[TeleporteSimples] Nome da scene de destino nao definido!");
            return;
        }

        SceneManager.LoadScene(nomeSceneDestino);
    }

    /// <summary>
    /// Teleporta o jogador e o robô localmente, desativando componentes físicos temporariamente para evitar colisões erradas.
    /// </summary>
    private IEnumerator ExecutarTeleporteLocal()
    {
        if (jogador == null || destino == null)
        {
            Debug.LogError("[TeleporteSimples] Jogador ou Destino nao definido!");
            yield break;
        }

        // Guarda o estado e desativa o CharacterController para não travar o movimento físico do Rig
        CharacterController cc = jogador.GetComponent<CharacterController>();
        bool ccWasOn = cc != null && cc.enabled;

        // Desativa sistemas de locomoção manual durante o salto
        var locomotions = jogador.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>();
        bool[] locoStates = new bool[locomotions.Length];
        for (int i = 0; i < locomotions.Length; i++)
        {
            locoStates[i] = locomotions[i].enabled;
            locomotions[i].enabled = false;
        }

        if (cc != null) cc.enabled = false;

        // Oculta os lasers de interação para evitar cliques acidentais durante a transição
        var lasers = jogador.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
        foreach (var laser in lasers) laser.enabled = false;

        // Reposiciona o Rig/Jogador
        var xrOrigin = jogador.GetComponent<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            // Move a câmara compensando a posição física real do utilizador no espaço de jogo
            xrOrigin.MoveCameraToWorldLocation(destino.position);
            xrOrigin.MatchOriginUpCameraForward(Vector3.up, destino.forward);
        }
        else
        {
            // Fallback para transform básico caso não seja um dispositivo VR
            jogador.transform.position = destino.position;
            jogador.transform.rotation = destino.rotation;
        }

        // Reposiciona o robô assistente para o seu ponto reservado
        if (roboAssistente != null && destinoDoRobo != null)
        {
            roboAssistente.transform.position = destinoDoRobo.position;
            roboAssistente.transform.rotation = destinoDoRobo.rotation;
        }

        // Aguarda dois frames físicos para garantir o correto posicionamento no motor de física do Unity
        yield return null;
        yield return null;

        // Restaura os estados anteriores dos controladores e lasers
        if (cc != null) cc.enabled = ccWasOn;
        for (int i = 0; i < locomotions.Length; i++)
            locomotions[i].enabled = locoStates[i];
        foreach (var laser in lasers) laser.enabled = true;
    }

    /// <summary>
    /// Interpola a opacidade do ecrã de fade preto.
    /// </summary>
    private IEnumerator FazerFade(float alvoAlpha)
    {
        if (ecraEscuro == null) yield break;

        float tempo = 0f;
        Color corInicial = ecraEscuro.color;
        Color corFinal   = new Color(0f, 0f, 0f, alvoAlpha);

        while (tempo < tempoFade)
        {
            tempo += Time.deltaTime;
            ecraEscuro.color = Color.Lerp(corInicial, corFinal, tempo / tempoFade);
            yield return null;
        }
        ecraEscuro.color = corFinal;
    }
}