#!/usr/bin/env python3
# =============================================================================
#  build_examples.py — gera (e VERIFICA) os circuitos de exemplo .json
#  Formato idêntico ao CircuitIO do app (chaves PascalCase; fios por índice).
# =============================================================================
import json, os, sys

OUT_DIR = os.path.dirname(os.path.abspath(__file__))
N, O, P = -1, 0, 1

# ---- avaliador (espelha EditorGraph.Evaluate / BuiltinChip) ----
def fa(a, b, c):
    t = a + b + c; carry = 0
    while t > 1: t -= 3; carry += 1
    while t < -1: t += 3; carry -= 1
    return [t, carry]
PRIM = {
 "Not": lambda i: [-i[0]],
 "Nti": lambda i: [P if i[0] == N else N],
 "Pti": lambda i: [N if i[0] == P else P],
 "ShiftUp": lambda i: [min(1, i[0] + 1)],
 "ShiftDown": lambda i: [max(-1, i[0] - 1)],
 "Min": lambda i: [min(i[0], i[1])],
 "Max": lambda i: [max(i[0], i[1])],
 "Nmin": lambda i: [-min(i[0], i[1])],
 "Nmax": lambda i: [-max(i[0], i[1])],
 "Consensus": lambda i: [P if i[0]==P and i[1]==P else N if i[0]==N and i[1]==N else O],
 "AnyGate": lambda i: [(1 if i[0]+i[1] > 0 else -1 if i[0]+i[1] < 0 else 0)],
 "Mul": lambda i: [i[0]*i[1]],
 "FullAdder": lambda i: fa(i[0], i[1], i[2]),
 "ConstNeg": lambda i: [N], "ConstZero": lambda i: [O], "ConstPos": lambda i: [P],
}
NIN = {"Input":0,"Output":1,"Not":1,"Nti":1,"Pti":1,"ShiftUp":1,"ShiftDown":1,
       "Min":2,"Max":2,"Nmin":2,"Nmax":2,"Consensus":2,"AnyGate":2,"Mul":2,
       "FullAdder":3,"ConstNeg":0,"ConstZero":0,"ConstPos":0}
NOUT= {"Input":1,"Output":0,"FullAdder":2,"ConstNeg":1,"ConstZero":1,"ConstPos":1}

class Circuit:
    def __init__(s): s.chips=[]; s.wires=[]
    def add(s, kind, x, y, val=0):
        idx=len(s.chips)
        s.chips.append({"Kind":kind,"Composite":None,"X":float(x),"Y":float(y),"Val":int(val)})
        return idx
    def wire(s, frm, fp, to, tp):
        s.wires.append({"From":frm,"FromPin":fp,"To":to,"ToPin":tp})
    def dump(s):
        return {"Version":1,"Library":[],"Circuit":{"Chips":s.chips,"Wires":s.wires}}
    # --- simulação para verificação ---
    def simulate(s):
        nin=[NIN.get(c["Kind"],0) for c in s.chips]
        nout=[NOUT.get(c["Kind"],1 if c["Kind"] not in("Output",) else 0) for c in s.chips]
        inp=[[O]*nin[i] for i in range(len(s.chips))]
        out=[[O]*nout[i] for i in range(len(s.chips))]
        for i,c in enumerate(s.chips):
            if c["Kind"]=="Input": out[i]=[c["Val"]]
        for _ in range(200):
            ch=False
            for w in s.wires:
                v=out[w["From"]][w["FromPin"]]
                if inp[w["To"]][w["ToPin"]]!=v: inp[w["To"]][w["ToPin"]]=v; ch=True
            for i,c in enumerate(s.chips):
                k=c["Kind"]
                if k in("Input","Output"): continue
                b=out[i][0] if out[i] else O
                out[i]=PRIM[k](inp[i])
                if out[i] and out[i][0]!=b: ch=True
            if not ch: break
        # devolve {label_do_OUT(idx): valor}
        res={}
        for i,c in enumerate(s.chips):
            if c["Kind"]=="Output": res[i]=inp[i][0]
        return res

