using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Unity.XR.CoreUtils;

// Enumeração para identificar os estados de animação da esfera de respiração
public enum TipoAnimacao { Nenhuma, Crescer, ManterTopo, Diminuir, ManterFundo }

// Estrutura para configurar cada passo do exercício de respiração guiada
[System.Serializable]
public struct PassoRespiracao
{
    public string nomePasso; 
    public AudioClip somVoz;
    public TipoAnimacao animacaoEsfera;
}

/// <summary>
/// Controlador principal da cena da sala de exames.
/// Orquestra a sequência temporal (deitar, deslizar a maca, respiração guiada, som da máquina e saída),
/// gere o bloqueio de rotação/altura da câmara do utilizador e integra com a Inteligência Artificial.
/// </summary>
public class ControloMaca : MonoBehaviour
{
    [Header("Movimento e Posições")]
    public GameObject jogador;
    public Transform pontoPaciente; 
    public Transform pontoDentroTunel;
    public Transform pontoSaida;
    public float tempoDeslizePadrao = 5f; 

    [Header("Efeitos Visuais")]
    public Image ecraEscuro;
    public float tempoFade = 0.5f;

    [Header("Bloqueio Inicial")]
    public AudioSource vozQueDestranca; 

    [Header("A Cena do Túnel (Atores Gerais)")]
    public AudioSource vozEntrada;       
    public AudioSource vozPerguntaPronto; 
    public AudioSource vozSaida;
    public AudioSource somMRIAlto;       
    public GameObject esferaLuz;

    [Header("Exercício de Respiração Modular")]
    public AudioSource fonteVozRobo; 
    public PassoRespiracao[] passosRespiracao; 

    [Header("Inteligência Artificial")]
    public GravadorVoz gestorDeVoz;

    [Header("Limite da Cabeça na Maca")]
    public float limiteOlharParaTras = -35f;   // Pitch mínimo (olhar para baixo/trás)
    public float limiteOlharParaCima  =  60f;  // Pitch máximo (olhar para cima)
    public float limiteYaw            =  60f;  // Graus máximos permitidos para rodar a cabeça
    private float yawInicialMaca;              // Rotação horizontal (yaw) inicial de referência
    private Vector3 posicaoInicialPaiCamara;   // Posição inicial do "Camera Offset"
    private float alturaInicialCamara;          // Altura Y de deitado guardada para trancamento

    [Header("Placa Final Diegética")]
    public PainelFala placaFinal;

    // Variáveis privadas de controlo
    private Vector3 posicaoInicialCama;
    private Behaviour componenteVR;
    private bool jaComecouAFalar = false;
    private bool podeIniciar = false;
    private bool exameTerminado = false;
    private bool limitarCabecaNaMaca = false;
    private Transform camaraXR;
    private Vector3 escalaInicialEsfera;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor[] lasers;

    // Inicialização do componente
    void Start()
    {
        posicaoInicialCama = transform.position; 
        componenteVR = (Behaviour)GetComponent("XRSimpleInteractable");

        if (esferaLuz != null)
        {
            escalaInicialEsfera = esferaLuz.transform.localScale;
            esferaLuz.SetActive(false);
        }

        if (vozQueDestranca == null) podeIniciar = true;
        else if (componenteVR != null) componenteVR.enabled = false;

        // Regista automaticamente todos os áudios e o microfone desta cena na Música Clínica persistente
        if (MusicaClinica.Instancia != null)
        {
            // Limpa as referências de AudioSources destruídos da cena anterior (Sala de Espera)
            MusicaClinica.Instancia.LimparReferenciasNulas();

            System.Collections.Generic.List<AudioSource> sonsDaCena = new System.Collections.Generic.List<AudioSource>();

            if (vozEntrada != null) sonsDaCena.Add(vozEntrada);
            if (vozPerguntaPronto != null) sonsDaCena.Add(vozPerguntaPronto);
            if (vozSaida != null) sonsDaCena.Add(vozSaida);
            if (fonteVozRobo != null) sonsDaCena.Add(fonteVozRobo);

            // Adiciona a coluna de voz da IA se estiver configurada
            if (gestorDeVoz != null && gestorDeVoz.servicoIA != null && gestorDeVoz.servicoIA.colunaDoRobo != null)
            {
                sonsDaCena.Add(gestorDeVoz.servicoIA.colunaDoRobo);
            }

            // Adiciona de forma cumulativa sem sobrescrever o que o AtrasarVoz já possa ter registado
            MusicaClinica.Instancia.AdicionarFalas(sonsDaCena.ToArray());

            // Regista o microfone/gravador do paciente
            if (gestorDeVoz != null)
            {
                MusicaClinica.Instancia.RegistarSonsDaScene(MusicaClinica.Instancia.falasParaOuvir, gestorDeVoz);
            }
        }
    }

