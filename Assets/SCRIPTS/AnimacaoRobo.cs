using UnityEngine;

/// <summary>
/// Controla o estado de animação de fala do robô assistente.
/// Monitoriza uma lista de fontes de áudio (AudioSources) e ativa a animação
/// de fala no Animator sempre que qualquer som estiver a ser reproduzido.
/// </summary>
public class AnimacaoRobo : MonoBehaviour
{
    [Header("O Cérebro das Animações")]
    [Tooltip("Componente Animator do robô que gere a transição dos estados de animação.")]
    public Animator animador;
    
    [Tooltip("Nome exato do parâmetro Booleano criado no Animator Controller (ex: estaAFalar).")]
    public string parametroFalar = "estaAFalar";

    [Header("As Vozes a Vigiar")]
    [Tooltip("Lista de AudioSources do robô cujos estados de reprodução serão monitorizados.")]
    public AudioSource[] falasDoRobo;

    /// <summary>
    /// Executado a cada frame para atualizar o estado da animação com base na reprodução do áudio.
    /// </summary>
    void Update()
    {
        // Se o componente Animator não estiver associado, interrompe a execução para evitar erros
        if (animador == null) return;

        bool aFalar = false;

        // Percorre a lista de fontes de áudio configuradas
        foreach (AudioSource audio in falasDoRobo)
        {
            // Se o áudio for válido e estiver ativamente a ser reproduzido
            if (audio != null && audio.isPlaying)
            {
                aFalar = true;
                break; // Se encontrou uma fala a tocar, para a pesquisa (otimização de performance)
            }
        }

        // Atualiza o parâmetro booleano no Animator Controller para ligar/desligar a animação
        animador.SetBool(parametroFalar, aFalar);
    }
}