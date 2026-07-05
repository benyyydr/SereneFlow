# SereneFlow XR - Manual de Compilação e Organização do Projeto

O **SereneFlow XR** é uma solução imersiva de Realidade Virtual (VR) desenvolvida no motor de jogo **Unity**, concebida especificamente para mitigar os sintomas de ansiedade e claustrofobia em pacientes submetidos a exames de Ressonância Magnética (MRI). 

A aplicação integra um robô assistente virtual (Jammo) cujo comportamento e diálogo se adaptam dinamicamente ao estado emocional do paciente em tempo real. Esta adaptação é coordenada através da integração com o modelo de linguagem multimodal **Gemini 2.5 Flash** (Google AI Studio), analisando a voz gravada do paciente dentro do cenário virtual.

---

## 1. Estrutura Geral do Projeto

O repositório está organizado da seguinte forma:

```text
COPIAPROJETOMRI/
├── Assets/                 # Ficheiros do Unity (Scripts C#, Cenas, Prefabs, Materiais)
│   ├── SCRIPTS/            # Código-fonte C# da lógica da aplicação
│   └── ...                 # Modelos 3D, Áudios, Texturas e Cenas
├── Packages/               # Definições de pacotes e dependências do Unity
├── ProjectSettings/        # Definições de configuração do projeto Unity
└── README.md               # Este manual de instruções
```

---

## 2. Compilação do Projeto Unity (Android)

A aplicação foi otimizada para compilação e exportação em formato Android.

### Requisitos Prévios
*   **Unity Editor:** Versão recomendada: **Unity 2022.3 LTS** (ou superior).
*   **Módulos Adicionais:** É necessário ter o módulo **Android Build Support** instalado através do Unity Hub.
*   **Hardware:** Cabo USB-C de ligação e os óculos de Realidade Virtual.

### Processo de Compilação (Build):

1.  **Importação no Unity Hub:**
    *   Abra o *Unity Hub*, clique em **Add > Add project from disk** e selecione a pasta raiz deste repositório (`COPIAPROJETOMRI`).
    *   Abra o projeto.
2.  **Alteração de Plataforma:**
    *   No menu do topo, vá a **File > Build Settings...**
    *   Selecione a plataforma **Android** e clique em **Switch Platform** (aguarde o reprocessamento de assets).
3.  **Configuração de Cenas:**
    *   Certifique-se de que as cenas da aplicação estão incluídas em *Scenes In Build* na seguinte ordem cronológica:
        1.  `FlorestaScene` (Cena inicial de tutorial, interação e pedido de permissão do microfone)
        2.  `SalaEsperaScene` (Cena intermédia de acolhimento na clínica e música persistente)
        3.  `SalaExameScene` (Cena final da sala de exames, maca e simulação de MRI com IA)
4.  **Configuração da Chave da API Gemini (Importante):**
    *   Para que a funcionalidade de reconhecimento de voz inteligente e diálogo com a Inteligência Artificial funcione, **o utilizador terá de criar e associar a sua própria chave de API**.
    *   Aceda ao portal do **Google AI Studio** (`https://aistudio.google.com/app/apikey`), faça login com uma conta Google e clique no botão **Create API Key** para gerar uma nova chave (gratuita).
    *   No Unity, abra a cena **`SalaExameScene`** e selecione o GameObject chamado **`Servicoia`** na hierarquia da cena.
    *   No painel do *Inspector*, localize o script **`ServicoGemini`**.
    *   Cole a sua chave gerada (que normalmente inicia com `AIzaSy...`) no campo **`Chave API`**.
   
5.  **Compilação e Execução (Build and Run):**
    *   Ligue os óculos de Realidade Virtual ao PC através do cabo USB-C.
    *   Volte a **File > Build Settings...** e clique no botão **Build And Run**.
    *   O Unity irá compilar o projeto, transferir o ficheiro diretamente para os óculos e iniciar a aplicação de forma automática no dispositivo.
---

## 3. Arquitetura de Software e Lógica C#

A pasta `Assets/SCRIPTS/` contém a implementação lógica da experiência em C#. Abaixo detalha-se o papel de cada script:

*   **`ControloMaca.cs`**: O orquestrador central da sala de exames. Gere o fluxo temporal da deitagem do paciente na maca, o movimento de translação para o interior do túnel, o exercício guiado de respiração e o ruído da ressonância. Também implementa a compensação trigonométrica matemática no `LateUpdate` para limitar a rotação da câmara em VR e impedir que o utilizador transpasse a geometria tridimensional do túnel da máquina (prevenindo cinetose).
*   **`Servicoia.cs` (`ServicoGemini`)**: Responsável por converter o áudio recolhido pelo microfone em formato WAV cru (PCM 16-bit, 16kHz) codificado em Base64, e enviá-lo via requisição HTTP assíncrona (`UnityWebRequest`) para a API do Gemini. Utiliza um `responseSchema` estrito no JSON de envio para forçar o Gemini a devolver um formato JSON estruturado com o tema emocional identificado.
*   **`GravadorVoz.cs`**: Implementa a deteção de atividade de voz (VAD) acedendo ao buffer nativo do microfone e calculando a média aritmética da amplitude absoluta das amostras a cada frame. Se o sinal ultrapassar o limiar de silêncio, inicia a gravação; se o silêncio se mantiver durante o tempo limite definido, encerra a captura e acorda a IA.
*   **`MusicaClinica.cs`**: Gere a reprodução da música ambiente e persiste entre a Sala de Espera e a Sala de Exames sem interrupções. Monitoriza dinamicamente todas as fontes de som associadas na cena e aplica um efeito suave de atenuação (*ducking*) no volume da música sempre que o robô está a falar ou o paciente está a falar para o microfone.
*   **`TextoAnimado.cs`**: Algoritmo de escrita de máquina de escrever para a projection de legendas tridimensionais, calculando a velocidade de escrita de forma a coincidir exatamente com a duração útil dos ficheiros de som reproduzidos.
*   **`PainelFala.cs`**: Controla o aspeto visual e transições de opacidade (fade) dos painéis diegéticos de legendas suspensos na sala.
*   **`TeleporteSimples.cs` & `FadeAoIniciar.cs`**: Controlam as transições visuais de escurecimento de ecrã (fade em cor preta) para suavizar a locomoção do utilizador entre posições no espaço virtual, eliminando a cinetose em VR.
*   **`BancoDeFalas.cs`**: Contém as referências às gravações de áudio locais organizadas por categorias emocionais (claustrofobia, medo de barulho, etc.).
*   **`AnimacaoRobo.cs`**: Desencadeia os gestos e expressões corporais no robô assistente Jammo quando este está a emitir som.
*   **`PedidoPermissaoMicrofone.cs`**: Faz o pedido de acesso nativo ao microfone em sistemas Android no arranque da experiência.

