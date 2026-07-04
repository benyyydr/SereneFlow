using UnityEngine;
using TMPro;
using System.Collections;

public class EscritaLegenda : MonoBehaviour
{
    [Header("Texto")]
    [Tooltip("TextMeshPro 3D (sem Canvas) ou TextMeshProUGUI (com Canvas) — liga apenas um dos dois")]
    public TextMeshPro textoMundo;
    public TextMeshProUGUI textoUI;

    [Tooltip("Caracteres por segundo")]
    public float velocidade = 40f;

    private Coroutine corotinaAtiva;

    // ==========================================
    // API PÚBLICA
    // ==========================================

    public void MostrarTexto(string texto)
    {
        if (corotinaAtiva != null) StopCoroutine(corotinaAtiva);
        corotinaAtiva = StartCoroutine(AnimarEscrita(texto));
    }

    public void EsconderTexto()
    {
        if (corotinaAtiva != null) StopCoroutine(corotinaAtiva);
        DefinirTexto("");
    }

    public void MostrarPorTempo(string texto, float segundos)
    {
        if (corotinaAtiva != null) StopCoroutine(corotinaAtiva);
        corotinaAtiva = StartCoroutine(AnimarEEsconder(texto, segundos));
    }

    /// <summary>
    /// Mostra várias frases em sequência. Cada frase aparece (letra a letra),
    /// fica visível por "tempoLeituraPorFrase" segundos, depois o painel limpa
    /// e a próxima frase começa a ser escrita.
    /// </summary>
    public void MostrarSequencia(string[] frases, float tempoLeituraPorFrase = 1.5f)
    {
        if (corotinaAtiva != null) StopCoroutine(corotinaAtiva);
        corotinaAtiva = StartCoroutine(AnimarSequencia(frases, tempoLeituraPorFrase));
    }

    // ==========================================
    // INTERNO
    // ==========================================

    private IEnumerator AnimarEscrita(string texto)
    {
        DefinirTexto("");
        foreach (char letra in texto)
        {
            DefinirTexto(ObterTexto() + letra);
            yield return new WaitForSeconds(1f / velocidade);
        }
    }

    private IEnumerator AnimarEEsconder(string texto, float segundos)
    {
        yield return StartCoroutine(AnimarEscrita(texto));
        yield return new WaitForSeconds(segundos);
        DefinirTexto("");
    }

    private IEnumerator AnimarSequencia(string[] frases, float tempoLeituraPorFrase)
    {
        if (frases == null || frases.Length == 0) yield break;

        foreach (string frase in frases)
        {
            // Limpa o painel antes de escrever a próxima frase
            DefinirTexto("");

            // Escreve letra a letra
            yield return StartCoroutine(AnimarEscrita(frase));

            // Mantém visível para dar tempo de ler
            yield return new WaitForSeconds(tempoLeituraPorFrase);
        }

        DefinirTexto("");
    }

    private void DefinirTexto(string valor)
    {
        if (textoMundo != null) textoMundo.text = valor;
        if (textoUI != null) textoUI.text = valor;
    }

    private string ObterTexto()
    {
        if (textoMundo != null) return textoMundo.text;
        if (textoUI != null) return textoUI.text;
        return "";
    }
}