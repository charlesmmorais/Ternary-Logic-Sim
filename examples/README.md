# 🧪 Circuitos de exemplo

Coleção de circuitos ternários prontos para abrir no app (**📂 Abrir**) e estudar. Cada um foi
**gerado e verificado** automaticamente por `build_examples.py` (a saída esperada confere).

> Dica: abra um exemplo, clique nos chips **IN** para mudar `N/O/P` e veja as saídas reagirem ao vivo.

| Arquivo | O que demonstra |
|:--------|:----------------|
| `01-inversores-STI-NTI-PTI.json` | Os **três inversores ternários** lado a lado (TINV/STI, NTI, PTI) a partir da mesma entrada. |
| `02-tmin-tmax.json` | **TMIN** (≈AND) e **TMAX** (≈OR) sobre duas entradas. |
| `03-consensus-any.json` | **CONSENSUS** (concordância forte) vs **ANY** (soma saturada). |
| `04-multiplicador-sinais.json` | **MUL**: produto de sinais (`N·P = N`) — o XNOR balanceado. |
| `05-shifters.json` | **SHIFT ▲ / ▼**: sobe/desce um nível com saturação. |
| `06-valor-absoluto.json` | **\|a\|** construído como `MAX(a, TINV(a))`. |
| `07-meio-somador.json` | **Meio-somador**: `a + b` → soma e vai-um (`P+P=+2` → soma N, carry P). |
| `08-somador-completo.json` | **Somador completo**: `a + b + carry_in` (`P+P+P=+3` → soma O, carry P). |
| `09-comparador-1trit.json` | **Comparador**: `sign(a−b) = ANY(a, TINV(b))` → P se a>b, O se igual, N se a<b. |
| `10-mediana-de-3.json` | **Mediana de 3 trits**: `MAX(MAX(MIN(a,b),MIN(b,c)),MIN(c,a))`. |
| `11-somador-2-trits.json` | **Somador de 2 trits** (ripple-carry) — soma dois números de 2 trits. |
| `12-somador-3-trits.json` | **Somador de 3 trits** — cobre a faixa `-13..+13` (`1+2=3`). |
| `13-de-morgan.json` | **Lei de De Morgan ternária**: `NOT(MIN(a,b)) = MAX(NOT a, NOT b)` (dois LEDs iguais). |
| `14-identidade-not-not.json` | `NOT(NOT(a)) = a` — a involução do inversor padrão. |
| `15-oscilador-instavel.json` | Curiosidade: um **NOT realimentado** em si mesmo não estabiliza (oscilação). |

## Como foram criados

O script `build_examples.py` define cada circuito em Python, **simula** para conferir as saídas e
escreve o `.json` no formato exato do app (`CircuitIO`). Para regenerar:

```bash
python3 examples/build_examples.py
```

Sinta-se livre para abrir, modificar e salvar suas próprias variações. Bom hacking ternário! 🔺
