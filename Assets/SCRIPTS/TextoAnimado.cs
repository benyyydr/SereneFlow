using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Executa um efeito de maquina de escrever dinâmico nas legendas.
/// Divide o texto em frases, calcula a velocidade ideal de escrita de forma a coincidir
/// exatamente com o tempo de reprodução do áudio e limpa o ecrã caso o texto exceda as linhas permitidas.
/// </summary>
public class TextoAnimado : MonoBehaviour
{
    [Header("Texto")]
    [TextArea(5, 20)]
    [Tooltip("O texto completo da fala do robô assistente que será animado.")]
    public string textoCompleto;

    [Header("Configuração")]
    [Tooltip("Velocidade mínima de caracteres por segundo.")]
    public float velocidadeMinima = 15f;
    
    [Tooltip("Velocidade máxima de caracteres por segundo.")]
    public float velocidadeMaxima = 60f;
    
    [Tooltip("Tempo de pausa em segundos no final de cada frase e nas limpezas de ecrã.")]
    public float pausaEntreFrases = 0.4f;

    [Tooltip("Número máximo de linhas antes de limpar o painel.")]
    public int maxLinhas = 3;

    private TMP_Text textoTMP;
    private Coroutine rotinaAtual;

    /// <summary>
    /// Configuração na inicialização. Obtém o componente TextMeshPro.
    /// </summary>
    void Awake()
    {
        textoTMP = GetComponent<TMP_Text>();

        if (textoTMP != null)
            textoTMP.text = "";
    }

    /// <summary>
    /// Inicia a animação de escrita sincronizada com uma fonte de áudio.
    /// </summary>
    /// <param name="audio">O AudioSource correspondente à fala.</param>
    public void Iniciar(AudioSource audio)
    {
        if (rotinaAtual != null)
            StopCoroutine(rotinaAtual);

        rotinaAtual = StartCoroutine(EscreverTexto(audio));
    }

    /// <summary>
    /// Para imediatamente a animação de escrita e limpa o texto exibido.
    /// </summary>
    public void Parar()
    {
        if (rotinaAtual != null)
            StopCoroutine(rotinaAtual);

        textoTMP.text = "";
    }

    /// <summary>
    /// Corrotina principal que divide o texto, calcula velocidades e digita as letras frame a frame.
    /// </summary>
    private IEnumerator EscreverTexto(AudioSource audio)
    {
        textoTMP.text = "";

        // Divide o texto completo em frases com base nos sinais de pontuação
        List<string> frases = DividirEmFrases(textoCompleto);

        // Calcula a velocidade necessária para que a escrita termine ao mesmo tempo que o som
        float velocidade = CalcularVelocidade(audio, frases);

        foreach (string frase in frases)
        {
            // Se a frase não couber no limite de linhas restante do painel, faz uma pausa e limpa-o
            if (!CabeNoPainel(frase))
            {
                yield return new WaitForSeconds(pausaEntreFrases);
                textoTMP.text = "";
            }

            string prefixo = textoTMP.text;

            // Adiciona uma quebra de linha se já houver texto escrito no painel
            if (!string.IsNullOrEmpty(prefixo))
                prefixo += "\n";

            float tempoTotalFrase = frase.Length / velocidade;
            float tempoDecorrido = 0f;

            // Efeito máquina de escrever frame a frame utilizando Time.deltaTime para maior precisão e fluidez
            while (tempoDecorrido < tempoTotalFrase)
            {
                tempoDecorrido += Time.deltaTime;
                int caracteresAMostrar = Mathf.Min(frase.Length, Mathf.FloorToInt(tempoDecorrido * velocidade));
                textoTMP.text = prefixo + frase.Substring(0, caracteresAMostrar);
                yield return null; 
            }

            // Garante que a frase é exibida por completo no final do ciclo de escrita
            textoTMP.text = prefixo + frase; 

            // Pausa entre as frases para simular respiração e leitura confortável
            yield return new WaitForSeconds(pausaEntreFrases);
        }
    }

