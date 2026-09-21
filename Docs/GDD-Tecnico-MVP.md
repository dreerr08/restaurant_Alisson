# [Nome Provisório] — GDD Técnico (MVP)

> Documento vivo. Escrito para: **dev solo, nível intermediário, dedicação alta, Unity 2D side-scroller.**
> Objetivo deste doc: chegar a um **MVP jogável do início ao fim** que prove o core loop (ouvir → cozinhar → servir). Tudo que não serve esse loop está marcado como corte.

---

## 0. Estado atual e decisões (atualizado no Sprint 0)

> Esta seção manda sobre o resto do documento quando houver conflito. As seções 1 em diante são o GDD original.

### Stack real

Unity **6000.3.0b4** (beta; migrar para a 6.3 LTS estável é dívida conhecida e adiada), URP 2D, Input System 1.14, Cinemachine 3.1.7, assembly `AliGame.Runtime` e testes em `Assets/_Project/Tests/EditMode`. **Yarn Spinner não é usado.**

### Onde o projeto diverge do GDD original

| Tema | GDD original | Implementado | Por quê |
|---|---|---|---|
| Diálogo | Yarn Spinner | Sistema próprio: `DialogueSO`, `DialogueRunner`, `IDialogueView` (UI trocável), `DialogueTrigger` | Dados em SO e UI modular, sem dependência externa |
| Crafting | Minigame no balcão, com inventário | `RecipeSO` exclusiva por bancada (Fogão, CutStation, MixStation); o resultado nasce no mundo como `ItemPickup` | Cada bancada vira um lugar do restaurante. **Ainda sem minigame** |
| Coleta | Exploração de um mundo | Pickups com física e ímã (`ItemMagnet`) | Base para coleta; o mundo de exploração está congelado |
| Movimento | "Talvez sem pulo" | Pulo com detecção de chão pelo collider | Pedido de design |
| Input | Leitura direta de dispositivos | `GameInput` + `Resources/GameInput.inputactions` (teclado, mouse, gamepad) | Rebind e gamepad sem tocar nos scripts |
| Cena | Boot/Menu + Restaurant | Só `Restaurant` (no Build Settings) | Menu fica para depois |

### Sistemas prontos

Movimento e animação do player, inventário (`ItemSO`, `Inventory`), crafting por bancada, pickups e ímã, aviso de coleta, diálogo com escolhas, NPC que passeia e conversa (Tio Ben), interação por proximidade (`Interactable`), câmera Cinemachine e uma UI própria aconchegante (`UIStyle`).

### Riscos do GDD (seção 6): nenhum foi validado ainda

1. A tensão de atenção é divertida? **Não testado. Falta clientes com paciência.**
2. O minigame de cozinha segura sozinho? **Não existe ainda.**
3. Necessidade emocional ↔ prato é clara? **Não existe ainda (clientes e necessidades).**
4. A caminhada agrega? Movimento pronto, **falta jogar dentro do loop**.
5. O mundo de exploração vale a viagem? **Congelado.**

### Roadmap revisado (substitui a seção 13)

| Sprint | Foco | Saída |
|---|---|---|
| 0 | Higiene: git, cena, Input Actions, asmdef e testes, esta seção | Projeto versionado e testável |
| 1 | **Loop central em graybox**: `CustomerSO` (necessidade, paciência), clientes em mesas, diálogo que revela a necessidade, prato vindo das bancadas, entrega e satisfação, `DayManager` com 1 dia de 3 clientes | Alguém de fora joga um dia e entende o que fazer |
| 2 | Minigame de cozinha (timing) nas bancadas, qualidade do prato pesando na satisfação | O minigame aguenta ser repetido |
| 3 | Playtest com 3 a 5 pessoas e ajuste da paciência e do ritmo | Decisão sobre o vertical slice |

**Congelado até o Sprint 3:** polimento de UI, mais comportamentos de NPC, economia, itens extras, mundo de exploração e migração de versão da Unity.

### Convenções do código

- Namespaces em `AliGame.*` (`Core`, `Data`, `Items`, `Movement`, `Dialogue`, `UI`), scripts em `Assets/_Project/Scripts`.
- Conteúdo em ScriptableObjects (`Assets/_Project/Data`), lógica pura separada de `MonoBehaviour` sempre que possível, para poder ser testada.
- Nenhum script lê dispositivos direto: input passa por `GameInput`.
- A lógica nova entra com testes em `Assets/_Project/Tests/EditMode`.

