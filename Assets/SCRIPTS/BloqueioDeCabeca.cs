using UnityEngine;

public class BloqueioDeCabeca : MonoBehaviour
{
    private Quaternion rotacaoInicial;
    private bool bloqueioAtivo = false;

    public void AtivarBloqueio()
    {
        rotacaoInicial = transform.rotation;
        bloqueioAtivo = true;
        enabled = true;
    }

    public void DesativarBloqueio()
    {
        bloqueioAtivo = false;
        enabled = false;
    }

    void LateUpdate()
    {
        if (bloqueioAtivo)
        {
            // Força a rotação de volta para a posição inicial todos os frames
            transform.rotation = rotacaoInicial;
        }
    }
}