def sym(v): return {N:"N",O:"O",P:"P"}.get(v,"?")

examples=[]  # (filename, builder, expected_check_or_None, descricao)

# 01 — Três inversores lado a lado
def c01():
    c=Circuit(); a=c.add("Input",60,160,P)
    n=c.add("Not",260,60); nt=c.add("Nti",260,160); pt=c.add("Pti",260,260)
    o1=c.add("Output",460,60); o2=c.add("Output",460,160); o3=c.add("Output",460,260)
    for g,o in((n,o1),(nt,o2),(pt,o3)):
        c.wire(a,0,g,0); c.wire(g,0,o,0)
    return c
examples.append(("01-inversores-STI-NTI-PTI.json", c01, lambda r: list(r.values())==[N,N,N], "a=P por STI/NTI/PTI"))

# 02 — TMIN e TMAX
def c02():
    c=Circuit(); a=c.add("Input",60,90,P); b=c.add("Input",60,230,N)
    mn=c.add("Min",280,90); mx=c.add("Max",280,230)
    o1=c.add("Output",500,90); o2=c.add("Output",500,230)
    for g in(mn,mx): c.wire(a,0,g,0); c.wire(b,0,g,1)
    c.wire(mn,0,o1,0); c.wire(mx,0,o2,0); return c
examples.append(("02-tmin-tmax.json", c02, lambda r: list(r.values())==[N,P], "MIN(P,N)=N, MAX(P,N)=P"))

# 03 — Consensus e Any
def c03():
    c=Circuit(); a=c.add("Input",60,90,P); b=c.add("Input",60,230,O)
    cs=c.add("Consensus",280,90); an=c.add("AnyGate",280,230)
    o1=c.add("Output",500,90); o2=c.add("Output",500,230)
    for g in(cs,an): c.wire(a,0,g,0); c.wire(b,0,g,1)
    c.wire(cs,0,o1,0); c.wire(an,0,o2,0); return c
examples.append(("03-consensus-any.json", c03, lambda r: list(r.values())==[O,P], "CONS(P,O)=O, ANY(P,O)=P"))

# 04 — Multiplicador de sinais
def c04():
    c=Circuit(); a=c.add("Input",60,90,N); b=c.add("Input",60,230,P)
    m=c.add("Mul",280,160); o=c.add("Output",500,160)
    c.wire(a,0,m,0); c.wire(b,0,m,1); c.wire(m,0,o,0); return c
examples.append(("04-multiplicador-sinais.json", c04, lambda r: list(r.values())==[N], "MUL(N,P)=N"))

# 05 — Shifters
def c05():
    c=Circuit(); a=c.add("Input",60,160,O)
    su=c.add("ShiftUp",280,90); sd=c.add("ShiftDown",280,230)
    o1=c.add("Output",500,90); o2=c.add("Output",500,230)
    for g,o in((su,o1),(sd,o2)): c.wire(a,0,g,0); c.wire(g,0,o,0)
    return c
examples.append(("05-shifters.json", c05, lambda r: list(r.values())==[P,N], "SH+(O)=P, SH-(O)=N"))

# 06 — Valor absoluto: |a| = MAX(a, TINV(a))
def c06():
    c=Circuit(); a=c.add("Input",60,160,N)
    inv=c.add("Not",260,240); mx=c.add("Max",460,160); o=c.add("Output",660,160)
    c.wire(a,0,inv,0); c.wire(a,0,mx,0); c.wire(inv,0,mx,1); c.wire(mx,0,o,0); return c
examples.append(("06-valor-absoluto.json", c06, lambda r: list(r.values())==[P], "|N| = P"))