---

## 1. Pitch (uma frase)

*Jogo de gerenciamento de restaurante 2D onde você cozinha e escuta seus clientes para nutrir o estado emocional deles — não os ingredientes — enquanto divide sua atenção entre quem precisa de você agora.*

O recurso escasso não é comida. É **a sua atenção**.

---

## 2. Core Loop

O núcleo, minuto a minuto:

```
Cliente chega e senta numa mesa
        │
        ▼
Você CAMINHA até a mesa e OUVE (diálogo)  ──► descobre a necessidade emocional
        │
        ▼
Você CAMINHA até a cozinha e COZINHA (minigame)  ──► produz o prato certo (ou errado)
        │                                    │
        │                     (não tem o ingrediente/item?)
        │                                    ▼
        │                    COLETAR o item pelo cenário (custa tempo)
        │                                    │
        ◄────────────────────────────────────┘
        ▼
Você SERVE e CONVERSA  ──► resolve (ou não) a necessidade
        │
        ▼
Recompensa: satisfação emocional + avanço da história daquele cliente
        │
        ▼
Enquanto isso, OUTROS clientes esperam e o humor deles decai ──► tensão de atenção
        │
        └──────────────► novo cliente / fim do dia
```

**Por que o side-scroll serve o loop:** o deslocamento físico (mesa ↔ cozinha ↔ mesa) é o que gera o custo de tempo. Se você para pra ouvir um cliente demoradamente, outro esfria. A caminhada *é* o sistema de gerenciamento. Sem isso, o "andar" viraria enfeite.

**Camada de itens (inventário + coleta):** alguns pratos exigem ingredientes/itens que você **não tem em estoque** e precisa **coletar pelo cenário**. Isso adiciona uma segunda demanda pela sua atenção. Feito certo, o inventário *reforça* o pilar de escassez de atenção; feito errado, vira logística paralela e contradiz o diferencial do jogo (o recurso escasso é atenção, não ingrediente). Regra: **coletar sempre custa tempo ou atenção** — nunca é grátis.

> **Decisão travada — o custo da coleta é a própria exploração.** Buscar ingredientes = explorar um mundo bonito; o custo/recompensa é o tempo e a jornada da exploração em si (modelo *Spiritfarer*), não a pressão contra clientes esfriando. A coleta acontece num **espaço/momento de exploração** separado do salão, então **não** compete em tempo real com o atendimento. Isso desacopla os dois sistemas e deixa a arquitetura mais limpa. Consequência: a qualidade do *mundo de exploração* (arte, ritmo, descoberta) passa a ser um pilar de valor — não é um corredor funcional, é um lugar que dá gosto de percorrer.

Duração de um loop: **~30–90s por cliente.** Duração de um "dia" (sessão): **~5–10 min** no MVP.

---

## 3. Pilares de Design

Toda decisão técnica ou de conteúdo deve reforçar pelo menos um destes:

1. **Escuta > eficiência** — o jogo premia entender a pessoa, não otimizar throughput.
2. **Aconchego com tensão leve** — cozy, mas com pressão de atenção suficiente pra ter decisão.
3. **Legibilidade imediata** — o jogador entende o estado de cada cliente num relance (sem menus pesados).
4. **História emerge do serviço** — a narrativa dos clientes se desenrola por atendê-los ao longo dos dias.

Teste rápido pra qualquer feature nova: *"isso reforça algum pilar? Se não, é corte ou fase 2."*

---

## 4. Referências e diferencial

| Jogo | O que puxar | O que **não** copiar (escopo) |
|---|---|---|
| Coffee Talk | Ritmo de diálogo, clientes recorrentes, preparo como resposta ao pedido | Ele é cena fixa — você é side-scroll, então herda um custo a mais |
| Spiritfarer | Travessia lateral, cuidado emocional dos personagens, aconchego | O mundo/exploração dele é ENORME. Seu MVP é **um restaurante pequeno**, não um mundo |
| A Short Hike / Night in the Woods | Tom de diálogo, uso do Yarn Spinner | Escopo de conteúdo narrativo — comece minúsculo |

**Diferencial:** o "recurso" gerenciado é emocional, e o preparo do prato é uma *resposta* à necessidade emocional revelada no diálogo (não a um pedido literal de comida). Comida certa pra pessoa errada não funciona.

