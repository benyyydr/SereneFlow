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
    public static MusicaClinica Instancia { get; private set; }

    [Header("Volumes")]
    public float volumeNormal = 0.1f;
    public float volumeBaixo  = 0.03f;
    public float velocidadeFade = 2f;

    [Header("Falas a Ouvir (Sala de Espera)")]
    [Tooltip("Arrasta aqui as falas que existem NESTA scene. Atualiza este array " +
             "manualmente quando entrares na SalaExameScene, arrastando as falas de lá.")]
    public AudioSource[] falasParaOuvir;

    [Header("Microfone do Paciente (Sala de Exames)")]
    public GravadorVoz gravadorDoPaciente;

    private AudioSource musica;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        musica = GetComponent<AudioSource>();
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        musica.volume = volumeNormal;
        musica.loop = true;
        musica.Play();
    }

    void Update()
    {
        bool algumSomAtivo = false;

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

        if (gravadorDoPaciente != null && gravadorDoPaciente.aGravar)
            algumSomAtivo = true;

        float volumeAlvo = algumSomAtivo ? volumeBaixo : volumeNormal;
        musica.volume = Mathf.Lerp(musica.volume, volumeAlvo, Time.deltaTime * velocidadeFade);
    }

    /// <summary>
    /// Limpa referências a AudioSources destruídos de cenas anteriores.
    /// </summary>
    public void LimparReferenciasNulas()
    {
        System.Collections.Generic.List<AudioSource> lista = new System.Collections.Generic.List<AudioSource>();
        if (falasParaOuvir != null)
        {
            foreach (AudioSource f in falasParaOuvir)
            {
                if (f != null) lista.Add(f);
            }
        }
        falasParaOuvir = lista.ToArray();
    }

    /// <summary>
    /// Adiciona dinamicamente um AudioSource à lista de monitorização sem apagar as outras referências ativas.
    /// </summary>
    public void AdicionarFala(AudioSource novaFala)
    {
        if (novaFala == null) return;

        System.Collections.Generic.List<AudioSource> lista = new System.Collections.Generic.List<AudioSource>();
        if (falasParaOuvir != null)
        {
            foreach (AudioSource f in falasParaOuvir)
            {
                if (f != null) lista.Add(f);
            }
        }

        if (!lista.Contains(novaFala))
        {
            lista.Add(novaFala);
        }
        falasParaOuvir = lista.ToArray();
    }

    /// <summary>
    /// Adiciona dinamicamente um conjunto de AudioSources à lista de monitorização.
    /// </summary>
    public void AdicionarFalas(AudioSource[] novasFalas)
    {
        if (novasFalas == null) return;
        foreach (AudioSource f in novasFalas)
        {
            AdicionarFala(f);
        }
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