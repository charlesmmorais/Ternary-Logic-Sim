# Ternary-Logic-Sim 🔺 (.NET / Avalonia)

**🌐 [Português](README.pt-BR.md) · [English](README.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md)** · [📖 Guía maker](docs/GUIA-MAKER.md)

Un simulador de **lógica digital ternaria balanceada** (`-1, 0, +1`), hecho en **C# puro + Avalonia** —
inspirado en el [Digital-Logic-Sim](https://github.com/SebLague/Digital-Logic-Sim) de Sebastian Lague,
pero **sin Unity**: se ejecuta como una app de escritorio .NET multiplataforma (Windows, Linux, macOS).

Mientras un ordenador común piensa en **bits** (0/1), aquí la señal fundamental es el **trit**,
que toma **tres** valores. Es un laboratorio didáctico y "maker" para entender cómo funciona una máquina ternaria.

> **Estado:** núcleo de simulación **probado** ✓ · editor visual **funcional** (guardar/abrir + chips compuestos).

---

## 🤔 Ternario balanceado en 30 segundos

| Valor | Símbolo | Color en la app |
|:-----:|:-------:|:----------------|
| **-1** | `N` | 🔴 rojo  |
| **0**  | `O` | ⚫ apagado |
| **+1** | `P` | 🟢 verde |

| Binario | Ternario | Qué hace |
|:--------|:---------|:---------|
| `NOT`   | `TINV` (STI) | invierte: N↔P, O queda O |
| `AND`   | `TMIN`  | el menor de los dos trits |
| `OR`    | `TMAX`  | el mayor de los dos trits |
| —       | `FULL ADDER` | suma `a + b + acarreo` en ternario |

La belleza de lo "balanceado": negar un número es solo invertir el signo de cada trit — **no hay
bit de signo separado** como en binario. El ternario tiene además **tres** inversores: STI, NTI, PTI.

---

## 📁 Estructura

```
TernaryLogicSim-NET/
├── TernaryLogicSim.sln
├── src/
│   ├── TernaryLogicSim.Core/        ← NÚCLEO de simulación (C# puro, 0 dependencias)
│   └── TernaryLogicSim.App/         ← app Avalonia (editor visual)
├── tests/
│   ├── TernaryLogicSim.Tests/       ← pruebas xUnit (dotnet test)
│   └── python/                      ← validación exhaustiva independiente
├── docs/GUIA-MAKER.md               ← guía detallada de "cómo se hizo"
└── LICENSE · README*.md
```

El **núcleo es C# puro sin dependencia de motor**, por eso se puede probar fuera de cualquier UI.

---

## 🛠️ Compilar y ejecutar (paso a paso)

**Requisito:** el [.NET SDK](https://dotnet.microsoft.com/download) 8.0 o superior, gratuito (probado en **.NET 10**).

```bash
git clone https://github.com/charlesmmorais/Ternary-Logic-Sim.git
cd Ternary-Logic-Sim
dotnet test                                   # ejecuta las pruebas del núcleo
dotnet run --project src/TernaryLogicSim.App  # abre el editor visual
```

Publicar un ejecutable independiente (opcional):
```bash
dotnet publish src/TernaryLogicSim.App -c Release -r win-x64 --self-contained   # Windows
dotnet publish src/TernaryLogicSim.App -c Release -r linux-x64 --self-contained # Linux
dotnet publish src/TernaryLogicSim.App -c Release -r osx-arm64 --self-contained # macOS (Apple Silicon)
```

> ¿Sin .NET? Puedes validar **solo la lógica** con Python: `python3 tests/python/validate_core.py`.

---

## 🕹️ Usar el editor

- **Haz clic en una puerta** de la paleta izquierda → aparece en el lienzo.
- **Arrastra el cuerpo** de un chip para moverlo.
- **Conectar cables:** clic en un **pin de salida** (derecha) y luego en uno de **entrada** (izquierda).
- **Entrada de 3 estados:** clic en un chip **IN** para alternar `N → O → P` (arrástralo para moverlo).
- **Borrar:** clic derecho sobre un chip.
- **➕ Crear chip:** agrupa el circuito actual en un chip reutilizable (aparece en *MIS CHIPS*).
- **💾 Guardar / 📂 Abrir:** guarda todo el proyecto (biblioteca + circuito) en JSON.

La app abre con un **sumador** de ejemplo. La simulación en vivo recolorea los cables según los valores.

---

## ✅ Cómo se probó

La lógica del núcleo se validó con **pruebas exhaustivas** (en xUnit e, independientemente, en Python):
todas las combinaciones de puertas, **los 27 casos del full adder**, conversión entero↔ternario
balanceado en amplio rango, sumador ripple multi-trit (miles de casos), empaquetado `TritBus`, el
evaluador de circuito por punto fijo y la evaluación de chips compuestos (incluso anidados).

---

## 📜 Créditos / NOTICE

Inspirado en [Digital-Logic-Sim](https://github.com/SebLague/Digital-Logic-Sim) de **Sebastian Lague**
(MIT). El código aquí es una implementación original en ternario en .NET/Avalonia. Licencia **MIT** —
ver [LICENSE](LICENSE).