# 07 — Meio-somador (a+b)
def c07():
    c=Circuit(); a=c.add("Input",60,80,P); b=c.add("Input",60,200,P); z=c.add("ConstZero",60,320)
    fad=c.add("FullAdder",300,150); s=c.add("Output",520,120); cy=c.add("Output",520,210)
    c.wire(a,0,fad,0); c.wire(b,0,fad,1); c.wire(z,0,fad,2)
    c.wire(fad,0,s,0); c.wire(fad,1,cy,0); return c
examples.append(("07-meio-somador.json", c07, lambda r: list(r.values())==[N,P], "P+P=+2 -> soma N, vai-um P"))

# 08 — Somador completo (a+b+cin)
def c08():
    c=Circuit(); a=c.add("Input",60,80,P); b=c.add("Input",60,200,P); ci=c.add("Input",60,320,P)
    fad=c.add("FullAdder",300,180); s=c.add("Output",520,150); cy=c.add("Output",520,240)
    c.wire(a,0,fad,0); c.wire(b,0,fad,1); c.wire(ci,0,fad,2)
    c.wire(fad,0,s,0); c.wire(fad,1,cy,0); return c
examples.append(("08-somador-completo.json", c08, lambda r: list(r.values())==[O,P], "P+P+P=+3 -> soma O, vai-um P"))

# 09 — Comparador de 1 trit: sign(a-b) = ANY(a, TINV(b))
def c09():
    c=Circuit(); a=c.add("Input",60,90,P); b=c.add("Input",60,230,O)
    inv=c.add("Not",260,230); an=c.add("AnyGate",460,160); o=c.add("Output",660,160)
    c.wire(b,0,inv,0); c.wire(a,0,an,0); c.wire(inv,0,an,1); c.wire(an,0,o,0); return c
examples.append(("09-comparador-1trit.json", c09, lambda r: list(r.values())==[P], "a>b -> P (P vs O)"))

# 10 — Mediana de 3: MAX(MAX(MIN(a,b),MIN(b,c)),MIN(c,a))
def c10():
    c=Circuit()
    a=c.add("Input",60,60,N); b=c.add("Input",60,180,O); cc=c.add("Input",60,300,P)
    m1=c.add("Min",260,90); m2=c.add("Min",260,210); m3=c.add("Min",260,330)
    x1=c.add("Max",470,140); x2=c.add("Max",670,210); o=c.add("Output",870,210)
    c.wire(a,0,m1,0); c.wire(b,0,m1,1)
    c.wire(b,0,m2,0); c.wire(cc,0,m2,1)
    c.wire(cc,0,m3,0); c.wire(a,0,m3,1)
    c.wire(m1,0,x1,0); c.wire(m2,0,x1,1)
    c.wire(x1,0,x2,0); c.wire(m3,0,x2,1); c.wire(x2,0,o,0); return c
examples.append(("10-mediana-de-3.json", c10, lambda r: list(r.values())==[O], "mediana(N,O,P)=O"))

# 11 — Somador de 2 trits (ripple): a(2t)+b(2t) -> s0,s1,carry
def c11():
    c=Circuit()
    a0=c.add("Input",60,40,P); a1=c.add("Input",60,120,O)
    b0=c.add("Input",60,220,P); b1=c.add("Input",60,300,O)
    z=c.add("ConstZero",60,380)
    f0=c.add("FullAdder",300,90); f1=c.add("FullAdder",300,260)
    s0=c.add("Output",560,60); s1=c.add("Output",560,150); cy=c.add("Output",560,260)
    c.wire(a0,0,f0,0); c.wire(b0,0,f0,1); c.wire(z,0,f0,2)
    c.wire(a1,0,f1,0); c.wire(b1,0,f1,1); c.wire(f0,1,f1,2)
    c.wire(f0,0,s0,0); c.wire(f1,0,s1,0); c.wire(f1,1,cy,0); return c
# 1 + 1 = 2 = [N,P]: a=[P,O]=1, b=[P,O]=1 -> s0=N,s1=P,carry O
examples.append(("11-somador-2-trits.json", c11, lambda r: list(r.values())==[N,P,O], "1+1=2 -> [N,P], carry O"))

