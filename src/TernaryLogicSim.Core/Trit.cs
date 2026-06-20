// =============================================================================
//  Trit.cs  —  O tipo fundamental da lógica TERNÁRIA BALANCEADA
// =============================================================================
//
//  No mundo BINÁRIO um sinal é um bit: 0 ou 1.
//  No mundo TERNÁRIO BALANCEADO um sinal é um "trit", que assume TRÊS valores:
//
//        -1  (Negativo)   símbolo: N   cor sugerida: vermelho
//         0  (Neutro)     símbolo: O   cor sugerida: apagado/cinza
//        +1  (Positivo)   símbolo: P   cor sugerida: verde
//
//  Por que "balanceado"? Porque os valores são simétricos em torno do zero
//  (-1, 0, +1). Isso traz uma propriedade linda: NEGAR um número é simplesmente
//  inverter o sinal de cada trit — não existe "bit de sinal" separado como no
//  binário (complemento de dois). Subtrair é somar o negativo. Elegante.
//
//  Guardamos também um quarto estado, Disconnected (flutuante), para representar
//  um pino que não está sendo acionado por nada — exatamente como o
//  Digital-Logic-Sim original tem o estado "desconectado" além de 0/1.
//
//  ESTE ARQUIVO NÃO DEPENDE DE UnityEngine de propósito: assim o coração da
//  simulação pode ser compilado e testado fora do Unity (e os algoritmos aqui
//  foram validados por testes exaustivos — veja a pasta /Tests).
// =============================================================================

namespace TernaryLogicSim.Simulation
{
    /// <summary>
    /// Um único dígito ternário balanceado. Os valores numéricos do enum (0,1,2,3)
    /// são escolhidos para caber em 2 bits, o que permite empacotar vários trits
    /// dentro de um inteiro (veja <see cref="TritBus"/>).
    /// </summary>
    public enum Trit : byte
    {
        Negative     = 0, // -1  (N)
        Zero         = 1, //  0  (O)  — também o valor "neutro / desligado" visual
        Positive     = 2, // +1  (P)
        Disconnected = 3  // flutuante: nenhum sinal acionando o pino
    }

    /// <summary>
    /// Métodos utilitários para converter entre <see cref="Trit"/> e inteiros com sinal.
    /// São métodos de extensão, então você escreve <c>meuTrit.ToInt()</c>.
    /// </summary>
    public static class TritOps
    {
        /// <summary>
        /// Converte o trit para seu valor numérico com sinal (-1, 0 ou +1).
        /// Disconnected é tratado como 0 para fins aritméticos.
        /// </summary>
        public static int ToInt(this Trit t)
        {
            switch (t)
            {
                case Trit.Negative: return -1;
                case Trit.Positive: return +1;
                default:            return 0; // Zero e Disconnected
            }
        }

        /// <summary>
        /// Cria um trit a partir de um inteiro: negativo → N, positivo → P, zero → O.
        /// Útil para "normalizar" o resultado de uma conta de volta para um trit.
        /// </summary>
        public static Trit FromInt(int value)
        {
            if (value < 0) return Trit.Negative;
            if (value > 0) return Trit.Positive;
            return Trit.Zero;
        }

        /// <summary>Símbolo de uma letra para exibição/depuração: N, O ou P.</summary>
        public static char Symbol(this Trit t)
        {
            switch (t)
            {
                case Trit.Negative:     return 'N';
                case Trit.Positive:     return 'P';
                case Trit.Disconnected: return '_';
                default:                return 'O';
            }
        }

        /// <summary>Um pino desconectado conta como Zero quando precisamos de um valor lógico.</summary>
        public static Trit Resolve(this Trit t) =>
            t == Trit.Disconnected ? Trit.Zero : t;
    }
}
