# White Room

Jogo educacional 3D sobre tecnologias atuais. Unity 6 (6000.2.7f2), URP.

- Design do jogo: [`docs/gdd.html`](docs/gdd.html) (referências visuais em `docs/refs/`)
- Código do projeto: `Assets/_Project/`

## Estado do protótipo

| Marco | Entrega | Estado |
|---|---|---|
| P1 | Modelo de rádio da Missão 1 + testes EditMode | pronto |
| P2 | Mapa e posicionamento dos rádios | — |
| P3 | Simulação da Missão 1 | — |
| P4 | Player e HUD world-space | — |
| P5 | Missão 2 (revisão espaçada) | — |
| P6 | Teste com jogadores | — |

## Modelo de rádio (`WhiteRoom.Radio`)

C# puro (`noEngineReferences`), sem dependência de cena.

- `RadioSpec` / `RadioConfig`: ficha da tecnologia e como o jogador a configurou (antena direcional, fator de espalhamento do LoRa).
- `RadioMath`: taxa do LoRa (`SF · BW/2^SF · CR`), alcance efetivo (SF, antena, prédios), ciclo de trabalho, autonomia.
- `LinkEvaluator`: avalia um enlace e devolve cada verificação com o número exigido, o obtido e a explicação em pt-BR.
- `HeistMission`: os quatro rádios e os três enlaces da Missão 1, fonte única para testes, assets e cena.

Resposta da Missão 1: voz no cofre = BLE, sensores = LoRa SF12, drone = Wi-Fi com antena direcional. O celular funciona onde há cobertura, mas deixa rastro.

## Testes

Pelo Editor: Window › General › Test Runner › EditMode › Run All.

Por linha de comando (exige licença ativa no Unity Hub e o Editor fechado):

```
.un-tests.ps1            # EditMode
.un-tests.ps1 -Platform PlayMode
```

O resultado fica em `TestResults/` (XML no formato NUnit + log da Unity).

### CI

`.github/workflows/tests.yml` roda os testes EditMode a cada push na `main` e em todo PR, com [GameCI](https://game.ci) (`game-ci/unity-test-runner`) na imagem Docker da mesma versão do Editor. O resultado aparece como check no commit/PR e o XML fica como artifact. A pasta `Library/` vai para cache entre execuções.

Secrets necessários no repositório: `UNITY_LICENSE` (conteúdo do `Unity_lic.ulf`), `UNITY_EMAIL` e `UNITY_PASSWORD`.
