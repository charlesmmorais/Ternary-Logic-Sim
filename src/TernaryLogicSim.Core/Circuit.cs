// =============================================================================
//  Circuit.cs  —  O grafo de simulação (chips + fios) e seu avaliador
// =============================================================================
//
//  Um circuito é um GRAFO:
//    • NÓS  = instâncias de chips primitivos (uma porta NOT, um full adder...).
//    • FIOS = conexões que copiam o valor de um pino de SAÍDA para um pino de
//             ENTRADA de outro nó.
//    • Pinos de ENTRADA do circuito (o que o usuário liga/desliga).
//    • Pinos de SAÍDA do circuito (os "LEDs" que mostram o resultado).
//
//  COMO AVALIAMOS?
//  Por "ponto fixo iterativo": começamos com tudo em estado conhecido, depois
//  recalculamos todos os nós repetidamente até nada mais mudar (estabilizar).
//  Essa abordagem lida naturalmente tanto com circuitos COMBINACIONAIS quanto
//  com REALIMENTAÇÃO (memória/sequencial), exatamente como um simulador real
//  precisa. Limitamos o número de iterações para detectar oscilação.
//
//  Esta classe é C# puro (sem UnityEngine) → testável fora do Unity.
// =============================================================================

using System;
using System.Collections.Generic;

namespace TernaryLogicSim.Simulation
{
    /// <summary>Referência a um pino: o nó dono e o índice do pino nele.</summary>
    public readonly struct PinRef
    {
        public readonly int NodeId;   // -1 = pino externo do circuito
        public readonly int PinIndex;
        public PinRef(int nodeId, int pinIndex) { NodeId = nodeId; PinIndex = pinIndex; }
    }

    /// <summary>Uma instância de chip primitivo dentro do circuito.</summary>
    public sealed class Node
    {
        public readonly int Id;
        public readonly BuiltinChipType Type;
        public readonly Trit[] Inputs;
        public readonly Trit[] Outputs;

        public Node(int id, BuiltinChipType type)
        {
            Id = id;
            Type = type;
            Inputs  = new Trit[BuiltinChip.InputCount(type)];
            Outputs = new Trit[BuiltinChip.OutputCount(type)];
            Array.Fill(Inputs,  Trit.Zero);
            Array.Fill(Outputs, Trit.Zero);
        }

        public void Evaluate() => BuiltinChip.Evaluate(Type, Inputs, Outputs);
    }

    /// <summary>Um fio: leva o valor de <see cref="From"/> (saída) até <see cref="To"/> (entrada).</summary>
    public readonly struct Connection
    {
        public readonly PinRef From; // pino de SAÍDA (de um nó, ou entrada externa)
        public readonly PinRef To;   // pino de ENTRADA (de um nó, ou saída externa)
        public Connection(PinRef from, PinRef to) { From = from; To = to; }
    }

    public sealed class Circuit
    {
        private readonly List<Node> _nodes = new();
        private readonly List<Connection> _connections = new();

        /// <summary>Estado atual dos pinos de entrada externos (acionados pelo usuário).</summary>
        public Trit[] InputPins;

        /// <summary>Estado atual dos pinos de saída externos (resultado/LEDs).</summary>
        public Trit[] OutputPins;

        public IReadOnlyList<Node> Nodes => _nodes;

        public Circuit(int externalInputs, int externalOutputs)
        {
            InputPins  = new Trit[externalInputs];
            OutputPins = new Trit[externalOutputs];
            Array.Fill(InputPins,  Trit.Zero);
            Array.Fill(OutputPins, Trit.Zero);
        }

        /// <summary>Adiciona um chip primitivo e devolve seu id (índice do nó).</summary>
        public int AddNode(BuiltinChipType type)
        {
            int id = _nodes.Count;
            _nodes.Add(new Node(id, type));
            return id;
        }

        /// <summary>Liga uma saída a uma entrada. Use NodeId = -1 para pinos externos.</summary>
        public void Connect(PinRef from, PinRef to) =>
            _connections.Add(new Connection(from, to));

        public void SetInput(int index, Trit value) => InputPins[index] = value;
        public Trit GetOutput(int index) => OutputPins[index];

        /// <summary>
        /// Lê o valor "fonte" de uma conexão: ou um pino de entrada externo
        /// (NodeId == -1) ou uma saída de um nó.
        /// </summary>
        private Trit ReadSource(PinRef src) =>
            src.NodeId == -1 ? InputPins[src.PinIndex] : _nodes[src.NodeId].Outputs[src.PinIndex];

        /// <summary>
        /// Roda a simulação até estabilizar (ponto fixo) ou até atingir
        /// <paramref name="maxIterations"/>. Devolve o número de iterações usadas;
        /// se igual a maxIterations, o circuito pode estar oscilando.
        /// </summary>
        public int Run(int maxIterations = 100)
        {
            for (int iter = 1; iter <= maxIterations; iter++)
            {
                bool changed = false;

                // 1) Propaga os fios: copia cada fonte para o destino.
                foreach (var c in _connections)
                {
                    Trit value = ReadSource(c.From);
                    if (c.To.NodeId == -1)
                    {
                        if (OutputPins[c.To.PinIndex] != value)
                        {
                            OutputPins[c.To.PinIndex] = value;
                            changed = true;
                        }
                    }
                    else
                    {
                        var dst = _nodes[c.To.NodeId].Inputs;
                        if (dst[c.To.PinIndex] != value)
                        {
                            dst[c.To.PinIndex] = value;
                            changed = true;
                        }
                    }
                }

                // 2) Reavalia todos os nós a partir das novas entradas.
                foreach (var node in _nodes)
                {
                    var before0 = node.Outputs.Length > 0 ? node.Outputs[0] : Trit.Zero;
                    node.Evaluate();
                    if (node.Outputs.Length > 0 && node.Outputs[0] != before0) changed = true;
                }

                if (!changed) return iter; // estabilizou
            }
            return maxIterations; // não convergiu (possível oscilação)
        }
    }
}
