using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.IO;
using System;
using Newtonsoft.Json;

/// <summary>
/// Trata da comunicação de rede com o modelo Gemini 2.5 Flash na nuvem da Google.
/// Converte as gravações de áudio do paciente em WAV/Base64, envia-as para classificação
/// e processa a resposta JSON estruturada para desencadear a fala correta do robô.
/// </summary>
public class ServicoGemini : MonoBehaviour
{
    [Header("Configurações da API Gemini")]
    [Tooltip("Chave de API do Google AI Studio.")]
    public string chaveAPI = "";

    [Header("Hardware")]
    [Tooltip("Referência ao orquestrador da maca para avançar na sequência clínica.")]
    public ControloMaca camaDoPaciente;
    
    [Tooltip("Componente de áudio (coluna física) do robô Jammo.")]
    public AudioSource colunaDoRobo;

    [Header("Banco de Falas")]
    [Tooltip("Referência ao banco de dados onde estão os áudios locais das falas do robô.")]
    public BancoDeFalas bancoDeFalas;

    [Header("Feedback ao Paciente")]
    [Tooltip("Som de preenchimento (ex: ruído estático de intercomunicador) que toca enquanto a IA processa.")]
    public AudioSource vozEspera;
    
    [Tooltip("Esfera física menor que pulsa no ar para dar feedback visual de que o sistema está a pensar.")]
    public GameObject esferaPulsar;

    [Header("Cérebro do Robô")]
    [TextArea(15, 20)]
    [Tooltip("Instruções de sistema (System Instructions) enviadas ao Gemini para orientar a classificação emocional.")]
    public string regrasDaIA = @"És uma assistente virtual empática de uma clínica de imagiologia.
A tua voz soa através do intercomunicador interno da Ressonância Magnética (MRI).
O paciente JÁ ESTÁ DENTRO do tubo da máquina e a fase magnética está prestes a começar.
O teu objetivo é identificar o estado emocional do paciente e escolher o tema de resposta mais adequado.

TEMAS DISPONÍVEIS:
- medo_barulho: paciente tem medo ou preocupação com o barulho da máquina
- medo_espaco: paciente sente claustrofobia ou desconforto com o espaço apertado
- medo_algo_correr_mal: paciente tem medo que algo corra mal ou que não seja seguro
- precisa_tempo: paciente precisa de mais tempo, está hesitante ou ansioso mas não tem um medo específico
- paciente_pronto: paciente diz claramente que está pronto para começar o exame

LÓGICA DO 'pacientePronto':
- Devolve true APENAS se o tema for 'paciente_pronto'.
- Em todos os outros casos devolve false.";

    // Flags internas de estado
    private bool aProcessar = false;
    private bool aPulsar = false;

    // ==========================================
    // PASSO 1: OUVIR E PENSAR (Gemini)
    // ==========================================
    
    /// <summary>
    /// Envia o áudio gravado em RAM para o Gemini e aguarda a sua classificação JSON.
    /// </summary>
    /// <param name="clipGravado">AudioClip capturado a partir do microfone.</param>
    public IEnumerator TranscreverAudio(AudioClip clipGravado)
    {
        if (aProcessar)
        {
            Debug.LogWarning("Ja a processar.");
            yield break;
        }
        aProcessar = true;
        Debug.Log("TranscreverAudio FOI CHAMADO!");

        // Inicia áudio de espera (estático do intercomunicador)
        if (vozEspera != null) vozEspera.Play();

        // Ativa e anima a esfera de pulsar visual de pensamento
        if (esferaPulsar != null)
        {
            esferaPulsar.SetActive(true);
            StartCoroutine(PulsarEsfera());
        }

        // Converte o áudio nativo do Unity em bytes WAV e depois em formato de texto Base64
        byte[] bytesWav = ConverterParaWav(clipGravado);
        string audioBase64 = Convert.ToBase64String(bytesWav);

        // Sanitize das System Instructions para não quebrar a formatação JSON
        string regrasSanitizadas = regrasDaIA
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "");

        // responseSchema: força estruturalmente o Gemini a devolver SEMPRE { "tema": <enum>, "pacientePronto": bool }
        string jsonEnvio = "{" +
            "\"systemInstruction\":{\"parts\":[{\"text\":\"" + regrasSanitizadas + "\"}]}," +
            "\"contents\":[{\"parts\":[" +
                "{\"inlineData\":{\"mimeType\":\"audio/wav\",\"data\":\"" + audioBase64 + "\"}}" +
            "]}]," +
            "\"generationConfig\":{" +
                "\"responseMimeType\":\"application/json\"," +
                "\"responseSchema\":{" +
                    "\"type\":\"OBJECT\"," +
                    "\"properties\":{" +
                        "\"tema\":{" +
                            "\"type\":\"STRING\"," +
                            "\"enum\":[\"medo_barulho\",\"medo_espaco\",\"medo_algo_correr_mal\",\"precisa_tempo\",\"paciente_pronto\"]" +
                        "}," +
                        "\"pacientePronto\":{\"type\":\"BOOLEAN\"}" +
                    "}," +
                    "\"required\":[\"tema\",\"pacientePronto\"]" +
                "}" +
            "}" +
        "}";

