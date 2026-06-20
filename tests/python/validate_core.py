#!/usr/bin/env python3
# =============================================================================
#  validate_core.py
#  Porta fiel dos algoritmos C# do nucleo (Trit, portas, somador, TritBus,
#  Circuit) para Python, e roda testes EXAUSTIVOS para provar a correcao.
#
#  IMPORTANTE: este validador imita a SEMANTICA DE INTEIROS DO C# ('/' trunca
#  em direcao ao zero, nao floor). Sem isso, bugs de divisao com negativos
#  passariam despercebidos (foi o que aconteceu na 1a versao).
# =============================================================================

import sys
from itertools import product

N, O, P = -1, 0, 1
ALL = [N, O, P]
fails = 0

def check(cond, msg):
    global fails
    if not cond:
        fails += 1
        print("  FALHA:", msg)

# ---- Semantica de inteiros do C# (trunca em direcao ao zero) ----------------
def cdiv(a, b):
    q = abs(a) // abs(b)
    return q if (a < 0) == (b < 0) else -q

def cmod(a, b):
    return a - b * cdiv(a, b)

# ---- Portas (espelham TernaryGates.cs) --------------------------------------
def g_not(a):       return -a
def g_min(a, b):    return min(a, b)
def g_max(a, b):    return max(a, b)
def g_nmin(a, b):   return -min(a, b)
def g_nmax(a, b):   return -max(a, b)
def g_consensus(a, b):
    if a == P and b == P: return P
    if a == N and b == N: return N
    return O
def g_any(a, b):    return (a + b > 0) - (a + b < 0)   # sign(a+b)
def g_mul(a, b):    return a * b
def g_shiftup(a):   return min(1, a + 1)
def g_shiftdown(a): return max(-1, a - 1)

# ---- Aritmetica (espelha TernaryArithmetic.cs) ------------------------------
def full_adder(a, b, cin):
    total = a + b + cin
    carry = 0
    while total > 1:  total -= 3; carry += 1
    while total < -1: total += 3; carry -= 1
    return total, carry

def from_int(value):
    if value == 0: return [O]
    trits = []
    while value != 0:
        rem = cmod(value, 3)          # -2..2, como no C#
        if rem == 2:    t = -1
        elif rem == -2: t = 1
        else:           t = rem       # -1, 0, 1
        trits.append(t)
        value = cdiv(value - t, 3)    # (value - t) e' multiplo exato de 3
    return trits

def to_int(trits):
    return sum(t * (3 ** i) for i, t in enumerate(trits))

def ripple_add(a, b, width):
    res, carry = [], O
    for i in range(width):
        ta = a[i] if i < len(a) else O
        tb = b[i] if i < len(b) else O
        s, carry = full_adder(ta, tb, carry)
        res.append(s)
    return res

print("== 1. Portas unarias ==")
for a in ALL:
    check(g_not(a) == -a, f"NOT {a}")
    check(g_not(g_not(a)) == a, f"NOT NOT involucao {a}")
    check(g_shiftup(a) == min(1, a + 1), f"ShiftUp {a}")
    check(g_shiftdown(a) == max(-1, a - 1), f"ShiftDown {a}")
check(g_not(N) == P and g_not(O) == O and g_not(P) == N, "tabela NOT explicita")

print("== 2. Portas binarias: exaustivo 3x3 ==")
for a, b in product(ALL, ALL):
    check(g_min(a, b) == min(a, b), f"MIN {a},{b}")
    check(g_max(a, b) == max(a, b), f"MAX {a},{b}")
    check(g_nmin(a, b) == -min(a, b), f"NMIN {a},{b}")
    check(g_nmax(a, b) == -max(a, b), f"NMAX {a},{b}")
    check(g_mul(a, b) == a * b, f"MUL {a},{b}")
    check(g_min(a, b) == g_min(b, a), f"MIN comutativo {a},{b}")
    check(g_max(a, b) == g_max(b, a), f"MAX comutativo {a},{b}")
for a, b in product(ALL, ALL):
    check(g_not(g_min(a, b)) == g_max(g_not(a), g_not(b)), f"De Morgan {a},{b}")

print("== 3. Full adder: exaustivo 3x3x3 = 27 ==")
for a, b, c in product(ALL, ALL, ALL):
    s, carry = full_adder(a, b, c)
    check(s in ALL and carry in ALL, f"faixa FA {a},{b},{c}")
    check(carry * 3 + s == a + b + c, f"FA correto {a},{b},{c}")

print("== 4. Conversao inteiro <-> ternario (round-trip -1000..1000) ==")
for v in range(-1000, 1001):
    check(to_int(from_int(v)) == v, f"round-trip {v}")
