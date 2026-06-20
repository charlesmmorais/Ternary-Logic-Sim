# 🛠️ Guia Maker — como o Ternary-Logic-Sim foi feito

Este guia conta, de forma didática e detalhada, **como o projeto foi construído** — as ideias, as
decisões de arquitetura, como cada peça funciona e até os **bugs reais** que apareceram no caminho e
como foram corrigidos. A intenção é que você consiga entender, modificar e estender o simulador.

> Pré-requisitos para acompanhar: noções de C# e vontade de mexer. Nenhum conhecimento prévio de
> ternário é necessário — começamos do zero.

---

## 1. A grande ideia: por que ternário "balanceado"?

O computador comum usa **bits**: cada fio carrega 0 ou 1. Este projeto explora uma alternativa onde
o sinal fundamental é o **trit**, que assume **três** valores. Escolhemos o ternário **balanceado**:

| Valor | Símbolo |
|:-----:|:-------:|
| -1    | `N`     |
| 0     | `O`     |
| +1    | `P`     |

Por que "balanceado" (em vez de 0, 1, 2)? Porque os valores são simétricos em torno do zero. Isso traz
uma propriedade elegante: **negar um número é apenas inverter o sinal de cada trit**. Não existe "bit de
sinal" nem complemento de dois — subtrair é somar o negativo. Um número é escrito como soma de potências
de 3:

```
valor = Σ  trit_i · 3^i

[P]      =  1
[N P]    = -1 + 1·3        =  2
[O P]    =  0 + 1·3        =  3
[P N]    =  1 + (-1)·3     = -2
```

---

## 2. Visão geral da arquitetura

O projeto é uma **solução .NET** com três partes, separando rigorosamente "o que é a verdade lógica"
de "como isso aparece na tela":

```
TernaryLogicSim.Core   →  o motor de simulação (C# puro, sem UI, sem engine)
TernaryLogicSim.App    →  o editor visual (Avalonia) que consome o Core
TernaryLogicSim.Tests  →  testes xUnit do Core
```

A decisão mais importante foi: **o núcleo não depende de nada gráfico**. Ele é C# puríssimo. Isso
permitiu testá-lo exaustivamente (inclusive portando os algoritmos para Python e rodando milhares de
casos) **antes** de qualquer interface — e foi o que deu confiança de que a lógica está correta.

---

## 3. O núcleo, peça por peça

### 3.1 `Trit` e `TritBus` — representando o sinal

`Trit` é um `enum` com quatro valores: `Negative`, `Zero`, `Positive` e `Disconnected` (flutuante).
Os valores numéricos do enum (0,1,2,3) foram escolhidos para caber em **2 bits**, o que permite
empacotar muitos trits dentro de um inteiro — exatamente como o Digital-Logic-Sim faz com bits.

`TritBus` é essa "embalagem": um `ulong` (64 bits) guarda até **32 trits** (2 bits cada). Ler/escrever
um trit é deslocamento + máscara:

```csharp
public Trit Get(int index)      => (Trit)((Packed >> (index*2)) & 0b11);
public void Set(int index, Trit value) {
    int shift = index * 2;
    Packed &= ~(0b11UL << shift);              // limpa os 2 bits
    Packed |= ((ulong)value & 0b11) << shift;  // grava
}
```

### 3.2 `TernaryGates` — as portas primitivas

No binário você tem AND/OR/NOT. No ternário balanceado os equivalentes naturais são **MIN, MAX e os
inversores**. São funções puras (sem estado), o que as torna triviais de testar.

- **TINV / STI** (inversor padrão): `N↔P`, `O→O`. É a negação balanceada.
- **NTI** (negativo): `N→P, O→N, P→N`.
- **PTI** (positivo): `N→P, O→P, P→N`.
- **TMIN** (≈AND): o menor dos dois trits. **TMAX** (≈OR): o maior.
- Mais: `Consensus`, `Any`, `Mul` (produto de sinais), `ShiftUp/Down`.

Com TMIN, TMAX e os inversores (+ constantes) já temos um conjunto **funcionalmente completo**: dá para
construir qualquer função ternária.

