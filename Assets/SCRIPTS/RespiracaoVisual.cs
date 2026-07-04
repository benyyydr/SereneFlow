using UnityEngine;

/// <summary>
/// Controla o comportamento visual cíclico (pulsação de escala) da esfera de respiração.
/// Utiliza uma interpolação linear (Lerp) combinada com um cronómetro local para animar
/// a esfera entre dimensões máximas e mínimas, reiniciando o ciclo sempre que o objeto é ativado.
/// </summary>
public class RespiracaoVisual : MonoBehaviour
{
    [Header("Escala da Esfera")]
    [Tooltip("Dimensões mínimas da esfera (estado de exalação completa).")]
    public Vector3 tamanhoMinimo = new Vector3(0.5f, 0.5f, 0.5f);
    
    [Tooltip("Dimensões máximas da esfera (estado de inalação completa).")]
    public Vector3 tamanhoMaximo = new Vector3(1.5f, 1.5f, 1.5f);
    
    [Header("Tempos do Ciclo")]
    [Tooltip("Tempo em segundos que demora cada etapa de expansão ou contração do ciclo (ex: 4 segundos).")]
    public float tempoCiclo = 4f; 
    
    // Cronómetro local independente do tempo global da cena
    private float cronometroLocal = 0f;

    /// <summary>
    /// Evento chamado automaticamente pelo Unity sempre que o objeto é ativado (SetActive(true)).
    /// Garante que o ciclo de respiração começa sempre do início e com o tamanho correto.
    /// </summary>
    void OnEnable()
    {
        cronometroLocal = 0f; // Reinicia o relógio local para o segundo zero
        transform.localScale = tamanhoMinimo; // Garante que a animação começa na dimensão mínima
    }

    /// <summary>
    /// Atualiza a escala da esfera frame a frame de forma suave.
    /// </summary>
    void Update()
    {
        // Acumula o tempo decorrido desde o último frame
        cronometroLocal += Time.deltaTime;
        
        // Calcula o fator de interpolação t (de 0 a 1 e de volta a 0) usando a função matemática PingPong
        // baseada no nosso cronómetro local, evitando saltos bruscos no ciclo visual
        float t = Mathf.PingPong(cronometroLocal, tempoCiclo) / tempoCiclo;
        
        // Aplica a escala interpolada suavemente de acordo com a percentagem calculada
        transform.localScale = Vector3.Lerp(tamanhoMinimo, tamanhoMaximo, t);
    }
}