check(from_int(1) == [P], "1 = [P]")
check(from_int(2) == [N, P], "2 = [N,P]")
check(from_int(3) == [O, P], "3 = [O,P]")
check(from_int(-2) == [P, N], "-2 = [P,N]")
check(from_int(-200) is not None and to_int(from_int(-200)) == -200, "-200 round-trip")

print("== 5. Somador ripple multi-trit vs aritmetica ==")
W = 6
lim = (3 ** W - 1) // 2
import random
random.seed(42)
for _ in range(3000):
    x = random.randint(-lim // 2, lim // 2)
    y = random.randint(-lim // 2, lim // 2)
    res = ripple_add(from_int(x), from_int(y), W)
    check(to_int(res) == x + y, f"ripple {x}+{y} = {to_int(res)}")

print("== 6. TritBus: empacotamento 2 bits/trit ==")
code = {N: 0, O: 1, P: 2}
decode = {0: N, 1: O, 2: P, 3: O}
def bus_set(packed, idx, val):
    shift = idx * 2
    packed &= ~(0b11 << shift)
    packed |= (code[val] & 0b11) << shift
    return packed
def bus_get(packed, idx):
    return decode[(packed >> (idx * 2)) & 0b11]
packed = 0
seq = [random.choice(ALL) for _ in range(32)]
for i, t in enumerate(seq):
    packed = bus_set(packed, i, t)
for i, t in enumerate(seq):
    check(bus_get(packed, i) == t, f"TritBus idx {i}")

print("== 7. Engine Circuit (ponto fixo): NOT-NOT e somador de 3 trits ==")
class Node:
    def __init__(self, nid, fn, nin, nout):
        self.id, self.fn = nid, fn
        self.inp = [O] * nin
        self.out = [O] * nout
    def evaluate(self):
        self.out = self.fn(self.inp)

class Circuit:
    def __init__(self, n_in, n_out):
        self.nodes = []
        self.conns = []
        self.inp = [O] * n_in
        self.out = [O] * n_out
    def add(self, fn, nin, nout):
        nid = len(self.nodes); self.nodes.append(Node(nid, fn, nin, nout)); return nid
    def connect(self, frm, to):
        self.conns.append((frm, to))
    def read_src(self, src):
        n, p = src
        return self.inp[p] if n == -1 else self.nodes[n].out[p]
    def run(self, max_iter=100):
        for _ in range(max_iter):
            changed = False
            for frm, to in self.conns:
                val = self.read_src(frm)
                tn, tp = to
                if tn == -1:
                    if self.out[tp] != val: self.out[tp] = val; changed = True
                else:
                    if self.nodes[tn].inp[tp] != val: self.nodes[tn].inp[tp] = val; changed = True
            for nd in self.nodes:
                b = nd.out[0] if nd.out else O
                nd.evaluate()
                if nd.out and nd.out[0] != b: changed = True
            if not changed: return
        return

# 7a) NOT-NOT(x) == x
for x in ALL:
    c = Circuit(1, 1)
    n1 = c.add(lambda i: [g_not(i[0])], 1, 1)
    n2 = c.add(lambda i: [g_not(i[0])], 1, 1)
    c.connect((-1, 0), (n1, 0))
    c.connect((n1, 0), (n2, 0))
    c.connect((n2, 0), (-1, 0))
    c.inp[0] = x
    c.run()
    check(c.out[0] == x, f"Circuit NOT-NOT {x}")

# 7b) Somador de 3 trits com 3 full adders encadeados
def fa_fn(i):
    s, carry = full_adder(i[0], i[1], i[2])
    return [s, carry]
for _ in range(500):
    x = random.randint(-13, 13)
    y = random.randint(-13, 13)
    xt = from_int(x) + [O, O, O]
    yt = from_int(y) + [O, O, O]
    c = Circuit(6, 4)
    fas = [c.add(fa_fn, 3, 2) for _ in range(3)]
    zero = c.add(lambda i: [O], 0, 1)
    for k in range(3):
        c.connect((-1, k), (fas[k], 0))
        c.connect((-1, 3 + k), (fas[k], 1))
        if k == 0:
            c.connect((zero, 0), (fas[0], 2))
        else:
            c.connect((fas[k - 1], 1), (fas[k], 2))
        c.connect((fas[k], 0), (-1, k))
    c.connect((fas[2], 1), (-1, 3))
    for k in range(3):
        c.inp[k] = xt[k]; c.inp[3 + k] = yt[k]
    c.run()
    res = [c.out[0], c.out[1], c.out[2], c.out[3]]
    check(to_int(res) == x + y, f"Circuit somador3 {x}+{y} = {to_int(res)}")

print()
if fails == 0:
    print("TODOS OS TESTES PASSARAM ✓")
    sys.exit(0)
else:
    print(f"{fails} TESTE(S) FALHARAM ✗")
    sys.exit(1)
