# Ternary-Logic-Sim 🔺 (.NET / Avalonia)

**🌐 [Português](README.pt-BR.md) · [English](README.md) · [Español](README.es.md) · [Français](README.fr.md) · [Italiano](README.it.md)** · [📖 Guide maker](docs/GUIA-MAKER.md)

Un simulateur de **logique numérique ternaire équilibrée** (`-1, 0, +1`), écrit en **C# pur + Avalonia** —
inspiré du [Digital-Logic-Sim](https://github.com/SebLague/Digital-Logic-Sim) de Sebastian Lague,
mais **sans Unity** : il s'exécute comme une application de bureau .NET multiplateforme (Windows, Linux, macOS).

Alors qu'un ordinateur ordinaire pense en **bits** (0/1), ici le signal fondamental est le **trit**,
qui prend **trois** valeurs. C'est un laboratoire pédagogique et « maker » pour comprendre le fonctionnement d'une machine ternaire.

> **État :** cœur de simulation **testé** ✓ · éditeur visuel **fonctionnel** (sauvegarde/ouverture + puces composites).

---

## 🤔 Le ternaire équilibré en 30 secondes

| Valeur | Symbole | Couleur dans l'app |
|:------:|:-------:|:-------------------|
| **-1** | `N` | 🔴 rouge   |
| **0**  | `O` | ⚫ éteint   |
| **+1** | `P` | 🟢 vert    |

| Binaire | Ternaire | Rôle |
|:--------|:---------|:-----|
| `NOT`   | `TINV` (STI) | inverse : N↔P, O reste O |
| `AND`   | `TMIN`  | le plus petit des deux trits |
| `OR`    | `TMAX`  | le plus grand des deux trits |
| —       | `FULL ADDER` | additionne `a + b + retenue` en ternaire |

La beauté de l'« équilibré » : nier un nombre revient à inverser le signe de chaque trit — **pas de
bit de signe séparé** comme en binaire. Le ternaire possède aussi **trois** inverseurs : STI, NTI, PTI.

---

## 📁 Structure

```
TernaryLogicSim-NET/
├── TernaryLogicSim.sln
├── src/
│   ├── TernaryLogicSim.Core/        ← CŒUR de simulation (C# pur, 0 dépendance)
│   └── TernaryLogicSim.App/         ← app Avalonia (éditeur visuel)
├── tests/
│   ├── TernaryLogicSim.Tests/       ← tests xUnit (dotnet test)
│   └── python/                      ← validation exhaustive indépendante
├── docs/GUIA-MAKER.md               ← guide détaillé « comment c'est fait »
└── LICENSE · README*.md
```

Le **cœur est du C# pur sans moteur**, il peut donc être testé en dehors de toute interface.

---

## 🛠️ Compiler et lancer (pas à pas)

**Prérequis :** le [.NET SDK](https://dotnet.microsoft.com/download) 8.0 ou plus récent, gratuit (testé sur **.NET 10**).

```bash
git clone https://github.com/charlesmmorais/Ternary-Logic-Sim.git
cd Ternary-Logic-Sim
dotnet test                                   # lance les tests du cœur
dotnet run --project src/TernaryLogicSim.App  # ouvre l'éditeur visuel
```

Publier un exécutable autonome (optionnel) :
```bash
dotnet publish src/TernaryLogicSim.App -c Release -r win-x64 --self-contained   # Windows
dotnet publish src/TernaryLogicSim.App -c Release -r linux-x64 --self-contained # Linux
dotnet publish src/TernaryLogicSim.App -c Release -r osx-arm64 --self-contained # macOS (Apple Silicon)
```

> Pas de .NET ? Vous pouvez valider **uniquement la logique** avec Python : `python3 tests/python/validate_core.py`.

---

## 🕹️ Utiliser l'éditeur

- **Cliquez sur une porte** dans la palette de gauche → elle apparaît sur le canevas.
- **Glissez le corps** d'une puce pour la déplacer.
- **Câbler :** cliquez sur une **broche de sortie** (droite) puis sur une **broche d'entrée** (gauche).
- **Entrée à 3 états :** cliquez sur une puce **IN** pour passer `N → O → P` (glissez pour la déplacer).
- **Supprimer :** clic droit sur une puce.
- **➕ Créer une puce :** regroupe le circuit courant en une puce réutilisable (dans *MES PUCES*).
- **💾 Enregistrer / 📂 Ouvrir :** stocke tout le projet (bibliothèque + circuit) en JSON.

L'app s'ouvre sur un **additionneur** d'exemple. La simulation en direct recolore les fils selon les valeurs.

---

## ✅ Comment c'est testé

La logique du cœur a été validée par des **tests exhaustifs** (en xUnit et, indépendamment, en Python) :
toutes les combinaisons de portes, **les 27 cas du full adder**, conversion entier↔ternaire équilibré
sur une large plage, additionneur ripple multi-trit (des milliers de cas), empaquetage `TritBus`,
l'évaluateur de circuit par point fixe et l'évaluation des puces composites (y compris imbriquées).

---

## 📜 Crédits / NOTICE

Inspiré de [Digital-Logic-Sim](https://github.com/SebLague/Digital-Logic-Sim) de **Sebastian Lague**
(MIT). Le code ici est une implémentation ternaire originale en .NET/Avalonia. Sous licence **MIT** —
voir [LICENSE](LICENSE).
