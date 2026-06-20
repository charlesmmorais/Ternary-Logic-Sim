#!/usr/bin/env python3
# =============================================================================
#  validate_composite.py
#  Mirror em Python da lógica de CHIPS COMPOSTOS (EditorModel.cs):
#  avaliador por ponto fixo + CompositeDefinition.Run (recursivo).
#  Prova que: (1) um composto avalia igual ao primitivo equivalente, e
#  (2) a composição ANINHADA (composto dentro de composto) funciona.
# =============================================================================

import sys
from itertools import product

N, O, P = -1, 0, 1
ALL = [N, O, P]
fails = 0
def check(c, m):
    global fails
    if not c: fails += 1; print("  FALHA:", m)

def full_adder(a, b, cin):
    t = a + b + cin; carry = 0
    while t > 1: t -= 3; carry += 1
    while t < -1: t += 3; carry -= 1
    return [t, carry]

# Avaliadores dos primitivos: recebem lista de entradas, devolvem lista de saídas
PRIM = {
    "Not":       lambda i: [-i[0]],
    "Min":       lambda i: [min(i[0], i[1])],
    "Max":       lambda i: [max(i[0], i[1])],
    "FullAdder": lambda i: full_adder(i[0], i[1], i[2]),
    "ConstZero": lambda i: [O],
    "ConstPos":  lambda i: [P],
    "ConstNeg":  lambda i: [N],
}
NIN  = {"Not":1,"Min":2,"Max":2,"FullAdder":3,"ConstZero":0,"ConstPos":0,"ConstNeg":0,
        "Input":0,"Output":1}
NOUT = {"Not":1,"Min":1,"Max":1,"FullAdder":2,"ConstZero":1,"ConstPos":1,"ConstNeg":1,
        "Input":1,"Output":0}

class Chip:
    def __init__(s, cid, kind, comp=None, nin=None, nout=None):
        s.id, s.kind, s.comp = cid, kind, comp
        ni = nin if nin is not None else NIN[kind]
        no = nout if nout is not None else NOUT[kind]
        s.inp = [O]*ni; s.out = [O]*no

class Graph:
    def __init__(s, lib): s.chips=[]; s.wires=[]; s.lib=lib; s._id=0
    def add(s, kind):
        c=Chip(s._id, kind); s._id+=1; s.chips.append(c); return c
    def add_comp(s, name):
        d=s.lib[name]; c=Chip(s._id,"Composite",name,len(d["in"]),len(d["out"]))
        s._id+=1; s.chips.append(c); return c
    def by(s, cid): return next(c for c in s.chips if c.id==cid)
    def connect(s, fc, fp, tc, tp): s.wires.append((fc,fp,tc,tp))
    def evaluate(s, mx=200):
        for _ in range(mx):
            ch=False
            for fc,fp,tc,tp in s.wires:
                v=s.by(fc).out[fp]
                t=s.by(tc)
                if t.inp[tp]!=v: t.inp[tp]=v; ch=True
            for c in s.chips:
                if c.kind in ("Input","Output"): continue
                if c.kind=="Composite":
                    outs=run_comp(s.lib[c.comp], c.inp, s.lib)
                    for i in range(len(c.out)):
                        if c.out[i]!=outs[i]: c.out[i]=outs[i]; ch=True
                else:
                    b=c.out[0] if c.out else O
                    c.out=PRIM[c.kind](c.inp)
                    if c.out and c.out[0]!=b: ch=True
            if not ch: break

# Espelha CompositeDefinition.Run
def run_comp(defn, inputs, lib):
    g=Graph(lib); ids=[]
    for nd in defn["nodes"]:
        if nd["kind"]=="Composite": ids.append(g.add_comp(nd["comp"]).id)
        else: ids.append(g.add(nd["kind"]).id)
    for fn,fp,tn,tp in defn["wires"]:
        g.connect(ids[fn],fp,ids[tn],tp)
    for k,ni in enumerate(defn["in"]):
        if k<len(inputs): g.by(ids[ni]).out[0]=inputs[k]
    g.evaluate()
    outs=[]
    for no in defn["out"]:
        c=g.by(ids[no]); outs.append(c.inp[0] if c.inp else O)
    return outs

# --- Definição: HalfAdder = FullAdder com carry-in fixo O, expõe a,b -> sum,carry
# nodes: 0=Input(a) 1=Input(b) 2=ConstZero 3=FullAdder 4=Output(sum) 5=Output(carry)
HALF = {
    "nodes":[{"kind":"Input"},{"kind":"Input"},{"kind":"ConstZero"},
             {"kind":"FullAdder"},{"kind":"Output"},{"kind":"Output"}],
    "wires":[(0,0,3,0),(1,0,3,1),(2,0,3,2),(3,0,4,0),(3,1,5,0)],
    "in":[0,1], "out":[4,5],
}
lib={"HalfAdder":HALF}

print("== Composto HalfAdder == FullAdder(a,b,0) (exaustivo 3x3) ==")
for a,b in product(ALL,ALL):
    got=run_comp(HALF,[a,b],lib)
    exp=full_adder(a,b,O)
    check(got==exp, f"HalfAdder {a},{b}: got {got} exp {exp}")

# --- Aninhado: Wrap usa uma instância de HalfAdder por dentro
# nodes: 0=Input(a) 1=Input(b) 2=Composite(HalfAdder) 3=Output 4=Output
WRAP = {
    "nodes":[{"kind":"Input"},{"kind":"Input"},
             {"kind":"Composite","comp":"HalfAdder"},{"kind":"Output"},{"kind":"Output"}],
    "wires":[(0,0,2,0),(1,0,2,1),(2,0,3,0),(2,1,4,0)],
    "in":[0,1], "out":[3,4],
}
lib["Wrap"]=WRAP
print("== Composto ANINHADO Wrap(HalfAdder) == FullAdder(a,b,0) ==")
for a,b in product(ALL,ALL):
    got=run_comp(WRAP,[a,b],lib)
    exp=full_adder(a,b,O)
    check(got==exp, f"Wrap {a},{b}: got {got} exp {exp}")

# --- Usar o composto dentro de um grafo de topo (como o editor faz)
print("== Instância de composto num grafo de topo ==")
for a,b in product(ALL,ALL):
    g=Graph(lib)
    ia=g.add("Input"); ib=g.add("Input")
    ha=g.add_comp("HalfAdder")
    os_=g.add("Output"); oc=g.add("Output")
    g.connect(ia.id,0,ha.id,0); g.connect(ib.id,0,ha.id,1)
    g.connect(ha.id,0,os_.id,0); g.connect(ha.id,1,oc.id,0)
    ia.out[0]=a; ib.out[0]=b
    g.evaluate()
    got=[os_.inp[0], oc.inp[0]]; exp=full_adder(a,b,O)
    check(got==exp, f"topo {a},{b}: got {got} exp {exp}")

print()
print("TODOS OS TESTES DE COMPOSIÇÃO PASSARAM ✓" if fails==0 else f"{fails} FALHARAM ✗")
sys.exit(0 if fails==0 else 1)
