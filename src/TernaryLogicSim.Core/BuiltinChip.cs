// =============================================================================
//  BuiltinChip.cs  —  Os chips PRIMITIVOS embutidos do simulador
// =============================================================================
//
//  Assim como no Digital-Logic-Sim original só um punhado de chips têm lógica
//  "hard-coded" em C# (e todo o resto é composição visual), aqui definimos os
//  primitivos ternários. Tudo o que o usuário construir no editor será feito
//  combinando ESTES blocos.
//
//  Cada built-in declara quantas entradas e saídas tem, e como calcular as
//  saídas a partir das entradas (uma função pura sobre trits).
// =============================================================================

using System;

namespace TernaryLogicSim.Simulation
{
    /// <summary>Identifica cada chip primitivo embutido.</summary>
    public enum BuiltinChipType
    {
        // Fontes / constantes
        ConstNeg,   // saída fixa N
        ConstZero,  // saída fixa O
        ConstPos,   // saída fixa P

        // Unários
        Not,        // negação balanceada
        Nti,        // NTI — Negative Ternary Inverter
        Pti,        // PTI — Positive Ternary Inverter
        ShiftUp,    // N→O→P (satura)
        ShiftDown,  // P→O→N (satura)

        // Binários
        Min,        // ≈ AND
        Max,        // ≈ OR
        Nmin,       // ≈ NAND
        Nmax,       // ≈ NOR
        Consensus,  // concordância forte
        AnyGate,    // soma saturada
        Mul,        // produto de sinais (≈ XNOR balanceado)

        // Aritmético
        FullAdder   // 3 entradas (a, b, carryIn) → 2 saídas (sum, carryOut)
    }

    /// <summary>
    /// Metadados + função de avaliação de cada chip embutido. Estático e sem
    /// estado: o mesmo definidor serve para todas as instâncias.
    /// </summary>
    public static class BuiltinChip
    {
        public static int InputCount(BuiltinChipType type)
        {
            switch (type)
            {
                case BuiltinChipType.ConstNeg:
                case BuiltinChipType.ConstZero:
                case BuiltinChipType.ConstPos:
                    return 0;
                case BuiltinChipType.Not:
                case BuiltinChipType.Nti:
                case BuiltinChipType.Pti:
                case BuiltinChipType.ShiftUp:
                case BuiltinChipType.ShiftDown:
                    return 1;
                case BuiltinChipType.FullAdder:
                    return 3;
                default:
                    return 2; // todos os binários
            }
        }

        public static int OutputCount(BuiltinChipType type) =>
            type == BuiltinChipType.FullAdder ? 2 : 1;

        public static string DisplayName(BuiltinChipType type) => type.ToString().ToUpperInvariant();

        /// <summary>
        /// Avalia o chip: lê <paramref name="inputs"/> e escreve <paramref name="outputs"/>.
        /// Os arrays já vêm com o tamanho certo (InputCount/OutputCount).
        /// </summary>
        public static void Evaluate(BuiltinChipType type, Trit[] inputs, Trit[] outputs)
        {
            switch (type)
            {
                case BuiltinChipType.ConstNeg:   outputs[0] = Trit.Negative; break;
                case BuiltinChipType.ConstZero:  outputs[0] = Trit.Zero;     break;
                case BuiltinChipType.ConstPos:   outputs[0] = Trit.Positive; break;

                case BuiltinChipType.Not:        outputs[0] = TernaryGates.Not(inputs[0]);       break;
                case BuiltinChipType.Nti:        outputs[0] = TernaryGates.Nti(inputs[0]);       break;
                case BuiltinChipType.Pti:        outputs[0] = TernaryGates.Pti(inputs[0]);       break;
                case BuiltinChipType.ShiftUp:    outputs[0] = TernaryGates.ShiftUp(inputs[0]);   break;
                case BuiltinChipType.ShiftDown:  outputs[0] = TernaryGates.ShiftDown(inputs[0]); break;

                case BuiltinChipType.Min:        outputs[0] = TernaryGates.Min(inputs[0], inputs[1]);       break;
                case BuiltinChipType.Max:        outputs[0] = TernaryGates.Max(inputs[0], inputs[1]);       break;
                case BuiltinChipType.Nmin:       outputs[0] = TernaryGates.Nmin(inputs[0], inputs[1]);      break;
                case BuiltinChipType.Nmax:       outputs[0] = TernaryGates.Nmax(inputs[0], inputs[1]);      break;
                case BuiltinChipType.Consensus:  outputs[0] = TernaryGates.Consensus(inputs[0], inputs[1]); break;
                case BuiltinChipType.AnyGate:    outputs[0] = TernaryGates.Any(inputs[0], inputs[1]);       break;
                case BuiltinChipType.Mul:        outputs[0] = TernaryGates.Mul(inputs[0], inputs[1]);       break;

                case BuiltinChipType.FullAdder:
                {
                    var (sum, carry) = TernaryArithmetic.FullAdder(inputs[0], inputs[1], inputs[2]);
                    outputs[0] = sum;
                    outputs[1] = carry;
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Chip embutido desconhecido");
            }
        }
    }
}