    // Monitorização a cada frame (destranque da maca)
    void Update()
    {
        if (podeIniciar || vozQueDestranca == null) return;
        if (vozQueDestranca.isPlaying && !jaComecouAFalar) jaComecouAFalar = true;
        if (jaComecouAFalar && !vozQueDestranca.isPlaying)
        {
            podeIniciar = true;
            if (componenteVR != null) componenteVR.enabled = true;
        }
    }

    // Limitação matemática de rotação e translação da cabeça do utilizador
    void LateUpdate()
    {
        if (!limitarCabecaNaMaca || camaraXR == null) return;

        Transform paiCamara = camaraXR.parent;
        if (paiCamara == null) return;

        Quaternion localRot = camaraXR.localRotation;
        Vector3 forward = localRot * Vector3.forward;

        // --- CÁLCULO E LIMITAÇÃO DO PITCH (Olhar para Cima/Baixo) ---
        float forwardY = Mathf.Clamp(forward.y, -1f, 1f);
        float pitch = -Mathf.Asin(forwardY) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, limiteOlharParaTras, limiteOlharParaCima);

        // --- CÁLCULO E LIMITAÇÃO DO YAW (Olhar para a Esquerda/Direita) ---
        float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        float deltaYaw = Mathf.DeltaAngle(yawInicialMaca, yaw);
        deltaYaw = Mathf.Clamp(deltaYaw, -limiteYaw, limiteYaw);
        float clampedYaw = yawInicialMaca + deltaYaw;

        // Mantém a inclinação de roll física original da câmara
        float roll = localRot.eulerAngles.z;

        // Determina a rotação final desejada em relação ao XROrigin
        Quaternion rotacaoDesejada = Quaternion.Euler(pitch, clampedYaw, roll);

        // Aplica a rotação de compensação no pai (Camera Offset) para anular a sobreposição do TrackedPoseDriver
        paiCamara.localRotation = rotacaoDesejada * Quaternion.Inverse(localRot);

        // --- COMPENSAÇÃO DE TRANSLAÇÃO COM TRANCAMENTO DE ALTURA (Y) ---
        Vector3 posCompensada = posicaoInicialPaiCamara + camaraXR.localPosition - (paiCamara.localRotation * camaraXR.localPosition);