### 3.3 `TernaryArithmetic` — o somador completo

O tijolo da aritmética é o **full adder ternário**: soma `a + b + carry_in` (cada um em {N,O,P}) e
devolve `soma` e `carry_out`, ambos normalizados para {N,O,P}:

```csharp
public static (Trit sum, Trit carryOut) FullAdder(Trit a, Trit b, Trit carryIn)
{
    int total = a.ToInt() + b.ToInt() + carryIn.ToInt(); // -3..+3
    int carry = 0;
    while (total > 1)  { total -= 3; carry += 1; }
    while (total < -1) { total += 3; carry -= 1; }
    return (TritOps.FromInt(total), TritOps.FromInt(carry));
}
```

### 3.4 `BuiltinChip` e `Circuit` — o motor

`BuiltinChip` é o catálogo de chips primitivos: declara quantas entradas/saídas cada um tem e como
avaliá-lo. `Circuit` é um **grafo**: nós (instâncias de chips) + fios (conexões saída→entrada) + pinos
externos. A avaliação é por **ponto fixo iterativo**: propaga os fios, reavalia todos os nós, e repete
até nada mudar (estabilizar). Essa abordagem lida naturalmente com circuitos combinacionais **e** com
realimentação (memória/sequencial). Um limite de iterações detecta oscilação.

---

## 4. O editor visual (Avalonia)

### 4.1 Por que Avalonia (e não Unity)?

O Digital-Logic-Sim é feito em Unity. Aqui escolhemos **Avalonia**, um framework de UI .NET puro,
multiplataforma e open-source. Vantagens para este projeto: roda com um simples `dotnet run`, sem
instalar engine; e como o núcleo já era C# puro, ele entrou **sem mudar uma linha**.

### 4.2 Desenho "na mão" — `EditorCanvas`

Em vez de criar centenas de controles, o `EditorCanvas` desenha **tudo** num único `Control`,
sobrescrevendo `Render(DrawingContext)`: chips são retângulos, pinos são círculos, fios são curvas de
Bézier. A interação (arrastar, ligar, alternar, apagar) é feita com **hit-testing manual** nos eventos
de ponteiro. Esse estilo dá o "feeling" de um editor de nós e é simples de entender.

As cores codificam o valor que passa em cada fio/pino: 🔴 `N`, ⚫ `O`, 🟢 `P`. A simulação roda a cada
interação e o canvas se redesenha.

### 4.3 O modelo do editor — `EditorModel`

Espelha o núcleo, mas com posições na tela: `EditorChip` (tipo, x, y, arrays de trits), `EditorWire`
(fio) e `EditorGraph` (grafo + avaliador de ponto fixo). Os chips `IN` (fonte que o usuário cicla) e
`OUT` (LED) são tratados de forma especial; todos os demais reusam `BuiltinChip.Evaluate` do núcleo —
ou seja, **a verdade ternária vem sempre do Core**.

### 4.4 Chips compostos — o recurso mais "DLS"

Você pode **agrupar um circuito inteiro num novo chip reutilizável**. Como funciona:

- `CompositeDefinition.CreateFrom(grafo, nome)` tira um "retrato" do circuito atual: a lista de nós, os
  fios, e a **ordem dos pinos externos** (os chips `IN` viram entradas; os `OUT` viram saídas, ordenados
  de cima para baixo para uma ordem estável).
- Quando você coloca esse chip em outro circuito, ele vira um único bloco. Para avaliá-lo, o método
  `Run` **reconstrói o circuito interno**, injeta os trits de entrada nos `IN`, roda o avaliador e lê os
  `OUT`. Como `Run` usa o mesmo avaliador, **chips compostos podem conter outros chips compostos** —
  a avaliação é recursiva.

### 4.5 Salvar/carregar — `CircuitIO`

Um "projeto" guarda a biblioteca de chips compostos + o circuito atual, serializados em **JSON** com
`System.Text.Json`. Detalhe importante: os fios referenciam chips por **índice na lista**, não por Id —
assim o arquivo independe de como os Ids foram gerados.