# 12 — Somador de 3 trits
def c12():
    c=Circuit(); z=c.add("ConstZero",60,470)
    a=[c.add("Input",60,40+k*70,v) for k,v in enumerate([P,O,O])]   # 1
    b=[c.add("Input",60,260+k*70,v) for k,v in enumerate([N,P,O])]  # 2
    f=[c.add("FullAdder",320,60+k*120) for k in range(3)]
    outs=[]
    for k in range(3):
        c.wire(a[k],0,f[k],0); c.wire(b[k],0,f[k],1)
        c.wire(z,0,f[k],2) if k==0 else c.wire(f[k-1],1,f[k],2)
        s=c.add("Output",580,60+k*120); c.wire(f[k],0,s,0); outs.append(s)
    cy=c.add("Output",580,60+3*120); c.wire(f[2],1,cy,0); outs.append(cy)
    return c
# 1 + 2 = 3 = [O,P,O], carry O
examples.append(("12-somador-3-trits.json", c12, lambda r: list(r.values())==[O,P,O,O], "1+2=3 -> [O,P,O], carry O"))

# 13 — Lei de De Morgan: NOT(MIN(a,b)) == MAX(NOT a, NOT b)
def c13():
    c=Circuit(); a=c.add("Input",60,90,P); b=c.add("Input",60,250,N)
    mn=c.add("Min",260,120); nmn=c.add("Not",460,120)
    na=c.add("Not",260,250); nb=c.add("Not",260,330); mx=c.add("Max",460,290)
    o1=c.add("Output",660,120); o2=c.add("Output",660,290)
    c.wire(a,0,mn,0); c.wire(b,0,mn,1); c.wire(mn,0,nmn,0); c.wire(nmn,0,o1,0)
    c.wire(a,0,na,0); c.wire(b,0,nb,0); c.wire(na,0,mx,0); c.wire(nb,0,mx,1); c.wire(mx,0,o2,0)
    return c
examples.append(("13-de-morgan.json", c13, lambda r: len(set(r.values()))==1, "os dois lados são iguais"))

# 14 — Identidade: NOT(NOT(a)) = a
def c14():
    c=Circuit(); a=c.add("Input",60,160,P)
    n1=c.add("Not",260,160); n2=c.add("Not",460,160); o=c.add("Output",660,160)
    c.wire(a,0,n1,0); c.wire(n1,0,n2,0); c.wire(n2,0,o,0); return c
examples.append(("14-identidade-not-not.json", c14, lambda r: list(r.values())==[P], "NOT(NOT(P))=P"))

# 15 — Oscilador (instável): NOT realimentado em si mesmo
def c15():
    c=Circuit(); n=c.add("Not",300,160); o=c.add("Output",520,160)
    c.wire(n,0,n,0)   # realimentação
    c.wire(n,0,o,0); return c
examples.append(("15-oscilador-instavel.json", c15, None, "loop de NOT — não estabiliza (curiosidade)"))

# ---- gerar + verificar ----
fails=0
print("Gerando e verificando exemplos:\n")
for fname, builder, check, desc in examples:
    c=builder()
    res=c.simulate()
    label=" ".join(sym(v) for v in res.values())
    ok="—"
    if check is not None:
        passed=check(res); ok="OK" if passed else "FALHOU"
        if not passed: fails+=1
    with open(os.path.join(OUT_DIR,fname),"w",encoding="utf-8") as fp:
        json.dump(c.dump(), fp, ensure_ascii=False, indent=2)
    print(f"  [{ok:6}] {fname:34} saídas: {label or '(loop)':10} — {desc}")

print()
print("TODOS OS EXEMPLOS VERIFICADOS ✓" if fails==0 else f"{fails} EXEMPLO(S) COM PROBLEMA ✗")
sys.exit(0 if fails==0 else 1)