---

## 5. Público, plataforma e escopo

- **Público:** fãs de cozy/narrativos (Coffee Talk, Unpacking, A Short Hike), sessões curtas.
- **Plataforma-alvo:** **PC (Steam), teclado + gamepad.** É o caminho de menor atrito pra side-scroller solo. Mobile fica pra depois (muda controles e UI).
- **Escopo do MVP:** 1 restaurante, 1 cena jogável, ~3 clientes, ~4 pratos, 1 dia jogável de ponta a ponta. Nada de progressão de longo prazo ainda.

---

## 6. Maiores riscos / incertezas (prototipar ISSO primeiro)

Ordenados por "o que mata o jogo se não funcionar":

1. **A tensão de atenção é divertida ou é estressante-ruim?** — o loop de dividir atenção entre clientes que decaem pode frustrar em vez de engajar. *É o maior risco. Prototipe primeiro, feio.*
2. **O minigame de cozinhar segura sozinho?** — se for chato, o jogo inteiro cansa (você repete ele o tempo todo).
3. **Casar "necessidade emocional ↔ prato certo" é claro pro jogador?** — se o jogador não entende por que o prato funcionou/falhou, o sistema vira sorte.
4. **A caminhada agrega ou irrita?** — se andar for lento/tedioso, o side-scroll vira imposto. Precisa de "game feel" bom cedo.
5. **O mundo de exploração vale a viagem?** — como o custo da coleta é a própria exploração, ela precisa ser *gostosa de percorrer* (arte, ritmo, descoberta). Se for um corredor funcional, vira busywork. É um risco de *feel/arte*, não de balanceamento.

> Regra: **prototipe risco antes de conteúdo.** Não produza 20 diálogos antes de saber se o loop é gostoso.

---

# PARTE TÉCNICA

## 7. Stack e configuração do projeto

| Item | Escolha recomendada | Por quê |
|---|---|---|
| **Engine** | Unity **6.3 LTS** | Suporte até dez/2027. (Evitar 6.0 LTS — EOL out/2026.) |
| **Render pipeline** | **URP com 2D Renderer** | Luzes 2D dão o clima aconchegante de um café com custo baixo |
| **Input** | **Input System** (novo) | Suporte a gamepad + teclado sem gambiarra; troca de contexto (andar vs cozinhar) fica limpa |
| **Câmera** | **Cinemachine** | Câmera que segue o player no side-scroll, sem escrever follow na mão |
| **Diálogo** | **Yarn Spinner for Unity 3.2.x** | Diálogo em arquivo de texto (`.yarn`), ramificações e condições prontas, gratuito. Não reinvente isso |
| **Tilemap/arte** | 2D Tilemap, Sprite, 2D Animation (opcional) | Montar o restaurante rápido com tiles; animação de sprite só se precisar |
| **Save** | JSON simples (Unity `JsonUtility` ou Newtonsoft se precisar de dicionários) | MVP não precisa de save robusto; um save/load básico basta |
| **Juice (opcional)** | DOTween (grátis) ou Awaitable/tweens da própria Unity | Feedback de UI e "pop" das interações |

**O que NÃO usar no MVP:** DOTS/ECS, frameworks de DI (Zenject/VContainer), sistemas de save enterprise, addressables. Overkill pra esse escopo — custo de aprendizado sem retorno no loop.

### Estrutura de pastas sugerida

```
Assets/
├── _Project/
│   ├── Art/            (sprites, tiles, UI)
│   ├── Audio/
│   ├── Data/           (ScriptableObjects: clientes, pratos, necessidades)
│   ├── Dialogue/       (arquivos .yarn)
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Scripts/
│   │   ├── Core/       (GameManager, DayManager, EventBus)
│   │   ├── Customers/
│   │   ├── Cooking/
│   │   ├── Movement/
│   │   ├── Dialogue/
│   │   ├── Data/       (definições dos ScriptableObjects)
│   │   └── UI/
│   └── Settings/       (URP assets, Input Actions)
```

O prefixo `_Project/` mantém tudo seu separado de pacotes importados.

---

## 8. Cenas

Para o MVP, **duas cenas** bastam:

