// =============================================================================
//  TritBus.cs  —  Empacotamento de vários trits dentro de um inteiro
// =============================================================================
//
//  POR QUE EMPACOTAR?
//  O Digital-Logic-Sim original guarda o estado de muitos pinos dentro de
//  inteiros, manipulados com operações de bit (AND, OR, shift). Isso é MUITO
//  mais rápido do que um array de objetos, porque o processador trabalha com
//  inteiros nativamente e tudo cabe em poucos bytes contíguos na memória.
//
//  Aqui fazemos o mesmo, adaptado ao ternário: como cada trit precisa de
//  4 estados (N, O, P, Disconnected), usamos 2 BITS por trit.
//
//      ulong  = 64 bits  ->  64 / 2 = 32 trits por barramento.
//
//  Layout (índice 0 são os 2 bits menos significativos):
//
//      bit:  ... 7 6 | 5 4 | 3 2 | 1 0
//      trit: ...  [3] | [2] | [1] | [0]
//
//  Isto é o equivalente ternário do "BitState"/máscaras do projeto original.
// =============================================================================

namespace TernaryLogicSim.Simulation
{
    /// <summary>
    /// Conjunto de até 32 trits empacotados em um ulong (2 bits cada).
    /// É uma struct (tipo de valor) por performance: copiar é barato e não
    /// gera lixo para o coletor.
    /// </summary>
    public struct TritBus
    {
        public ulong Packed;

        private const int BitsPerTrit = 2;
        private const ulong TritMask  = 0b11;          // máscara de 2 bits
        public const int Capacity     = 64 / BitsPerTrit; // 32 trits

        public TritBus(ulong packed) { Packed = packed; }

        /// <summary>Lê o trit na posição <paramref name="index"/> (0..31).</summary>
        public Trit Get(int index)
        {
            int shift = index * BitsPerTrit;
            return (Trit)((Packed >> shift) & TritMask);
        }

        /// <summary>Escreve <paramref name="value"/> na posição <paramref name="index"/> (0..31).</summary>
        public void Set(int index, Trit value)
        {
            int shift = index * BitsPerTrit;
            Packed &= ~(TritMask << shift);                 // 1) limpa os 2 bits do slot
            Packed |= ((ulong)value & TritMask) << shift;   // 2) grava o novo valor
        }

        /// <summary>Preenche todo o barramento com o mesmo trit (útil para reset).</summary>
        public void Fill(Trit value, int count)
        {
            for (int i = 0; i < count; i++) Set(i, value);
        }

        /// <summary>Representação legível, ex.: "P O N" (índice 0 à esquerda).</summary>
        public string ToString(int count)
        {
            var chars = new char[count == 0 ? 0 : count * 2 - 1];
            for (int i = 0; i < count; i++)
            {
                chars[i * 2] = Get(i).Symbol();
                if (i < count - 1) chars[i * 2 + 1] = ' ';
            }
            return new string(chars);
        }
    }
}
