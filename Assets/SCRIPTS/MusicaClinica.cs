using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Música que nasce na SalaEsperaScene e persiste (DontDestroyOnLoad)
/// até à SalaExameScene, sem reiniciar.
/// Coloca este script no mesmo objeto que o AudioSource da música clínica,
/// SÓ na SalaEsperaScene (não o repitas na SalaExameScene).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicaClinica : MonoBehaviour
{
    // Instância única estática (padrão de desenho Singleton) para acesso global
    public static MusicaClinica Instancia { get; private set; }

    [Header("Volumes")]
    [Tooltip("Volume padrão da música clínica quando ninguém está a falar.")]
    public float volumeNormal = 0.1f;
    
    [Tooltip("Volume reduzido (ducking) para quando o robô fala ou o paciente grava voz.")]
    public float volumeBaixo  = 0.03f;
    
    [Tooltip("Velocidade da transição de esvanecimento de volume (fade).")]
    public float velocidadeFade = 2f;

    [Header("Falas a Ouvir (Sala de Espera)")]
    [Tooltip("Arrasta aqui as falas que existem NESTA scene. Atualiza este array manualmente quando entrares na SalaExameScene, arrastando as falas de lá.")]
    public AudioSource[] falasParaOuvir;

    [Header("Microfone do Paciente (Sala de Exames)")]
    [Tooltip("Referência ao gravador de voz para saber quando diminuir a música.")]
    public GravadorVoz gravadorDoPaciente;

    private AudioSource musica;

    /// <summary>
    /// Configuração do Singleton na inicialização.
    /// </summary>
    void Awake()
    {
        // Se já existir uma instância ativa deste script no cenário, destrói-se para evitar duplicados
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        musica = GetComponent<AudioSource>();
        
        // Garante que o objeto de som não seja destruído ao mudar de cena (sala de espera para sala de exame)
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Inicia a reprodução da música de fundo em loop.
    /// </summary>
    void Start()
    {
        musica.volume = volumeNormal;
        musica.loop = true;
        musica.Play();
    }

    /// <summary>
    /// Atualização a cada frame. Diminui o volume da música se houver vozes ativas ou gravação em curso.
    /// </summary>
    void Update()
    {
        bool algumSomAtivo = false;

        // Verifica se alguma fala na cena está a ser reproduzida
        if (falasParaOuvir != null)
        {
            foreach (AudioSource som in falasParaOuvir)
            {
                if (som != null && som.isPlaying)
                {
                    algumSomAtivo = true;
                    break;
                }
            }
        }

        // Verifica se o paciente está ativamente a gravar voz (para não haver ruído de fundo na IA)
        if (gravadorDoPaciente != null && gravadorDoPaciente.aGravar)
            algumSomAtivo = true;

        // Escolhe o volume alvo com base na presença de vozes
        float volumeAlvo = algumSomAtivo ? volumeBaixo : volumeNormal;
        
        // Transição suave de volume (Fade/Lerp) ao longo do tempo
        musica.volume = Mathf.Lerp(musica.volume, volumeAlvo, Time.deltaTime * velocidadeFade);
    }

    /// <summary>
    /// Chama isto manualmente (de outro script) quando entrares na SalaExameScene,
    /// para registar as falas/sons dessa nova scene.
    /// </summary>
    public void RegistarSonsDaScene(AudioSource[] novasFalas, GravadorVoz novoGravador = null)
    {
        falasParaOuvir = novasFalas;
        if (novoGravador != null)
            gravadorDoPaciente = novoGravador;
    }

    /// <summary>
    /// Para a música completamente — chama isto se algum dia precisares
    /// de silenciar a clínica (ex: fim do exame).
    /// </summary>
    public void PararMusica()
    {
        if (musica != null) musica.Stop();
    }
}