1. **Boot/Menu** — tela inicial, botão "Começar dia". (Pode até ser um painel na própria cena de jogo pra começar; mas separar é mais limpo.)
2. **Restaurant** — a cena de gameplay. Um único restaurante lateral com ~2–3 zonas caminháveis: **entrada/salão (mesas)** e **cozinha (balcão)**. Talvez uma zona extra depois (estoque/fundos) — mas **não no MVP**.

Nada de carregamento de cenas complexo. Uma cena de jogo só.

---

## 9. Arquitetura de dados (ScriptableObject-driven)

O coração técnico. Modele conteúdo como dados, não como código, pra você criar cliente/prato novo sem programar.

**`NeedSO` (Necessidade emocional)** — ex: "solidão", "ansiedade", "nostalgia"
- id, nome, descrição, ícone, cor/tag de humor

**`ItemSO` (Item / ingrediente coletável)**
- id, nome, sprite/ícone, descrição curta
- (opcional) empilhável? quantidade máxima
- onde aparece no cenário (tag de spawn) — ou isso fica no prefab do coletável

**`DishSO` (Prato)**
- id, nome, sprite
- `List<NeedSO> satisfaz` — quais necessidades esse prato atende bem
- `List<ItemRequirement> ingredientes` — **novo:** pares (ItemSO, quantidade) necessários pra cozinhar. Se faltar item, o prato fica bloqueado até coletar
- dados do minigame (dificuldade/etapas), se variarem por prato

**`CustomerSO` (Cliente)**
- id, nome, retrato/sprite
- `NeedSO necessidadeAtual` (ou uma lista/sequência por dia da história)
- `string nodeYarnDeAbertura` (qual nó do Yarn tocar ao sentar)
- parâmetros de humor: paciência inicial, taxa de decaimento
- referência a diálogos de reação (prato certo / prato errado)

**`DaySO` (Dia / roteiro)**
- lista ordenada de `CustomerSO` que aparecem
- intervalos de chegada

Com isso, montar um "dia" novo = criar/arrastar assets no Inspector. Zero recompilação.

---

## 10. Sistemas principais

### 10.1 Fluxo de jogo — `DayManager` (máquina de estados)

Estados: `DiaNaoIniciado → EmAndamento → FimDeDia → Resumo`.
- Spawna clientes conforme o `DaySO`.
- Mantém a lista de clientes ativos.
- Detecta condição de fim (todos atendidos ou tempo esgotado).
- **Serve ao loop:** é o relógio que cria pressão. Sem ele, não há "dia", não há tensão.

Implementação: um `enum` de estado + `switch`, ou um mini state machine com classes. Para esse tamanho, **enum + switch é suficiente** — não crie um framework de FSM.

### 10.2 Movimento e câmera — side-scroll hub

- `PlayerController2D`: movimento horizontal simples (talvez sem física de pulo — é um restaurante, não um platformer). `Rigidbody2D` kinematic ou transform direto + colisores pra limites.
- Interação por proximidade: ao chegar perto de uma **mesa** ou do **balcão da cozinha**, aparece prompt "E / botão pra interagir".
- Cinemachine seguindo o player no eixo X, travado no Y.
- **Serve ao loop:** o custo de deslocamento entre pontos é a mecânica de gestão de tempo.
- **Risco a cuidar:** game feel do andar (velocidade, aceleração, som de passos). Prototipe cedo.

### 10.3 Sistema de clientes e necessidades — `CustomerInstance` + `MoodSystem`

O núcleo emocional. Cada cliente na mesa é uma instância runtime que lê de um `CustomerSO`.

- **Estado de humor:** `paciencia` (0–100) que **decai enquanto o cliente é ignorado** e pausa/melhora quando você está interagindo. Visualizável por ícone/cor acima da cabeça (pilar: legibilidade imediata).
- **Necessidade oculta → revelada:** ao ouvir (diálogo), a `necessidadeAtual` do cliente é revelada ao jogador (ícone aparece, ou fica anotado numa UI leve).
- **Resolução:** quando você serve, compara `prato.satisfaz` vs `cliente.necessidadeAtual`:
  - Certo → satisfação alta, diálogo de reação positiva, "história avança".
  - Errado → satisfação baixa, diálogo neutro/negativo.
- Emite eventos (`OnClienteSatisfeito`, `OnClienteFoiEmbora`) pro `DayManager` e pra UI.

