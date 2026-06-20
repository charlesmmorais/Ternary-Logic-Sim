// =============================================================================
//  TernaryGates.cs  —  As PORTAS LÓGICAS primitivas do ternário
// =============================================================================
//
//  No binário você conhece AND, OR, NOT. No ternário balanceado os equivalentes
//  mais naturais são MIN, MAX e NOT (negação). A partir DESTES três você
//  consegue construir qualquer outra função ternária — eles são um conjunto
//  funcionalmente completo (com a ajuda das constantes N/O/P).
//
//  Tabelas-verdade (linhas = A, colunas = B), valores em {N=-1, O=0, P=+1}:
//
//    NOT (negação)            MIN  (≈ AND)            MAX  (≈ OR)
//    A | NOT A                 | N O P                  | N O P
//   ---+------               --+------               --+------
//    N |  P                   N | N N N                N | N O P
//    O |  O                   O | N O O                O | O O P
//    P |  N                   P | N O P                P | P P P
//
//  MIN devolve o MENOR dos dois; MAX devolve o MAIOR. Simples assim.
//
//  Estas são funções PURAS (sem estado, sem efeitos colaterais). Por isso são
//  triviais de testar — e foram validadas exaustivamente (9 combinações cada).
// =============================================================================

using System;

namespace TernaryLogicSim.Simulation
{
    public static class TernaryGates
    {
        // ---------------------------------------------------------------------
        //  Operadores UNÁRIOS (uma entrada)
        // ---------------------------------------------------------------------

        /// <summary>
        /// STI — Standard Ternary Inverter (o "TINV" padrão / negação balanceada):
        /// N↔P, O fica O. É o inversor mais usado.
        /// </summary>
        public static Trit Not(Trit a)
        {
            switch (a.Resolve())
            {
                case Trit.Negative: return Trit.Positive;
                case Trit.Positive: return Trit.Negative;
                default:            return Trit.Zero;
            }
        }

        /// <summary>
        /// NTI — Negative Ternary Inverter: N→P, O→N, P→N.
        /// (Só fica positivo quando a entrada é a mais baixa.)
        /// </summary>
        public static Trit Nti(Trit a) => a.Resolve() == Trit.Negative ? Trit.Positive : Trit.Negative;

        /// <summary>
        /// PTI — Positive Ternary Inverter: N→P, O→P, P→N.
        /// (Só fica negativo quando a entrada é a mais alta.)
        /// </summary>
        public static Trit Pti(Trit a) => a.Resolve() == Trit.Positive ? Trit.Negative : Trit.Positive;

        /// <summary>Sobe um nível com saturação: N→O, O→P, P→P.</summary>
        public static Trit ShiftUp(Trit a) =>
            TritOps.FromInt(Math.Min(1, a.ToInt() + 1));

        /// <summary>Desce um nível com saturação: P→O, O→N, N→N.</summary>
        public static Trit ShiftDown(Trit a) =>
            TritOps.FromInt(Math.Max(-1, a.ToInt() - 1));

        /// <summary>Operador "é positivo?": devolve P se a=P, senão N. (decodificador)</summary>
        public static Trit IsPositive(Trit a) => a.Resolve() == Trit.Positive ? Trit.Positive : Trit.Negative;

        /// <summary>Operador "é zero?": devolve P se a=O, senão N.</summary>
        public static Trit IsZero(Trit a) => a.Resolve() == Trit.Zero ? Trit.Positive : Trit.Negative;

        /// <summary>Operador "é negativo?": devolve P se a=N, senão N.</summary>
        public static Trit IsNegative(Trit a) => a.Resolve() == Trit.Negative ? Trit.Positive : Trit.Negative;

        // ---------------------------------------------------------------------
        //  Operadores BINÁRIOS (duas entradas)
        // ---------------------------------------------------------------------

        /// <summary>MIN (≈ AND): o menor dos dois trits.</summary>
        public static Trit Min(Trit a, Trit b) =>
            TritOps.FromInt(Math.Min(a.ToInt(), b.ToInt()));

        /// <summary>MAX (≈ OR): o maior dos dois trits.</summary>
        public static Trit Max(Trit a, Trit b) =>
            TritOps.FromInt(Math.Max(a.ToInt(), b.ToInt()));

        /// <summary>NMIN (≈ NAND): negação do MIN.</summary>
        public static Trit Nmin(Trit a, Trit b) => Not(Min(a, b));

        /// <summary>NMAX (≈ NOR): negação do MAX.</summary>
        public static Trit Nmax(Trit a, Trit b) => Not(Max(a, b));

        /// <summary>
        /// CONSENSO: P se ambos P, N se ambos N, caso contrário O.
        /// É a "concordância forte" entre dois trits.
        /// </summary>
        public static Trit Consensus(Trit a, Trit b)
        {
            var ra = a.Resolve();
            var rb = b.Resolve();
            if (ra == Trit.Positive && rb == Trit.Positive) return Trit.Positive;
            if (ra == Trit.Negative && rb == Trit.Negative) return Trit.Negative;
            return Trit.Zero;
        }

        /// <summary>
        /// "ANY" (qualquer): P se algum for P (e nenhum N), N se algum for N (e nenhum P),
        /// O se empatar ou ambos O. É a soma saturada de dois trits.
        /// </summary>
        public static Trit Any(Trit a, Trit b) =>
            TritOps.FromInt(Math.Sign(a.ToInt() + b.ToInt()));

        /// <summary>
        /// PRODUTO ternário (multiplicação de sinais): N·P = N, N·N = P, qualquer·O = O.
        /// Equivale ao XNOR balanceado e é a base da multiplicação numérica.
        /// </summary>
        public static Trit Mul(Trit a, Trit b) =>
            TritOps.FromInt(a.ToInt() * b.ToInt());
    }
}