        // URL da API oficial do Gemini 2.5 Flash
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=" + chaveAPI.Trim();

        using (UnityWebRequest carteiro = new UnityWebRequest(url, "POST"))
        {
            // Define o payload JSON a enviar e os cabeçalhos necessários
            carteiro.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonEnvio));
            carteiro.downloadHandler = new DownloadHandlerBuffer();
            carteiro.SetRequestHeader("Content-Type", "application/json");
            carteiro.timeout = 30; // Timeout de rede de 30 segundos

            yield return carteiro.SendWebRequest();

            Debug.Log("HTTP Gemini: " + carteiro.responseCode + " | " + carteiro.result);

            if (carteiro.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Gemini respondeu!");

                // Deserializa o envelope JSON genérico retornado pela API da Google
                RespostaGemini respostaDaNet = JsonConvert.DeserializeObject<RespostaGemini>(carteiro.downloadHandler.text);

                if (respostaDaNet == null || respostaDaNet.candidates == null)
                {
                    Debug.LogError("Resposta nula ou sem candidates!");
                    aProcessar = false;
                    yield break;
                }

                // Limpa marcações markdown de blocos de código que o LLM possa ter incluído
                string textoCru = respostaDaNet.candidates[0].content.parts[0].text;
                textoCru = textoCru.Replace("```json", "").Replace("```", "").Trim();
                Debug.Log("Texto limpo: " + textoCru);

                // Deserializa o nosso JSON interno estruturado pelo responseSchema
                RespostaRoboJson decisao = JsonConvert.DeserializeObject<RespostaRoboJson>(textoCru);

                if (decisao == null || string.IsNullOrEmpty(decisao.tema))
                {
                    Debug.LogError("JSON mal formatado: [" + textoCru + "]");
                    aProcessar = false;
                    yield break;
                }

                Debug.Log("TEMA ESCOLHIDO: " + decisao.tema);
                Debug.Log("pacientePronto: " + decisao.pacientePronto);

                // Passa para a reprodução da fala selecionada
                yield return StartCoroutine(ResponderComFala(decisao.tema, decisao.pacientePronto));

                aProcessar = false;
            }
            else
            {
                Debug.LogError("Erro Gemini " + carteiro.responseCode + ": " + carteiro.error);
                Debug.LogError("Body: " + carteiro.downloadHandler.text);

                // Desliga o efeito de pulsar e oculta a esfera de pensamento
                aPulsar = false;
                if (esferaPulsar != null) esferaPulsar.SetActive(false);

                // Se houver limite de quota (HTTP 429) ou instabilidade de rede (HTTP 503), tenta reescutar após 10 segundos
                if (carteiro.responseCode == 429 || carteiro.responseCode == 503)
                {
                    Debug.LogWarning("Servidor ocupado. A aguardar 10s...");
                    aProcessar = false;
                    yield return new WaitForSeconds(10f);
                    GravadorVoz gravador = FindObjectOfType<GravadorVoz>();
                    if (gravador != null) gravador.IniciarEscutaAutomatica();
                }
                else
                {
                    aProcessar = false;
                    AtivarPlanoDeEmergencia(); // Em erros críticos, avança sem bloquear
                }
            }
        }
    }

    // ==========================================
    // PASSO 2: ESCOLHER E REPRODUZIR A FALA
    // ==========================================
    
    /// <summary>
    /// Escolhe o áudio correspondente no banco de falas locais e toca-o, decidindo depois o rumo do exame.
    /// </summary>
    private IEnumerator ResponderComFala(string tema, bool pacientePronto)
    {
        // Aguarda que o som de estático/espera termine de tocar para não sobrepor vozes
        if (vozEspera != null)
            yield return new WaitWhile(() => vozEspera.isPlaying);

        // Desliga a esfera de pulsar de pensamento
        aPulsar = false;
        if (esferaPulsar != null) esferaPulsar.SetActive(false);

        // Pesquisa no dicionário do Banco de Falas o áudio associado ao tema emocional classificado
        FalaTematica fala = bancoDeFalas != null ? bancoDeFalas.ObterFala(tema) : null;

        if (fala == null)
        {
            Debug.LogError("Fala nao encontrada para o tema: " + tema);
            AtivarPlanoDeEmergencia();
            yield break;
        }

        Debug.Log("A reproduzir fala do tema: " + tema);

        // Reproduz o clipe de áudio correspondente na coluna do robô
        if (fala.audio != null && colunaDoRobo != null)
        {
            colunaDoRobo.clip = fala.audio;
            colunaDoRobo.Play();
            yield return new WaitForSeconds(fala.audio.length + 0.5f); // Aguarda a duração total da fala
        }
        else
        {
            Debug.LogWarning("AudioClip em falta para o tema: " + tema);
            yield return new WaitForSeconds(2f);
        }

        yield return new WaitForSeconds(0.5f);

        // Encaminhamento final com base no veredito
        if (pacientePronto)
        {
            Debug.Log("Paciente pronto! A iniciar exame...");
            if (camaDoPaciente != null) camaDoPaciente.IniciarExameReal();
        }
        else
        {
            // Se o paciente ainda está com receios, volta a ativar a escuta para continuar o diálogo
            Debug.Log("A voltar a escutar...");
            GravadorVoz gravador = FindObjectOfType<GravadorVoz>();
            if (gravador != null) gravador.IniciarEscutaAutomatica();
        }
    }

    // ==========================================
    // PULSAR ESFERA
    // ==========================================
    
    /// <summary>
    /// Anima suavemente a escala da esfera de pulsar enquanto o Gemini processa a resposta de rede.
    /// </summary>
    private IEnumerator PulsarEsfera()
    {
        aPulsar = true;
        Vector3 escalaMin = new Vector3(0.04f, 0.04f, 0.04f);
        Vector3 escalaMax = new Vector3(0.08f, 0.08f, 0.08f);

        while (aPulsar)
        {
            float t = 0;
            // Interpola para maior
            while (t < 0.5f && aPulsar)
            {
                t += Time.deltaTime;
                if (esferaPulsar != null)
                    esferaPulsar.transform.localScale = Vector3.Lerp(escalaMin, escalaMax, t / 0.5f);
                yield return null;
            }
            t = 0;
            // Interpola de volta para menor
            while (t < 0.5f && aPulsar)
            {
                t += Time.deltaTime;
                if (esferaPulsar != null)
                    esferaPulsar.transform.localScale = Vector3.Lerp(escalaMax, escalaMin, t / 0.5f);
                yield return null;
            }
        }

        // Repõe a escala padrão mínima ao desativar
        if (esferaPulsar != null)
            esferaPulsar.transform.localScale = escalaMin;
    }

    // ==========================================
    // AUXILIARES
    // ==========================================
    
    /// <summary>
    /// Fallback de segurança para evitar bloqueio do paciente caso a internet caia ou a API falhe.
    /// Avança automaticamente a maca e corre o exame.
    /// </summary>
    private void AtivarPlanoDeEmergencia()
    {
        Debug.LogWarning("Plano de emergencia ativado!");
        if (camaDoPaciente != null) camaDoPaciente.IniciarExameReal();
    }

    /// <summary>
    /// Converte um AudioClip (dados de float do Unity) num array de bytes formatados em ficheiro WAV de 16-bit PCM.
    /// </summary>
    /// <param name="clip">O áudio gravado em RAM.</param>
    /// <returns>Array de bytes no formato padrão WAV.</returns>
    private byte[] ConverterParaWav(AudioClip clip)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        float[] amostras = new float[clip.samples * clip.channels];
        clip.GetData(amostras, 0);
        Int16[] dadosInt = new Int16[amostras.Length];
        
        // Conversão matemática de amplitudes float (-1f a 1f) para short inteiro de 16 bits (-32767 a 32767)
        for (int i = 0; i < amostras.Length; i++) { dadosInt[i] = (short)(amostras[i] * 32767); }

        // Construção do cabeçalho físico padrão do contentor de ficheiro WAV (RIFF/WAVE)
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dadosInt.Length * 2);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // Tamanho da secção format
        writer.Write((short)1); // Código de formato PCM linear
        writer.Write((short)clip.channels); // Número de canais (mono)
        writer.Write(clip.frequency); // Taxa de amostragem (16000 Hz)
        writer.Write(clip.frequency * clip.channels * 2); // Byte rate
        writer.Write((short)(clip.channels * 2)); // Alinhamento de bloco
        writer.Write((short)16); // Bits por sample (16-bit)
        writer.Write(Encoding.ASCII.GetBytes("data")); 
        writer.Write(dadosInt.Length * 2); // Tamanho total da secção de som
        
        // Grava as amostras PCM de áudio sequencialmente
        foreach (Int16 dado in dadosInt) { writer.Write(dado); }
        
        return stream.ToArray();
    }

    // Estruturas de suporte serializáveis para deserializar a árvore JSON da API do Gemini
    [System.Serializable] public class RespostaGemini { public Candidato[] candidates; }
    [System.Serializable] public class Candidato { public Conteudo content; }
    [System.Serializable] public class Conteudo { public Parte[] parts; }
    [System.Serializable] public class Parte { public string text; }
    
    // Objeto final estruturado retornado pelo Gemini 2.5 Flash via responseSchema
    [System.Serializable] public class RespostaRoboJson { public string tema; public bool pacientePronto; }
}