> **Decisão de design pra validar jogando (risco #1 e #3):** o decaimento de paciência precisa ser *lento o bastante pra dar tempo de ouvir + cozinhar 1 cliente sem pânico*, mas *rápido o bastante pra punir ignorar*. Isso **só se acerta prototipando** — não tente balancear no papel.

### 10.4 Diálogo — Yarn Spinner

- Escreva as conversas em arquivos `.yarn` (formato tipo roteiro, com ramificações e condições).
- O `DialogueRunner` do Yarn dispara ao interagir com uma mesa; o `CustomerSO` diz qual nó tocar.
- Use **comandos do Yarn** (`<<revelarNecessidade solidao>>`, `<<darHumor triste>>`) pra o script de diálogo mexer no estado do cliente — isso mantém narrativa e sistema conectados sem hardcode.
- MVP: diálogo simples, poucas ramificações. A profundidade narrativa é fase 2.

### 10.5 Minigame de cozinha — `CookingMinigame`

- Dispara ao interagir com o balcão, sabendo qual prato o jogador escolheu preparar.
- **Antes de cozinhar, checa o inventário** (ver 10.6): se faltar `ingredientes` do `DishSO`, o prato aparece bloqueado com o ícone do item que falta → sinaliza "vá coletar".
- **Mantenha SIMPLES e curto** (5–15s): ex. timing (parar uma barra no ponto certo), sequência (apertar na ordem), ou arrastar ingredientes. Escolha **UM** tipo pro MVP.
- Ao cozinhar com sucesso, **consome** os itens do inventário.
- Resultado (perfeito/ok/queimado) modula a satisfação final junto do "prato certo pra necessidade".
- **Serve ao loop:** é a ação ativa mais repetida. **Risco #2:** se for chato, o jogo cansa. Prototipe 1 tipo, teste, só depois decida se varia por prato.

### 10.6 Inventário e coleta — `Inventory` + `WorldItem`

Sistema **propositalmente simples** — nada de grid/Tetris (isso não serve nenhum pilar e come tempo).

- **`Inventory`** (runtime, um por jogador): guarda quantidades por item. Estrutura interna: `Dictionary<ItemSO, int>` ou `List<ItemStack>`. Métodos: `Add(item, qtd)`, `Remove(item, qtd)`, `Has(item, qtd)`, `Count(item)`.
- **`WorldItem`** (MonoBehaviour num prefab no cenário): referencia um `ItemSO`. Ao interagir/entrar em alcance, adiciona ao `Inventory` e some (ou fica em cooldown/reaparece no próximo dia).
- **Onde os itens ficam:** espalhados pelas zonas caminháveis do restaurante (ex.: uma horta nos fundos, uma despensa). Coletar = **caminhar até lá** → é aí que a coleta custa tempo/atenção.
- **UI:** uma barra/HUD leve mostrando o que você tem (pilar: legibilidade imediata). Sem menu pesado. Ícone + contador.
- **Consumo:** o `CookingMinigame` remove os itens ao produzir o prato. Sumidouro claro → inventário não infla.
- **Serve ao loop:** liga a coleta ao ato de cozinhar (não é coleta por coletar). **Cuidado (risco #5):** só agrega se sair pra buscar custar algo. Na opção B (fase de preparo), o custo é "tempo de manhã / o que você deixa de estocar"; na opção A (durante o expediente), é "clientes esfriando".

> **Escopo mínimo de verdade pro MVP:** ~3–4 tipos de item, poucos pontos de coleta, sem stack complexo, sem drop aleatório. Só o suficiente pra criar o ciclo "faltou → busquei → cozinhei".

### 10.7 Save/load — mínimo

- Serializar: dia atual, progresso de história por cliente, conteúdo do inventário, dinheiro/pontuação (se houver). Em JSON, um arquivo.
- MVP pode até salvar só entre dias, não no meio de um dia. Não invista aqui além do necessário.

---

## 11. Comunicação entre sistemas

Para desacoplar (e não virar espaguete solo), use **eventos**:
- C# `event`/`Action` simples, **ou** ScriptableObject Events (um `GameEventSO` com lista de listeners) se quiser conectar via Inspector.
- Ex.: `MoodSystem` avisa `OnClienteFoiEmbora` → `DayManager` remove da lista, `UI` atualiza, `Audio` toca sino.
- **Não** faça cada sistema chamar o outro diretamente por referência dura. Um EventBus leve resolve.

Padrão geral: **dados em ScriptableObjects, estado runtime em MonoBehaviours, comunicação por eventos.** É o suficiente pra esse escopo — resista a abstrair mais.

---

## 12. Escopo do MVP — o corte honesto

| Feature | Classificação | No MVP? |
|---|---|---|
| Core loop (ouvir → cozinhar → servir) | **Essencial** | ✅ |
| Movimento side-scroll + interação por proximidade | **Essencial** | ✅ |
| Sistema de humor/paciência com decaimento | **Essencial** | ✅ |
| Diálogo (Yarn) com revelação de necessidade | **Essencial** | ✅ |
| 1 minigame de cozinha (um tipo só) | **Essencial** | ✅ |
| Match necessidade ↔ prato | **Essencial** | ✅ |
| Inventário simples + coleta de itens explorando o mundo | **Essencial** | ✅ |
| Espaço de exploração pequeno mas bonito (não um corredor) | **Essencial** | ✅ |
| Inventário com stack/grid, drop aleatório | Nice-to-have | ❌ |
| ~3 clientes, ~4 pratos, 1 dia jogável | **Essencial** | ✅ |
| Save entre dias | Reforça | ⚠️ básico |
| Variação de minigame por prato | Reforça | ❌ fase 2 |
| Progressão de longo prazo / upgrades do restaurante | Reforça | ❌ fase 2 |
| Múltiplos dias / arco narrativo dos clientes | Reforça | ❌ fase 2 |
| Economia (dinheiro, comprar ingredientes) | Nice-to-have | ❌ |
| Exploração/mais zonas do restaurante | Nice-to-have | ❌ |
| Trilha adaptativa, luzes 2D elaboradas | Nice-to-have | ❌ (polish depois) |

**Regra pro solo:** se não está na coluna "Essencial", não entra na primeira versão jogável. Ponto.

---

## 13. Roadmap por milestones

Sem datas fixas (você é solo, dedicação alta) — use **critérios de saída**. Reserve **~25–30%** do tempo total pra bugs/integração/polish; integrar sistemas leva mais tempo que implementá-los isolados.

| # | Milestone | O que entra | Critério de saída |
|---|---|---|---|
| 0 | **Protótipos de risco** | Loop feiíssimo: 1 cliente que decai, 1 minigame tosco, servir certo/errado. Sem arte, sem Yarn. | Você consegue responder: *"dividir atenção + cozinhar é divertido?"* Se não, itere aqui antes de tudo |
| 1 | **Prototype (core loop)** | Movimento + interação, `DayManager`, `MoodSystem`, minigame integrado, match necessidade↔prato, cubos e placeholders | Um "dia" jogável do início ao fim com 2–3 clientes, mesmo feio |
| 2 | **Alpha (sistemas completos)** | Yarn integrado, ScriptableObjects de conteúdo, inventário + coleta (fase de preparo), UI de humor e inventário legíveis, save básico, áudio placeholder | Todas as mecânicas do MVP presentes e jogáveis; conteúdo ainda mínimo |
| 3 | **MVP jogável** | ~3 clientes com diálogo real, ~4 pratos, arte mínima coesa, game feel do andar/cozinhar afinado | Alguém que nunca viu o jogo joga o dia inteiro e **entende o loop sem você explicar** |

Depois do MVP: playtest com outras pessoas → decidir o que vira fase 2 (arco narrativo, mais dias, economia).

> **Revise este roadmap a cada milestone concluído**, não só agora. E depois do Milestone 0, se o loop não for divertido, é melhor mudar o loop do que produzir conteúdo em cima de algo que não segura.

---

## 14. Cortes já decididos (guardados pra depois)

- Economia / compra de ingredientes
- Upgrades e customização do restaurante
- Arco narrativo multi-dia dos clientes
- Múltiplos tipos de minigame
- Mais zonas caminháveis / exploração ampliada
- Mobile
- Polish de luz/trilha adaptativa

Nada disso está morto — está **estacionado** até o core loop se provar.

---

## Próximos passos sugeridos

1. **Escolher o tipo do minigame de cozinha** (timing / sequência / arrastar) — quer que eu detalhe as opções com prós e contras técnicos?
2. **Fazer o Milestone 0** antes de qualquer arte.
3. Quando quiser, posso detalhar **um sistema específico em nível de implementação** (ex.: o `MoodSystem` com a lógica de decaimento, ou a integração Yarn ↔ estado do cliente).
