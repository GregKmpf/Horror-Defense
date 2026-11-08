# 🧟 Horror-Defense

Um jogo de **Tower Defense** focado em uma atmosfera de **Horror**, desenvolvido na Unity. Enfrente ondas de inimigos aterrorizantes construindo e gerenciando suas defesas.

## 🛠️ Tecnologias
* **Engine:** Unity
* **Linguagem:** C#

## 👥 Integrantes
* **Gregori Yago Kempf** (GregKmpf/204034) - Dev
* **Arthur Benedetti** (NyuuDev) - Dev
* **Guilherme Grosselli** (grosselliguilherme) - Dev

## 🚀 Como Executar o Projeto

1. Clone o repositório:
   ```bash
   git clone https://github.com/GregKmpf/Horror-Defense.git
   ```

## 📦 Entrega

* **Executável do jogo:** _link pendente_
* **Vídeo demonstrativo:** _link pendente_

## 📋 Requisitos do Trabalho

Requisitos do Trabalho Prático de CCC300 – Computação Gráfica e Realidade Virtual (2026/2), com a situação de cada um em **08/11/2026**.

| Legenda | Significado |
|---|---|
| ✅ | Feito e já está em `main` |
| 🔀 | Feito, mas aguardando o merge de um PR aberto |
| 🟡 | Parcial: existe, mas não atende ao requisito por completo |
| ❌ | Não iniciado |

### Resumo da situação

