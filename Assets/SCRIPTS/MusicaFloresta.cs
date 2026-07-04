using UnityEngine;

/// <summary>
/// Música exclusiva da FlorestaScene. NÃO tem DontDestroyOnLoad —
/// quando a scene muda, este objeto e a música morrem com ela automaticamente.
/// Coloca este script no mesmo objeto que o AudioSource da música da floresta.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicaFloresta : MonoBehaviour
{
    [Header("Volumes")]
    [Tooltip("Volume padrão da música de natureza quando ninguém está a falar.")]
    public float volumeNormal = 0.1f;
    
    [Tooltip("Volume reduzido (ducking) da música quando as instruções do robô estão a tocar.")]
    public float volumeBaixo  = 0.03f;
    
    [Tooltip("Velocidade de transição para suavizar as variações de volume (fade).")]
    public float velocidadeFade = 2f;

    [Header("Falas a Ouvir")]
    [Tooltip("Arrasta aqui a(s) fala(s) do tutorial — quando tocam, a música baixa.")]
    public AudioSource[] falasParaOuvir;

    private AudioSource musica;

    /// <summary>
    /// Configuração na inicialização. Obtém a referência da fonte de áudio.
    /// </summary>
    void Awake()
    {
        musica = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Inicia a reprodução da música da floresta em loop.
    /// </summary>
    void Start()
    {
        musica.volume = volumeNormal;
        musica.loop = true;
        musica.Play();
    }

    /// <summary>
    /// Atualização a cada frame. Suaviza o volume com base na reprodução das falas.
    /// </summary>
    void Update()
    {
        bool algumaFalaAtiva = false;

        // Verifica se alguma fala do tutorial está ativamente a tocar
        if (falasParaOuvir != null)
        {
            foreach (AudioSource fala in falasParaOuvir)
            {
                if (fala != null && fala.isPlaying)
                {
                    algumaFalaAtiva = true;
                    break;
                }
            }
        }

        // Escolhe o volume alvo dependendo se o robô está a falar ou calado
        float volumeAlvo = algumaFalaAtiva ? volumeBaixo : volumeNormal;
        
        // Transição suave de volume (Fade/Lerp) ao longo do tempo
        musica.volume = Mathf.Lerp(musica.volume, volumeAlvo, Time.deltaTime * velocidadeFade);
    }
}