        // Mantém X e Z compensados lateralmente, mas tranca rigidamente a altura Y de deitado
        paiCamara.localPosition = new Vector3(
            posCompensada.x,
            alturaInicialCamara - (paiCamara.localRotation * camaraXR.localPosition).y,
            posCompensada.z
        );
    }

    // Inicia a deitagem na maca (chamado pelo evento de clique VR)
    public void IniciarTreino()
    {
        if (!podeIniciar || exameTerminado) return;
        StartCoroutine(Sequencia_Parte1_Treino());
    }

    // Sequência que deita o jogador, move a maca e inicia o relaxamento
    private IEnumerator Sequencia_Parte1_Treino()
    {
        yield return StartCoroutine(Fade(1f));

        yield return null;
        yield return null;

        // Desativa a movimentação e lasers de interação física
        lasers = jogador.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
        DesativarControlos(true);

        // Teleporta o jogador para a maca
        TeleportarJogadorParaPonto(pontoPaciente);

        // Torna o jogador filho da maca para se moverem juntos
        jogador.transform.SetParent(transform);
        BloquearCabeca(true);
        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(Fade(0f));
        
        // Move a maca para dentro do túnel enquanto toca a voz explicativa
        if (vozEntrada != null)
        {
            vozEntrada.Play();
            yield return StartCoroutine(MoverMaca(posicaoInicialCama, pontoDentroTunel.position, vozEntrada.clip.length));
        }

        // Corre o exercício de respiração guiada
        if (esferaLuz != null && fonteVozRobo != null) 
        {
            esferaLuz.SetActive(true);
            yield return StartCoroutine(RotinaRespiracaoModular());
        }

        // Pergunta se o paciente está pronto
        if (vozPerguntaPronto != null)
        {
            yield return new WaitForSeconds(1f);
            vozPerguntaPronto.Play();
            yield return new WaitWhile(() => vozPerguntaPronto.isPlaying);
        }

        Debug.Log("[ControloMaca] Fase de preparacao terminada. A acordar a IA...");
        
        // Se não houver microfone, avança diretamente sem processar IA
        if (!PedidoPermissaoMicrofone.TemPermissao)
        {
            Debug.LogWarning("[ControloMaca] Sem permissao de microfone - a avancar direto para o exame real.");
            IniciarExameReal();
            yield break;
        }

        // Ativa a escuta por IA
        if (gestorDeVoz != null)
            gestorDeVoz.IniciarEscutaAutomatica();
        else
        {
            Debug.LogError("[ControloMaca] Falta associar o GestorIA na Maca!");
            IniciarExameReal(); 
        }
    }

    // Entrada pública para iniciar o exame real (chamada pela IA)
    public void IniciarExameReal()
    {
        if (exameTerminado) return;
        StartCoroutine(Sequencia_Parte2_ExameReal());
    }

    // Sequência que corre o ruído alto da MRI, remove a maca e finaliza o jogo
    private IEnumerator Sequencia_Parte2_ExameReal()
    {
        exameTerminado = true;
        Debug.Log("[ControloMaca] A iniciar exame real!");
        
        // Reproduz o ruído real alto da ressonância magnética
        if (somMRIAlto != null)
        {
            somMRIAlto.volume = 1f;
            somMRIAlto.loop = false;
            somMRIAlto.Play();
            Debug.Log("[ControloMaca] Toca 1a vez! Duracao: " + somMRIAlto.clip.length + "s");
            yield return new WaitForSeconds(somMRIAlto.clip.length);
            somMRIAlto.Play();
            Debug.Log("[ControloMaca] Toca 2a vez!");
            yield return new WaitForSeconds(somMRIAlto.clip.length);
            somMRIAlto.Stop();
        }
        else 
        {
            Debug.LogError("[ControloMaca] somMRIAlto NULL!");
            yield return new WaitForSeconds(15f);
        }

        // Apaga a esfera de luz tridimensional
        yield return StartCoroutine(FadeEsfera(2f));

        // Retira a maca de dentro da máquina
        if (vozSaida != null)
        {
            vozSaida.Play();
            yield return StartCoroutine(MoverMaca(pontoDentroTunel.position, posicaoInicialCama, vozSaida.clip.length));
        }

        // Escurece o ecrã para reposicionar o jogador em pé na sala
        yield return StartCoroutine(Fade(1f));
        yield return null;
        yield return null;

        // Desassocia o jogador da maca e liberta a câmara
        jogador.transform.SetParent(null);
        BloquearCabeca(false);

        // Teleporta o jogador para fora da máquina
        if (pontoSaida != null)
        {
            XROrigin xrOrigin = jogador.GetComponent<XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                Transform rig    = jogador.transform;
                Transform camara = xrOrigin.Camera.transform;

                rig.rotation = Quaternion.Euler(0f, pontoSaida.eulerAngles.y, 0f);

                Vector3 offset = camara.position - rig.position;
                offset.y = 0f;

                // Faz um raycast vertical para alinhar o jogador exatamente com o chão físico da sala
                float alturaChao = pontoSaida.position.y;
                RaycastHit hit;
                if (Physics.Raycast(pontoSaida.position + Vector3.up * 0.2f, Vector3.down, out hit, 2f))
                {
                    alturaChao = hit.point.y;
                }

                rig.position = new Vector3(
                    pontoSaida.position.x - offset.x,
                    alturaChao,
                    pontoSaida.position.z - offset.z
                );
            }
        }

        yield return null;
        yield return null;
        yield return new WaitForFixedUpdate();

        // Reativa os controlos no escuro para esconder ajustes do Character Controller
        DesativarControlos(false);

        yield return null;
        yield return new WaitForFixedUpdate();
        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(Fade(0f));

        yield return new WaitForSeconds(1f);

        // Mostra a placa com a mensagem final
        if (placaFinal != null)
        {
            placaFinal.fala.Play();
            placaFinal.MostrarComFala();
            yield return new WaitWhile(() => placaFinal.fala != null && placaFinal.fala.isPlaying);
        }

        yield return new WaitForSeconds(2f);

        // Escurece o ecrã para terminar o projeto
        tempoFade = 4f;
        yield return StartCoroutine(Fade(1f));
    }

    // Ativa/desativa locomoção, gravidade e lasers do utilizador
    private void DesativarControlos(bool desativar)
    {
        if (lasers != null)
            foreach (var laser in lasers) laser.enabled = !desativar;

        var locomotions = jogador.GetComponentsInChildren
            <UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>();
        foreach (var loco in locomotions) loco.enabled = !desativar;

        CharacterController corpo = jogador.GetComponent<CharacterController>();
        if (corpo != null) corpo.enabled = !desativar;
    }

    // Anima a escala da esfera de respiração para zero
    private IEnumerator FadeEsfera(float duracaoFade)
    {
        if (esferaLuz == null) yield break;
        Vector3 escalaAtual = esferaLuz.transform.localScale;
        float tempo = 0f;
        while (tempo < duracaoFade)
        {
            tempo += Time.deltaTime;
            esferaLuz.transform.localScale = Vector3.Lerp(escalaAtual, Vector3.zero, tempo / duracaoFade);
            yield return null;
        }
        esferaLuz.SetActive(false);
        esferaLuz.transform.localScale = escalaInicialEsfera;
    }

    // Corre a sequência e ciclo de escalas da esfera sincronizada com o áudio
    private IEnumerator RotinaRespiracaoModular()
    {
        Vector3 escalaMin = new Vector3(0.05f, 0.05f, 0.05f);
        Vector3 escalaMax = new Vector3(0.1f, 0.1f, 0.1f);

        foreach (var passo in passosRespiracao)
        {
            if (passo.somVoz != null)
            {
                fonteVozRobo.clip = passo.somVoz;
                fonteVozRobo.Play();

                float duracaoAudio = passo.somVoz.length;
                float t = 0;

                while (t < duracaoAudio)
                {
                    t += Time.deltaTime;
                    float percentagem = t / duracaoAudio;
                    switch (passo.animacaoEsfera)
                    {
                        case TipoAnimacao.Crescer:
                            esferaLuz.transform.localScale = Vector3.Lerp(escalaMin, escalaMax, percentagem);
                            break;
                        case TipoAnimacao.Diminuir:
                            esferaLuz.transform.localScale = Vector3.Lerp(escalaMax, escalaMin, percentagem);
                            break;
                        case TipoAnimacao.ManterTopo:
                            esferaLuz.transform.localScale = escalaMax;
                            break;
                        case TipoAnimacao.Nenhuma:
                        case TipoAnimacao.ManterFundo:
                            esferaLuz.transform.localScale = escalaMin;
                            break;
                    }
                    yield return null;
                }
            }
        }
    }

    // Faz a maca deslizar suavemente
    private IEnumerator MoverMaca(Vector3 inicio, Vector3 fim, float tempo)
    {
        float t = 0;
        while (t < tempo)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(inicio, fim, t / tempo);
            yield return null;
        }
        transform.position = fim;
    }

    // Interpola a opacidade do ecrã de fade preto
    private IEnumerator Fade(float alvoAlpha)
    {
        if (ecraEscuro == null) yield break;
        float tempo = 0;
        Color corInicial = ecraEscuro.color;
        Color corFinal = new Color(0, 0, 0, alvoAlpha);
        while (tempo < tempoFade)
        {
            tempo += Time.deltaTime;
            ecraEscuro.color = Color.Lerp(corInicial, corFinal, tempo / tempoFade);
            yield return null;
        }
        corFinal.a = alvoAlpha;
        ecraEscuro.color = corFinal;
    }

    // Tranca e guarda as referências iniciais da cabeça do utilizador deitado
    private void BloquearCabeca(bool bloquear)
    {
        XROrigin xrOrigin = jogador.GetComponent<XROrigin>();

        if (xrOrigin != null && xrOrigin.Camera != null)
            camaraXR = xrOrigin.Camera.transform;

        if (bloquear && camaraXR != null)
        {
            Vector3 forward = camaraXR.localRotation * Vector3.forward;
            yawInicialMaca = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

            Transform paiCamara = camaraXR.parent;
            if (paiCamara != null)
            {
                posicaoInicialPaiCamara = paiCamara.localPosition;
                alturaInicialCamara = paiCamara.localPosition.y + camaraXR.localPosition.y;
            }
        }
        else if (!bloquear && camaraXR != null)
        {
            Transform paiCamara = camaraXR.parent;
            if (paiCamara != null)
            {
                paiCamara.localRotation = Quaternion.identity;
                paiCamara.localPosition = posicaoInicialPaiCamara;
            }
        }

        limitarCabecaNaMaca = bloquear;
    }

    // Move a câmara física compensando os offsets do utilizador em VR
    private void TeleportarJogadorParaPonto(Transform pontoDestino)
    {
        XROrigin xrOrigin = jogador.GetComponent<XROrigin>();

        Transform rig    = jogador.transform;
        Transform camara = xrOrigin != null && xrOrigin.Camera != null ? xrOrigin.Camera.transform : null;

        if (camara == null)
        {
            jogador.transform.position = pontoDestino.position;
            jogador.transform.rotation = pontoDestino.rotation;
            return;
        }

        Quaternion classRot = camara.rotation;
        Quaternion rotacaodesejada = pontoDestino.rotation;
        Quaternion deltaRotacao = rotacaodesejada * Quaternion.Inverse(classRot);

        rig.rotation = deltaRotacao * rig.rotation;

        Vector3 offsetCamaraRig = camara.position - rig.position;
        rig.position = pontoDestino.position - offsetCamaraRig;
    }
}