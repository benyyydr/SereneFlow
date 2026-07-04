using UnityEngine;
using UnityEngine.Android;
using System.Collections;

/// <summary>
/// Pede a permissão de microfone logo ao início da experiência, sem qualquer
/// painel ou mensagem extra — só o popup nativo do Android.
///
/// Se a permissão for negada, a propriedade estática TemPermissao fica false.
/// O ControloMaca consulta esta propriedade no momento em que normalmente
/// ativaria a IA: se não houver permissão, avança diretamente para o exame
/// real, sem tentar gravar ou comunicar com o GravadorVoz/ServicoGemini.
///
/// Coloca este script num objeto vazio na FlorestaScene, no início da experiência,
/// antes do Jammo começar a falar.
/// </summary>
public class PedidoPermissaoMicrofone : MonoBehaviour
{
    /// <summary>
    /// True se a permissão foi concedida. Acessível globalmente por
    /// qualquer outro script (ex: ControloMaca) sem precisar de referência direta.
    /// </summary>
#if UNITY_EDITOR
    // No Editor do Unity, assume sempre permissão concedida por padrão para facilitar testes rápidos
    public static bool TemPermissao { get; private set; } = true;
#else
    // Em dispositivos móveis reais (Meta Quest), assume false até ser expressamente concedida pelo utilizador
    public static bool TemPermissao { get; private set; } = false;
#endif

    /// <summary>
    /// Método de inicialização do Unity. Pede a permissão logo ao arrancar a experiência.
    /// </summary>
    void Start()
    {
        StartCoroutine(PedirPermissaoRotina());
    }

    /// <summary>
    /// Corrotina que interage com o sistema Android para solicitar e aguardar a permissão de áudio.
    /// </summary>
    private IEnumerator PedirPermissaoRotina()
    {
#if UNITY_ANDROID
        // Verifica se o dispositivo (Meta Quest correndo Android) não tem a permissão autorizada
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            // Dispara a caixa de diálogo popup nativa do Android a pedir acesso ao microfone
            Permission.RequestUserPermission(Permission.Microphone);

            float tempoEspera = 0f;
            const float tempoLimite = 30f; // Timeout de segurança de 30 segundos

            // Aguarda numa contagem até que o utilizador responda ao popup do Android ou o tempo limite expire
            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone) && tempoEspera < tempoLimite)
            {
                tempoEspera += Time.deltaTime;
                yield return null;
            }

            // Atualiza o estado da propriedade global com base na escolha final do utilizador
            TemPermissao = Permission.HasUserAuthorizedPermission(Permission.Microphone);
        }
        else
        {
            // Se já tinha sido anteriormente autorizada, ativa de imediato
            TemPermissao = true;
        }

        Debug.Log("[PedidoPermissaoMicrofone] TemPermissao = " + TemPermissao);
#else
        // Fallback para PC/Editor ou outras plataformas que não exijam popups Android em execução
        TemPermissao = true;
        yield return null;
#endif
    }
}