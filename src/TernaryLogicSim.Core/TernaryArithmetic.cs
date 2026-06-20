// =============================================================================
//  TernaryArithmetic.cs  —  Aritmética em ternário balanceado
// =============================================================================
//
//  Aqui mostramos a grande recompensa do ternário balanceado: a ARITMÉTICA.
//
//  Um número é escrito como uma sequência de trits, cada um valendo uma potência
//  de 3 (em vez de potência de 2). Como cada trit vale -1, 0 ou +1:
//
//      valor = Σ  trit_i * 3^i
//
//  Exemplos (trit menos significativo à esquerda):
//      [P]            =  1
//      [N P]          = -1 + 1*3        =  2
//      [O P]          =  0 + 1*3        =  3
//      [P N]          =  1 + (-1)*3     = -2
//
//  Repare que NÃO existe sinal separado: um número negativo é só um número cujo
//  trit mais significativo é N. Negar = inverter todos os trits. Lindo.
//
//  O somador completo (full adder) abaixo é o tijolo de toda a aritmética, do
//  mesmo jeito que o full adder binário é no Digital-Logic-Sim original.
// =============================================================================

using System.Collections.Generic;

namespace TernaryLogicSim.Simulation
{
    public static class TernaryArithmetic
    {
        /// <summary>
        /// SOMADOR COMPLETO TERNÁRIO.
        /// Soma três trits (a + b + vai-um de entrada) e devolve a soma e o
        /// vai-um de saída, ambos já normalizados para a faixa {N, O, P}.
        ///
        /// A soma bruta está em [-3, +3]. Normalizamos somando/subtraindo 3
        /// (que vale "1" na próxima casa) e ajustando o vai-um.
        /// </summary>
        public static (Trit sum, Trit carryOut) FullAdder(Trit a, Trit b, Trit carryIn)
        {
            int total = a.ToInt() + b.ToInt() + carryIn.ToInt(); // -3..+3

            int carry = 0;
            while (total > 1)  { total -= 3; carry += 1; } // ex.: +2 → soma N, vai-um P
            while (total < -1) { total += 3; carry -= 1; } // ex.: -2 → soma P, vai-um N

            return (TritOps.FromInt(total), TritOps.FromInt(carry));
        }

        /// <summary>
        /// Converte um inteiro com sinal para sua representação em ternário
        /// balanceado, do trit menos significativo para o mais significativo.
        /// </summary>
        public static List<Trit> FromInt(int value)
        {
            var trits = new List<Trit>();
            if (value == 0) { trits.Add(Trit.Zero); return trits; }

            while (value != 0)
            {
                // Resto truncado do C# fica em {-2..2}; normalizamos o TRIT para
                // {-1, 0, +1} (ternário balanceado).
                int rem = value % 3; // C#: -2..2 (trunca em direção ao zero)
                int t;
                if (rem == 2)       t = -1; // 2  ≡ -1 (vai-um +1)
                else if (rem == -2) t = +1; // -2 ≡ +1 (vai-um -1)
                else                t = rem; // -1, 0, +1

                trits.Add(TritOps.FromInt(t));
                // (value - t) é múltiplo exato de 3 → divisão sem erro de truncamento.
                value = (value - t) / 3;
            }
            return trits;
        }

        /// <summary>
        /// Converte uma sequência de trits (menos significativo primeiro) de volta
        /// para um inteiro com sinal.
        /// </summary>
        public static int ToInt(IReadOnlyList<Trit> trits)
        {
            int value = 0;
            int power = 1; // 3^0
            for (int i = 0; i < trits.Count; i++)
            {
                value += trits[i].ToInt() * power;
                power *= 3;
            }
            return value;
        }

        /// <summary>
        /// Soma dois números ternários (ripple-carry), retornando um resultado com
        /// <paramref name="width"/> trits. Demonstra o full adder em cadeia.
        /// </summary>
        public static List<Trit> Add(IReadOnlyList<Trit> a, IReadOnlyList<Trit> b, int width)
        {
            var result = new List<Trit>(width);
            Trit carry = Trit.Zero;
            for (int i = 0; i < width; i++)
            {
                Trit ta = i < a.Count ? a[i] : Trit.Zero;
                Trit tb = i < b.Count ? b[i] : Trit.Zero;
                var (sum, c) = FullAdder(ta, tb, carry);
                result.Add(sum);
                carry = c;
            }
            return result;
        }
    }
}
