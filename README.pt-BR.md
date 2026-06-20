# Ternary-Logic-Sim 🔺 (.NET / Avalonia)

**🌐 [Português](README.pt-BR.md) · [English](README.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md)** · [📖 Guia maker](docs/GUIA-MAKER.md)

Um simulador de **lógica digital ternária balanceada** (`-1, 0, +1`), feito em **C# puro + Avalonia**,
inspirado no [Digital-Logic-Sim](https://github.com/SebLague/Digital-Logic-Sim) do Sebastian Lague —
mas **sem Unity**: roda como um app desktop .NET multiplataforma (Windows, Linux, macOS).

Enquanto o computador comum pensa em **bits** (0/1), aqui o sinal fundamental é o **trit**, que assume
**três** valores. É um laboratório didático e "maker" para entender como uma máquina ternária funciona.

> **Status:** núcleo de simulação **testado** ✓ · editor visual **funcional** (Fase atual).

---

## 🤔 Ternário balanceado em 30 segundos

| Valor | Símbolo | Cor no app |
|:-----:|:-------:|:-----------|
| **-1** | `N` | 🔴 vermelho |
| **0**  | `O` | ⚫ cinza   |
| **+1** | `P` | 🟢 verde   |

| Binário | Ternário | O que faz |
|:--------|:---------|:----------|
| `NOT`   | `NOT`    | inverte: N↔P, O fica O |
| `AND`   | `MIN`    | o menor dos dois trits |
| `OR`    | `MAX`    | o maior dos dois trits |
| —       | `FULL ADDER` | soma `a + b + carry` em ternário |

A beleza do "balanceado": negar um número é só inverter o sinal de cada trit — **não existe bit de
sinal separado** como no binário.

---

## 📁 Estrutura

```
TernaryLogicSim-NET/
├── TernaryLogicSim.sln
├── src/
│   ├── TernaryLogicSim.Core/        ← NÚCLEO de simulação (C# puro, 0 dependências)
│   │   ├── Trit.cs · TritBus.cs
│   │   ├── TernaryGates.cs · TernaryArithmetic.cs
│   │   ├── BuiltinChip.cs · Circuit.cs
│   └── TernaryLogicSim.App/         ← App Avalonia (editor visual)
│       ├── Program.cs · App.axaml
│       ├── Views/MainWindow.axaml
│       └── Editor/EditorModel.cs · EditorCanvas.cs
├── tests/
│   ├── TernaryLogicSim.Tests/       ← testes xUnit (dotnet test)
│   └── python/validate_core.py      ← validação exaustiva (roda sem .NET)
├── LICENSE · .gitignore · README.md
```

O **núcleo é idêntico** ao que foi escrito e testado antes — C# puro, sem nenhuma dependência de
engine. O app Avalonia apenas o consome.

---

## 🛠️ Como compilar e rodar (passo a passo)

**Pré-requisito:** [.NET SDK](https://dotnet.microsoft.com/download) 8.0 ou superior — testado com **.NET 10** (gratuito). Confira com:

```bash
dotnet --version      # deve mostrar 8.x, 9.x ou 10.x
```

**1. Clonar e restaurar:**
```bash
git clone https://github.com/charlesmmorais/Ternary-Logic-Sim.git
cd Ternary-Logic-Sim
dotnet restore
```

**2. Rodar os testes do núcleo** (recomendado antes de tudo):
```bash
dotnet test
# Esperado: todos os testes verdes (portas, full adder, conversões, somador)
```

**3. Rodar o app visual:**
```bash
dotnet run --project src/TernaryLogicSim.App
```

**4. (Opcional) Publicar um executável standalone:**
```bash
# Windows:
dotnet publish src/TernaryLogicSim.App -c Release -r win-x64 --self-contained
# Linux:
dotnet publish src/TernaryLogicSim.App -c Release -r linux-x64 --self-contained
# macOS (Apple Silicon):
dotnet publish src/TernaryLogicSim.App -c Release -r osx-arm64 --self-contained
```

> Sem .NET? Dá para validar **só a lógica** com Python: `python3 tests/python/validate_core.py`.

---

## 🕹️ Como usar o editor

- **Clique numa porta** na paleta da esquerda → ela aparece no canvas.
- **Arraste o corpo** de um chip para movê-lo.
- **Ligar fios:** clique num **pino de saída** (direita) e depois num **pino de entrada** (esquerda).
- **Entrada de 3 estados:** clique no corpo de um chip **IN** para alternar `N → O → P`.
- **Apagar:** botão direito sobre um chip.
- A simulação roda **ao vivo** e recolore os fios conforme o valor que passa por eles.

O app já abre com um **exemplo de somador** montado para você experimentar.

---

## ✅ Como foi testado

A lógica do núcleo foi validada com **testes exaustivos** (em xUnit e, de forma independente, em
Python): todas as combinações das portas, **as 27 do full adder**, conversão inteiro↔ternário
em ampla faixa, somador ripple multi-trit (milhares de casos), empacotamento `TritBus` e o
avaliador de circuito por ponto fixo.

```bash
dotnet test                          # testes nativos C#
python3 tests/python/validate_core.py  # validação independente
```

---

## 🗺️ Roadmap

- [x] Núcleo de simulação + testes
- [x] App Avalonia: editor visual interativo (colocar, ligar, 3 estados, 3 cores, simulação ao vivo)
- [x] Salvar/carregar circuitos (JSON) e **criar chips compostos** reutilizáveis (incl. aninhados)
- [x] Inversores ternários completos: TINV (STI), NTI, PTI
- [x] Biblioteca de 15 circuitos de exemplo (examples/)
- [ ] Barramentos multi-trit nativos e mais exemplos avançados
- [x] READMEs em 🇧🇷 🇪🇸 🇫🇷 🇮🇹 🇬🇧 + guia "maker" detalhado

---

## 📜 Créditos / NOTICE

Inspirado no [Digital-Logic-Sim](https://github.com/