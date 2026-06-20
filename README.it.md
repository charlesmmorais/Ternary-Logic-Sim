# Ternary-Logic-Sim 🔺 (.NET / Avalonia)

**🌐 [Português](README.pt-BR.md) · [English](README.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md)** · [📖 Guida maker](docs/GUIA-MAKER.md)

Un simulatore di **logica digitale ternaria bilanciata** (`-1, 0, +1`), scritto in **C# puro + Avalonia** —
ispirato al [Digital-Logic-Sim](https://github.com/SebLague/Digital-Logic-Sim) di Sebastian Lague,
ma **senza Unity**: gira come app desktop .NET multipiattaforma (Windows, Linux, macOS).

Mentre un computer comune pensa in **bit** (0/1), qui il segnale fondamentale è il **trit**,
che assume **tre** valori. È un laboratorio didattico e "maker" per capire come funziona una macchina ternaria.

> **Stato:** core di simulazione **testato** ✓ · editor visuale **funzionante** (salva/apri + chip composti).

---

## 🤔 Ternario bilanciato in 30 secondi

| Valore | Simbolo | Colore nell'app |
|:------:|:-------:|:----------------|
| **-1** | `N` | 🔴 rosso  |
| **0**  | `O` | ⚫ spento  |
| **+1** | `P` | 🟢 verde  |

| Binario | Ternario | Cosa fa |
|:--------|:---------|:--------|
| `NOT`   | `TINV` (STI) | inverte: N↔P, O resta O |
| `AND`   | `TMIN`  | il minore dei due trit |
| `OR`    | `TMAX`  | il maggiore dei due trit |
| —       | `FULL ADDER` | somma `a + b + riporto` in ternario |

La bellezza del "bilanciato": negare un numero è solo invertire il segno di ogni trit — **non c'è
un bit di segno separato** come nel binario. Il ternario ha inoltre **tre** invertitori: STI, NTI, PTI.

---

## 📁 Struttura

```
TernaryLogicSim-NET/
├── TernaryLogicSim.sln
├── src/
│   ├── TernaryLogicSim.Core/        ← CORE di simulazione (C# puro, 0 dipendenze)
│   └── TernaryLogicSim.App/         ← app Avalonia (editor visuale)
├── tests/
│   ├── TernaryLogicSim.Tests/       ← test xUnit (dotnet test)
│   └── python/                      ← validazione esaustiva indipendente
├── docs/GUIA-MAKER.md               ← guida dettagliata "come è fatto"
└── LICENSE · README*.md
```

Il **core è C# puro senza motore**, quindi può essere testato fuori da qualsiasi interfaccia.

---

## 🛠️ Compilare ed eseguire (passo dopo passo)

**Prerequisito:** il [.NET SDK](https://dotnet.microsoft.com/download) 8.0 o successivo, gratuito (testato su **.NET 10**).

```bash
git clone https://github.com/charlesmmorais/Ternary-Logic-Sim.git
cd Ternary-Logic-Sim
dotnet test                                   # esegue i test del core
dotnet run --project src/TernaryLogicSim.App  # apre l'editor visuale
```

Pubblicare un eseguibile autonomo (opzionale):
```bash
dotnet publish src/TernaryLogicSim.App -c Release -r win-x64 --self-contained   # Windows
dotnet publish src/TernaryLogicSim.App -c Release -r linux-x64 --self-contained # Linux
dotnet publish src/TernaryLogicSim.App -c Release -r osx-arm64 --self-contained # macOS (Apple Silicon)
```

> Niente .NET? Puoi validare **solo la logica** con Python: `python3 tests/python/validate_core.py`.

---

## 🕹️ Usare l'editor

- **Clicca una porta** nella palette a sinistra → appare sul canvas.
- **Trascina il corpo** di un chip per spostarlo.
- **Collegare i fili:** clicca un **pin di uscita** (destra) e poi uno di **ingresso** (sinistra).
- **Ingresso a 3 stati:** clicca un chip **IN** per alternare `N → O → P` (trascinalo per spostarlo).
- **Eliminare:** clic destro su un chip.
- **➕ Crea chip:** raggruppa il circuito attuale in un chip riutilizzabile (in *I MIEI CHIP*).
- **💾 Salva / 📂 Apri:** salva l'intero progetto (libreria + circuito) in JSON.

L'app si apre con un **sommatore** di esempio. La simulazione dal vivo ricolora i fili secondo i valori.

---

## ✅ Come è stato testato

La logica del core è stata validata con **test esaustivi** (in xUnit e, in modo indipendente, in Python):
tutte le combinazioni di porte, **i 27 casi del full adder**, conversione intero↔ternario bilanciato
su ampio intervallo, sommatore ripple multi-trit (migliaia di casi), impacchettamento `TritBus`,
il valutatore di circuito a punto fisso e la valutazione dei chip composti (anche annidati).

---

## 📜 Crediti / NOTICE

Ispirato a [Digital-Logic-Sim](https://github.com/SebLague/Digital-Logic-Sim) di **Sebastian Lague**
(MIT). Il codice qui è un'implementazione ternaria originale in .NET/Avalonia. Licenza **MIT** —
vedi [LICENSE](LICENSE).