    /// <summary>
    /// Divide o texto de entrada num array de frases limpas utilizando expressões regulares (regex).
    /// </summary>
    private List<string> DividirEmFrases(string texto)
    {
        List<string> resultado = new List<string>();

        // Divide o texto nos limites onde ocorrem pontos finais, de interrogação ou exclamação
        string[] partes = Regex.Split(texto.Trim(), @"(?<=[.!?])\s+");

        foreach (string parte in partes)
        {
            string limpa = parte.Trim();

            if (!string.IsNullOrEmpty(limpa))
                resultado.Add(limpa);
        }

        return resultado;
    }

    /// <summary>
    /// Calcula a velocidade ideal de digitação subtraindo o tempo gasto com pausas do tempo total do áudio.
    /// </summary>
    private float CalcularVelocidade(AudioSource audio, List<string> frases)
    {
        if (audio != null && audio.clip != null)
        {
            float duracao = audio.clip.length;

            if (duracao > 0f)
            {
                // Conta o número de pausas inerentes ao final de cada frase
                int numPausas = frases.Count;

                // Simula o texto corrido para estimar quantas quebras de ecrã vão ocorrer
                int numLimpezas = 0;
                string acumulado = "";
                foreach (string frase in frases)
                {
                    if (acumulado.Length > 0)
                    {
                        if (!SimularCabeNoPainel(acumulado, frase))
                        {
                            numLimpezas++;
                            acumulado = frase;
                        }
                        else
                        {
                            acumulado += "\n" + frase;
                        }
                    }
                    else
                    {
                        acumulado = frase;
                    }
                }

                // Subtrai o tempo total ocupado pelas pausas para obter a duração útil real da escrita
                float tempoPausas = (numPausas + numLimpezas) * pausaEntreFrases;
                float tempoEscrita = duracao - tempoPausas;

                // Garante por segurança que o tempo de escrita é pelo menos 30% do áudio (evitando divisão por zero)
                if (tempoEscrita < duracao * 0.3f)
                {
                    tempoEscrita = duracao * 0.3f;
                }

                float velocidade = textoCompleto.Length / tempoEscrita;

                // Limita a velocidade calculada entre os extremos configurados
                return Mathf.Clamp(
                    velocidade,
                    velocidadeMinima,
                    velocidadeMaxima
                );
            }
        }

        return velocidadeMinima;
    }

    /// <summary>
    /// Executa uma verificação simulada e temporária no TextMeshPro para saber se o texto acumulado cabe no painel.
    /// </summary>
    private bool SimularCabeNoPainel(string textoAcumulado, string novaFrase)
    {
        if (textoTMP == null) return true;
        string textoOriginal = textoTMP.text;
        string teste = textoAcumulado + "\n" + novaFrase;

        textoTMP.text = teste;
        textoTMP.ForceMeshUpdate(); // Força a atualização do motor de texto para calcular a geometria

        int linhas = textoTMP.textInfo.lineCount;

        // Restaura o texto original para não alterar a exibição real do painel
        textoTMP.text = textoOriginal;
        textoTMP.ForceMeshUpdate();

        return linhas <= maxLinhas;
    }

    /// <summary>
    /// Verifica se a nova frase ultrapassa o limite de linhas permitido, considerando o texto já escrito no painel.
    /// </summary>
    private bool CabeNoPainel(string novaFrase)
    {
        if (textoTMP.text.Length == 0) return true;

        string textoOriginal = textoTMP.text;
        string teste = textoOriginal + "\n" + novaFrase;

        textoTMP.text = teste;
        textoTMP.ForceMeshUpdate();

        int linhas = textoTMP.textInfo.lineCount;

        textoTMP.text = textoOriginal;
        textoTMP.ForceMeshUpdate();

        return linhas <= maxLinhas;
    }
}