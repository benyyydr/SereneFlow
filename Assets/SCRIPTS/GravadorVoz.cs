using UnityEngine;
using System.Collections;

/// <summary>
/// Controla a captura de voz através do microfone dos óculos.
/// Utiliza um algoritmo simples de volume para detetar quando o paciente começa a falar
/// e termina automaticamente a gravação assim que detetar um silêncio prolongado.
/// </summary>
public class GravadorVoz : MonoBehaviour
{
    [Header("Ligação ao Cérebro")]
    [Tooltip("Referência ao script do serviço Gemini que processará o áudio.")]
    public ServicoGemini servicoIA; 

    [Header("Inteligência do Microfone")]
    [Tooltip("Volume mínimo para o sistema considerar que o utilizador está a falar (ajuste conforme o microfone e o ruído ambiente).")]
    public float limiteSilencio = 0.01f; 
    
    [Tooltip("Quantos segundos o utilizador tem de estar em silêncio contínuo para indicar que terminou a fala.")]
    public float tempoParaCortar = 4.0f; 
    
    [Tooltip("Tempo limite absoluto em segundos para travar a gravação por segurança (caso o ambiente seja muito ruidoso e o silêncio nunca seja atingido).")]
    public int limiteSegurancaSegundos = 30;
    
    [Tooltip("Quantos segundos o sistema aguarda que o utilizador comece a falar antes de desistir e avançar por inatividade.")]
    public float timeoutEsperaVoz = 15f;

    // Variáveis privadas de controlo do microfone
    private string nomeMicrofone;
    private AudioClip clipGravado;
    
    [HideInInspector]
    public bool aGravar = false;

    /// <summary>
    /// Inicialização do script. Deteta e lista os microfones disponíveis no dispositivo.
    /// </summary>
    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            // Escolhe o microfone padrão (geralmente o índice 0 nos óculos VR)
            nomeMicrofone = Microphone.devices[0]; 
            Debug.Log("Microfone detetado: " + nomeMicrofone);

            // Lista todos os microfones disponíveis no console para facilidade de diagnóstico
            for (int i = 0; i < Microphone.devices.Length; i++)
                Debug.Log("Microfone [" + i + "]: " + Microphone.devices[i]);
        }
        else
        {
            Debug.LogError("Nenhum microfone encontrado no sistema!");
        }
    }

    /// <summary>
    /// Inicia publicamente o processo automatizado de escuta.
    /// </summary>
    public void IniciarEscutaAutomatica()
    {
        if (nomeMicrofone == null || aGravar) return;
        StartCoroutine(RotinaGravarComInteligencia());
    }

    /// <summary>
    /// Corrotina que faz a gestão inteligente da gravação da voz em duas fases (deteção de início e fim).
    /// </summary>
    private IEnumerator RotinaGravarComInteligencia()
    {
        aGravar = true;
        // Inicia a gravação no microfone a 16 kHz (frequência ideal recomendada pela Google para a API)
        clipGravado = Microphone.Start(nomeMicrofone, false, limiteSegurancaSegundos, 16000);
        Debug.Log("A escutar... (Podes falar à vontade, vou parar quando te calares)");

        // Aguarda que o microfone arranque fisicamente no sistema operacional
        while (!(Microphone.GetPosition(nomeMicrofone) > 0)) { yield return null; }

        // ─── FASE 1: Aguarda que o utilizador COMECE a falar ───
        Debug.Log("À espera que comeces a falar...");
        bool jaFalou = false;
        float cronometroEspera = 0f;

        while (!jaFalou)
        {
            cronometroEspera += Time.deltaTime;

            // Se o volume ultrapassar o limite, passa à gravação ativa
            if (AnalisarVolume() >= limiteSilencio)
            {
                jaFalou = true;
            }

            // Se atingir o limite de tempo sem que o utilizador diga nada, aborta por inatividade
            if (cronometroEspera >= timeoutEsperaVoz)
            {
                Debug.LogWarning("Nenhuma voz detetada em " + timeoutEsperaVoz + "s. A avançar por segurança...");
                AbandonarGravacao();
                yield break;
            }

            yield return null;
        }

        // ─── FASE 2: Grava ativamente até detetar silêncio prolongado ───
        Debug.Log("Voz detetada! A gravar...");
        float cronometroSilencio = 0f;

        while (aGravar)
        {
            float volumeAtual = AnalisarVolume();

            // Acumula o tempo de silêncio se o volume for menor que o limite
            if (volumeAtual < limiteSilencio)
                cronometroSilencio += Time.deltaTime;
            else
                cronometroSilencio = 0f; // Reinicia o cronómetro se houver som

            // Termina se o silêncio durar tempo suficiente ou se a gravação atingir o limite absoluto
            if (cronometroSilencio >= tempoParaCortar || !Microphone.IsRecording(nomeMicrofone))
                aGravar = false;

            yield return null;
        }

        // ─── GRAVAÇÃO TERMINADA ───
        Microphone.End(nomeMicrofone); // Pára a captação física de áudio
        Debug.Log("Silêncio detetado! Gravação terminada. A enviar para a IA...");
        
        // Envia o clipe gravado em RAM para ser analisado pelo Gemini
        if (servicoIA != null)
            StartCoroutine(servicoIA.TranscreverAudio(clipGravado));
    }

    /// <summary>
    /// Cancela a gravação por falta de atividade e avança diretamente para o exame real.
    /// </summary>
    private void AbandonarGravacao()
    {
        aGravar = false;
        Microphone.End(nomeMicrofone);

        // Fallback: avança para o exame para que o paciente não fique bloqueado na maca
        if (servicoIA != null && servicoIA.camaDoPaciente != null)
            servicoIA.camaDoPaciente.IniciarExameReal();
    }

    /// <summary>
    /// Lê e calcula a média do volume dos últimos 256 frames (samples) gravados no buffer do microfone.
    /// </summary>
    /// <returns>A amplitude absoluta média do áudio atual.</returns>
    private float AnalisarVolume()
    {
        int precisao = 256;
        float[] amostras = new float[precisao];
        
        // Determina a posição final no buffer para ler os dados mais recentes
        int posicaoAtual = Microphone.GetPosition(nomeMicrofone) - precisao + 1;
        if (posicaoAtual < 0) return 0;
        
        // Copia os dados do microfone para a lista temporária de amostras
        clipGravado.GetData(amostras, posicaoAtual);
        
        // Soma as amplitudes absolutas (retirando o sinal negativo de frequência)
        float soma = 0;
        for (int i = 0; i < precisao; i++)
            soma += Mathf.Abs(amostras[i]);
        
        // Devolve a média matemática das amostras
        return soma / precisao;
    }
}