---

## 5. Como tudo foi testado (e por que confiar)

Antes de qualquer tela, os algoritmos foram validados com **testes exaustivos**:

- Portas: todas as 3 e 9 combinações conferidas contra a definição matemática, incluindo as **Leis de
  De Morgan ternárias**.
- Full adder: todas as **27** combinações (`carry·3 + soma == a + b + c`).
- Conversão inteiro↔ternário: round-trip em ampla faixa.
- Somador ripple multi-trit: milhares de somas aleatórias vs. a aritmética normal.
- `TritBus`: empacotamento/desempacotamento de 32 trits.
- Motor `Circuit` e **chips compostos** (inclusive aninhados): montados e conferidos contra o esperado.

Rodando:
```bash
dotnet test                                # testes nativos C# (xUnit)
python3 tests/python/validate_core.py      # validação independente da lógica
python3 tests/python/validate_composite.py # validação da composição
```

---

## 6. Bugs reais que apareceram (e o que aprendemos)

Esta seção é de propósito — depurar faz parte de "fazer".

### 6.1 Divisão de negativos: Python ≠ C#

A conversão `FromInt` (inteiro → ternário) funcionava nos testes em Python mas **falhava em C#** para
negativos como −200. Causa: em Python a divisão inteira (`//`) arredonda para baixo (*floor*); em C# o
operador `/` **trunca em direção ao zero**. Para negativos isso dá quocientes diferentes. A correção foi
usar divisão exata: como `valor − t` é múltiplo de 3, `(valor − t) / 3` não sofre com truncamento.
**Lição:** ao "espelhar" um algoritmo entre linguagens, cuidado com a semântica de inteiros. O validador
Python foi ajustado para imitar a truncagem do C# e pegar esse tipo de bug no futuro.

### 6.2 `Render` selado no Avalonia

A primeira versão do canvas herdava de `Canvas`, mas `Panel.Render` é **sealed** (não pode ser
sobrescrito). Solução: herdar de `Control` (cujo `Render` é virtual) e implementar `ICustomHitTest`
para receber cliques.

### 6.3 O hit-test que "roubava" todos os cliques

Sintoma curioso: **nenhum botão da janela funcionava**, só as interações dentro do canvas. Causa: o
`ICustomHitTest.HitTest` retornava `true` para **qualquer ponto**. Como o canvas é o elemento mais "por
cima" na ordem de desenho, ele reivindicava os cliques da **janela inteira** — paleta e barra incluídas.
Correção: limitar o hit-test aos próprios limites do canvas (`point` dentro de `Bounds`).

### 6.4 O `IN` que não arrastava

Clicar no corpo do chip `IN` alternava o valor **em vez** de iniciar o arraste, então ele nunca movia.
Correção: começar o arraste em todos os chips e, ao **soltar sem ter movido** (um clique, distinguido por
um limiar de ~4px), aí sim alternar `N → O → P`.

---

## 7. Como estender o projeto

Algumas ideias, da mais simples à mais ambiciosa:

1. **Nova porta primitiva:** adicione um valor em `BuiltinChipType`, a função em `TernaryGates`, o `case`
   em `BuiltinChip.Evaluate`, o mapeamento em `ChipMeta`, e um botão na paleta (`MainWindow.axaml`).
2. **Barramentos multi-trit:** representar números com vários trits como um único "fio grosso" (já temos
   `TritBus` pronto para o empacotamento).
3. **Biblioteca de exemplos:** salvar circuitos clássicos (comparador, registrador/memória, multiplicador)
   como `.json` e abri-los pelo menu.
4. **Performance:** para circuitos enormes, trocar o avaliador "reavalia tudo" por uma fila de nós que
   só recalcula o que mudou.

---

## 8. Créditos

Inspirado no [Digital-Logic-Sim](https://github.com/SebLague/Digital-Logic-Sim) de **Sebastian Lague**
(licença MIT). Implementação ternária original em .NET/Avalonia. Licença **MIT**.
