# Projeto de Jogo de Terror

Projeto de desenvolvimento de um jogo com temática de terror, criado na Unity. O repositório contém o projeto Unity e uma cena de mansão em desenvolvimento, usada para construir e testar o ambiente.

## Sobre o projeto

O foco atual inclui a construção do cenário e a exploração em primeira pessoa para testar escala, circulação e colisões do ambiente. O projeto está em desenvolvimento; os recursos e sistemas podem mudar conforme o trabalho avança.

## Tecnologias

- **Unity 6** — versão registrada: `6000.5.8f1`
- **C#** — scripts de gameplay e teste
- **Universal Render Pipeline (URP)** — renderização
- **ProBuilder** — modelagem e prototipagem de ambientes
- **Input System** — entrada de teclado e mouse

## Estrutura do projeto

- `Assets/_Game/Art/` — recursos visuais, materiais e texturas
- `Assets/_Game/Audio/` — organização de recursos de áudio
- `Assets/_Game/Prefabs/` — prefabs de elementos do jogo
- `Assets/_Game/Scenes/Tests/` — cenas de teste, incluindo versões do blockout da mansão
- `Assets/_Game/Scripts/Player/` — controlador de teste em primeira pessoa
- `Packages/` — dependências do projeto Unity
- `ProjectSettings/` — configurações do projeto

## Como abrir o projeto

1. Instale uma versão da Unity compatível com a versão registrada em `ProjectSettings/ProjectVersion.txt`.
2. Clone ou baixe este repositório.
3. Abra o Unity Hub e selecione **Add project from disk**.
4. Selecione a pasta do projeto.
5. Aguarde a Unity importar os arquivos e resolver os pacotes.

> Recomenda-se criar uma cópia ou uma branch antes de fazer alterações importantes.

## Controles do controlador de teste

- **W, A, S, D** — movimentar o personagem
- **Mouse** — controlar a visão
- **Esc** — liberar o cursor
- **Clique esquerdo na janela Game** — capturar o cursor novamente

Esses controles correspondem ao controlador de teste presente no projeto e podem diferir dos controles finais do jogo.

## Estado do projeto

O projeto está em desenvolvimento. Esta documentação descreve a estrutura identificada no repositório e será atualizada conforme novas funcionalidades forem implementadas.

---

Projeto acadêmico/pessoal de desenvolvimento de jogos.