* **Feito ou em PR:** cenário, base, caminho dos inimigos, seis tipos de torre, projéteis com detecção de colisão e efeitos visuais.
* **Não iniciado:** ondas, níveis de dificuldade, menu, ranking, vitória, tempo máximo, sons, build e vídeo.
* **Atenção:** as torres e os projéteis estão no PR [#6][pr6], ainda sem merge. Em `main` ainda não há torres.

### Jogo e menu

| Requisito | Status | Observação |
|---|---|---|
| Tower Defense 2D ou 3D na Unity, em trio | ✅ | Jogo 3D |
| Plataforma PC ou mobile (livre) | ❌ | Falta o grupo decidir e gerar a build |
| Menu para iniciar o jogo | ❌ | |
| Escolha do nível de dificuldade pelo menu | ❌ | |
| Ranking simples, mostrado no menu | ❌ | |
| Informações sobre o jogo no menu | ❌ | Consta nos critérios de avaliação |

### Níveis de dificuldade e ondas

| Requisito | Status | Observação |
|---|---|---|
| Uma ou mais ondas de inimigos | ❌ | Não há gerenciador de ondas, a cena tem só o `Mimic (test)` |
| Pelo menos 2 níveis de dificuldade | ❌ | Devem variar quantidade e/ou características de inimigos e ondas |
| Pelo menos 5 inimigos no nível mais simples | ❌ | Depende do gerenciador de ondas |

### Ambiente e área de jogo

| Requisito | Status | Observação |
|---|---|---|
| Área de jogo limitada | ✅ | Fazenda Abandonada: [#1][pr1], restaurada no [#5][pr5] |
| Base a ser defendida | ✅ | `Objetivo`, com 100 de vida: [#5][pr5] |
| Um ou mais pontos de entrada | ✅ | Um ponto, o início do `Caminho`: [#5][pr5]. Ele começa no rio e precisa de ajuste |
| Caminho até a base | ✅ | `EnemyPath` com 5 waypoints: [#2][pr2], [#5][pr5]. A rota passa por dentro do celeiro |
| Posições para as estruturas defensivas | 🔀 | 4 `TurretSlot` na cena: [#6][pr6] |
| Cenário com pelo menos o tamanho de 30 inimigos enfileirados | ✅ | Rota de 85 m e corpo do Mimic com cerca de 1 m (collider). As pernas o deixam bem mais largo, vale confirmar a escala visualmente |

### Estruturas defensivas

| Requisito | Status | Observação |
|---|---|---|
| Pelo menos 2 tipos com comportamentos distintos | 🔀 | 6 tipos: Cannon, Rocket Pod, Artillery, Minigun, Heavy e Shock Generator: [#3][pr3], [#6][pr6] |
| Detectar inimigos por alcance | 🔀 | [#3][pr3], [#6][pr6] |
| Selecionar um alvo | 🔀 | Prioridades First, Last, Closest e Strongest: [#3][pr3], [#6][pr6] |
| Atacar, causar dano e aguardar um intervalo | 🔀 | [#3][pr3], [#6][pr6] |
| Jogador posiciona ou ativa as estruturas durante o jogo | 🟡 | `TurretSlot.Build()` existe ([#6][pr6]), mas falta a interface. Hoje só há uma torre fixa de teste no `TurretSlot 1` |

### Inimigos

| Requisito | Status | Observação |
|---|---|---|
| Seguir um caminho predefinido até a base | ✅ | `EnemyMovement`: [#2][pr2], configurado na cena pelo [#5][pr5] |
| Ter vida e receber dano | ✅ | `Enemy`: [#2][pr2] |
| Ser removido com vida zero | ✅ | `Enemy`: [#2][pr2] |
| Causar consequência ao chegar à base | ✅ | Tira vida do `Objetivo`: [#5][pr5] |
| Quantidade e características variam com a dificuldade | ❌ | Depende do gerenciador de ondas |
| Pelo menos 2 comportamentos distintos (recomendado) | 🟡 | Só existe o Mimic. O modelo `Enemy_Base` já facilita criar outros |

### Ataques e projéteis

A torre só mira e dispara. O dano vem do projétil (`Assets/Scripts/Turrets/Projectile.cs`), e só quando ele colide com o inimigo. Detalhes em `Assets/Turrets/README.md`.

| Requisito | Status | Observação |
|---|---|---|
| Projétil criado a partir da estrutura | 🔀 | Cada disparo cria um projétil no cano da torre, um por alvo. Cinco tipos: Shell, Bullet, Heavy Shell, Rocket e Bolt: [#6][pr6] |
| Projétil se desloca até o alvo | 🔀 | Segue o alvo enquanto ele vive. Se o alvo morrer antes, continua em linha reta: [#6][pr6] |
| Projétil detecta colisão com o inimigo | 🔀 | `OnTriggerEnter` entre o collider do projétil e o `SphereCollider` do inimigo. A camada `Projectile` só colide com a camada `Enemy`: [#6][pr6] |
| Projétil causa o dano | 🔀 | Só no impacto. O dano em área usa `Physics.OverlapSphere` na camada `Enemy`: [#6][pr6] |
| Projétil é destruído ao acertar ou sair da área válida | 🔀 | Ao acertar, ou ao se afastar mais de 1,25 × o alcance da torre (ou após 4 s): [#6][pr6] |

### Detecção de colisão

| Requisito | Status | Observação |
|---|---|---|
| Impacto entre projéteis e inimigos | 🔀 | `Projectile.OnTriggerEnter`: [#6][pr6] |
| Chegada dos inimigos à base | 🟡 | Funciona pelo fim do caminho, sem colisão. O [#5][pr5] removeu o trigger que o `Objetivo` tinha, então será preciso recolocá-lo |
| Destruição ou remoção dos inimigos | 🔀 | Por vida zero, e o dano agora só vem do impacto do projétil: [#6][pr6] |

### Vitória e derrota

| Requisito | Status | Observação |
|---|---|---|
| Vitória ao resistir às ondas ou cumprir a missão | ❌ | |
| Derrota quando a vida da base chega a zero | 🟡 | Dispara o evento `aoSerDestruido` e um log, mas o jogo não termina: [#5][pr5] |
| Tempo máximo para a missão, com derrota ao esgotar | ❌ | Obrigatório, pode variar com a dificuldade |

### Efeitos e som

| Requisito | Status | Observação |
|---|---|---|
| Efeitos visuais | 🔀 | Clarão, projéteis com rastro, impactos, explosões e pulso: [#3][pr3], [#6][pr6] |
| Recursos sonoros | ❌ | Não há áudio no projeto, e os sons contam na nota |

### Repositório e apresentação

| Requisito | Status | Observação |
|---|---|---|
| Repositório no GitHub compartilhado com `rafaelrieder` | ✅ | Eliminatório |
| Commits de todos os integrantes | ✅ | Os três já têm commits |
| Somente código-fonte e dependências pertinentes | 🟡 | Remover `Assets.tar.gz`, que está versionado |
| Link do executável no README | ❌ | |
| Link do vídeo demonstrativo no README | ❌ | |
| Apresentação de 10 min + até 5 min de perguntas | ❌ | Exige domínio do projeto por todo o trio |

## 🎯 Critérios de Avaliação

| Critério | Pontos | Situação |
|---|---:|---|
| Menu principal, com níveis de dificuldade, ranking e informações | 0,5 | ❌ |
| Modelagem das estruturas, dos inimigos e do cenário: quantidade e variedade | 1,0 | 🟡 Boa variedade de torres, só um inimigo |
| Movimentação dos inimigos, comportamento das estruturas e projéteis | 2,5 | 🔀 Projéteis no [#6][pr6] |
| Interatividade: controle, velocidade, gerenciamento de alvos e colisões | 2,5 | 🟡 Falta a interação do jogador. Colisões dos projéteis no [#6][pr6] |
| Efeitos visuais e recursos sonoros | 1,0 | 🟡 Faltam os sons |
| Demo em vídeo | 0,5 | ❌ |
| **Individual:** contribuições no GitHub e domínio na apresentação | 2,0 | Avaliado por integrante |
| **Total** | **10,0** | |
| **Extra:** suporte a Realidade Virtual ou Aumentada | +1,0 | Opcional, não iniciado |

## 📅 Datas

| Data | Etapa |
|---|---|
| 24/08/2026 | Definição do trio |
| 13/10/2026 | Sprint |
| 03/11/2026 | Sprint (tempo de aula) |
| 10/11/2026 | Sprint (tempo de aula) |
| 17/11/2026 | Sprint (tempo de aula) |
| 24/11/2026 | **Apresentação final** |

## 🔗 Pull Requests

| PR | Conteúdo | Situação |
|---|---|---|
| [#1][pr1] | Fazenda Abandonada na cena | Em `main` |
| [#2][pr2] | Sistema de inimigos e o Mimic | Em `main`. A configuração da cena, perdida em um merge, voltou com o [#5][pr5] |
| [#3][pr3] | Sistema de torres | Entrou só em `feat/turrets`. Chega a `main` pelo [#6][pr6] |
| [#4][pr4] | Câmera: inclinação, zoom e limites | Em `main`. Por enquanto só está na cena `CameraTests`, falta levar para a cena principal |
| [#5][pr5] | Restaura a cena e o sistema de inimigos | Em `main` |
| [#6][pr6] | Torres na Fazenda Abandonada, com projéteis e detecção de colisão | Aberto, já atualizado com a `main` |

## 💡 Extras Planejados

Ideias do grupo que não são exigidas pelo trabalho:

- [ ] Upgrade e venda de torres
- [ ] Economia: recursos ganhos ao derrotar monstros (`cost` e `bounty` já existem nos dados, falta o sistema)
- [ ] Pós-processamento com iluminação sombria, vinheta e névoa
- [ ] Animações de ataque, movimentação e destruição
- [ ] Tela de pause
- [ ] HUD com vida da base, recursos, número da onda e seleção de torres
- [ ] Telas de vitória e game over com opção de reiniciar
- [ ] Balanceamento do dano das torres contra a vida e a velocidade dos inimigos
- [ ] Otimização para manter o FPS em ondas grandes

**Em aberto:** gerar build para Linux, Windows ou ambos? O trabalho deixa a plataforma livre, e a Unity gera uma build separada para cada sistema.

[pr1]: https://github.com/GregKmpf/Horror-Defense/pull/1
[pr2]: https://github.com/GregKmpf/Horror-Defense/pull/2
[pr3]: https://github.com/GregKmpf/Horror-Defense/pull/3
[pr4]: https://github.com/GregKmpf/Horror-Defense/pull/4
[pr5]: https://github.com/GregKmpf/Horror-Defense/pull/5
[pr6]: https://github.com/GregKmpf/Horror-Defense/pull/6
