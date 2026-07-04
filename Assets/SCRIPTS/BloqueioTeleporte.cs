using UnityEngine;

/// <summary>
/// Bloqueia temporariamente os scripts de teletransporte e de interação VR 
/// no início da cena. Estes componentes são reativados automaticamente 
/// apenas quando a locução de áudio inicial do robô terminar.
/// </summary>
public class BloqueioTeleporte : MonoBehaviour
{
    [Header("Áudio de Bloqueio")]
    [Tooltip("Fonte de áudio que bloqueia a passagem (ex: a voz explicativa do robô).")]
    public AudioSource vozDoRobo;         
    
    [Header("Componentes a Bloquear")]
    [Tooltip("Referência ao script de teletransporte (ex: TeleporteSimples).")]
    public Behaviour scriptTeleporteSimples; 
    
    [Tooltip("Referência ao componente de interação física do Unity XR (ex: XRBaseInteractable).")]
    public Behaviour scriptXRInteractable;
    
    // Variáveis de controlo de estado interno
    private bool jaComecouAFalar = false;
    private bool jaDesbloqueou = false;

    /// <summary>
    /// Método de inicialização. Desativa os componentes de interação logo no início da cena.
    /// </summary>
    void Start()
    {
        // Garante que o jogador não se move nem interage com objetos antes de ouvir as explicações
        if (scriptTeleporteSimples != null) scriptTeleporteSimples.enabled = false;
        if (scriptXRInteractable != null) scriptXRInteractable.enabled = false;
    }

    /// <summary>
    /// Monitoriza frame a frame o estado do áudio para detetar quando o robô termina de falar.
    /// </summary>
    void Update()
    {
        // Se a passagem já foi desbloqueada, ignora o resto da lógica (otimização de processamento)
        if (jaDesbloqueou) return;

        // Deteta o momento exato em que o robô começa a falar
        if (vozDoRobo.isPlaying && !jaComecouAFalar)
        {
            jaComecouAFalar = true; 
        }

        // Se o robô já falou e agora a faixa de áudio parou de tocar, desbloqueia a interação
        if (jaComecouAFalar && !vozDoRobo.isPlaying)
        {
            DesbloquearPassagem();
        }
    }

    /// <summary>
    /// Reativa os scripts de movimentação e interação do jogador.
    /// </summary>
    void DesbloquearPassagem()
    {
        jaDesbloqueou = true;
        
        // Reativa os componentes para que o utilizador possa navegar livremente
        if (scriptTeleporteSimples != null) scriptTeleporteSimples.enabled = true;
        if (scriptXRInteractable != null) scriptXRInteractable.enabled = true;
        
        Debug.Log("VR ativado! Já podes olhar para a porta.");
    }
}