// =============================================================================
//  EditorModel.cs  —  Modelo do EDITOR + chips COMPOSTOS + avaliador ao vivo
// =============================================================================
//
//  Aqui vive o "cérebro" do editor visual:
//    • EditorChip  — um chip colocado na tela (primitivo OU composto).
//    • EditorWire  — um fio entre pinos.
//    • EditorGraph — o grafo + avaliador por ponto fixo.
//    • ChipLibrary / CompositeDefinition — o recurso mais "DLS": agrupar um
//      circuito inteiro num NOVO chip reutilizável (que pode conter outros
//      chips compostos — a avaliação é recursiva).
//
//  Toda a "verdade" ternária continua vindo do núcleo (BuiltinChip.Evaluate).
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using TernaryLogicSim.Simulation;

namespace TernaryLogicSim.App.Editor
{
    public enum EditorChipKind
    {
        Input, Output,
        Not, Nti, Pti, ShiftUp, ShiftDown,
        Min, Max, Nmin, Nmax, Consensus, AnyGate, Mul,
        FullAdder,
        ConstNeg, ConstZero, ConstPos,
        Composite                       // instância de um chip definido pelo usuário
    }

    public static class ChipMeta
    {
        public static int InputCount(EditorChipKind k) => k switch
        {
            EditorChipKind.Input     => 0,
            EditorChipKind.Output    => 1,
            EditorChipKind.Not       => 1,
            EditorChipKind.Nti       => 1,
            EditorChipKind.Pti       => 1,
            EditorChipKind.ShiftUp   => 1,
            EditorChipKind.ShiftDown => 1,
            EditorChipKind.FullAdder => 3,
            EditorChipKind.ConstNeg  => 0,
            EditorChipKind.ConstZero => 0,
            EditorChipKind.ConstPos  => 0,
            EditorChipKind.Composite => 0, // definido por instância
            _                        => 2  // binários
        };

        public static int OutputCount(EditorChipKind k) => k switch
        {
            EditorChipKind.Output    => 0,
            EditorChipKind.FullAdder => 2,
            EditorChipKind.Composite => 0, // definido por instância
            _                        => 1
        };

        // Rótulo desenhado no corpo do chip (nomes da convenção ternária).
        public static string Label(EditorChipKind k) => k switch
        {
            EditorChipKind.Input     => "IN",
            EditorChipKind.Output    => "OUT",
            EditorChipKind.Not       => "TINV",  // STI (inversor padrão)
            EditorChipKind.Nti       => "NTI",
            EditorChipKind.Pti       => "PTI",
            EditorChipKind.Min       => "TMIN",
            EditorChipKind.Max       => "TMAX",
            EditorChipKind.Nmin      => "NMIN",
            EditorChipKind.Nmax      => "NMAX",
            EditorChipKind.AnyGate   => "ANY",
            EditorChipKind.FullAdder => "ADD",
            EditorChipKind.ConstNeg  => "N",
            EditorChipKind.ConstZero => "O",
            EditorChipKind.ConstPos  => "P",
            EditorChipKind.ShiftUp   => "SH+",
            EditorChipKind.ShiftDown => "SH-",
            _                        => k.ToString().ToUpperInvariant()
        };

        public static bool TryToBuiltin(EditorChipKind k, out BuiltinChipType bt)
        {
            switch (k)
            {
                case EditorChipKind.Not:       bt = BuiltinChipType.Not;       return true;
                case EditorChipKind.Nti:       bt = BuiltinChipType.Nti;       return true;
                case EditorChipKind.Pti:       bt = BuiltinChipType.Pti;       return true;
                case EditorChipKind.ShiftUp:   bt = BuiltinChipType.ShiftUp;   return true;
                case EditorChipKind.ShiftDown: bt = BuiltinChipType.ShiftDown; return true;
                case EditorChipKind.Min:       bt = BuiltinChipType.Min;       return true;
                case EditorChipKind.Max:       bt = BuiltinChipType.Max;       return true;
                case EditorChipKind.Nmin:      bt = BuiltinChipType.Nmin;      return true;
                case EditorChipKind.Nmax:      bt = BuiltinChipType.Nmax;      return true;
                case EditorChipKind.Consensus: bt = BuiltinChipType.Consensus; return true;
                case EditorChipKind.AnyGate:   bt = BuiltinChipType.AnyGate;   return true;
                case EditorChipKind.Mul:       bt = BuiltinChipType.Mul;       return true;
                case EditorChipKind.FullAdder: bt = BuiltinChipType.FullAdder; return true;
                case EditorChipKind.ConstNeg:  bt = BuiltinChipType.ConstNeg;  return true;
                case EditorChipKind.ConstZero: bt = BuiltinChipType.ConstZero; return true;
                case EditorChipKind.ConstPos:  bt = BuiltinChipType.ConstPos;  return true;
                default:                       bt = BuiltinChipType.Not;       return false;
            }
        }
    }

    public sealed class EditorChip
    {
        public int Id;
        public EditorChipKind Kind;
        public string CompositeName;     // só para Kind == Composite
        public double X, Y;
        public Trit[] Inputs;
        public Trit[] Outputs;

        // Construtor para chips PRIMITIVOS.
        public EditorChip(int id, EditorChipKind kind, double x, double y)
        {
            Id = id; Kind = kind; X = x; Y = y;
            Inputs  = Fill(ChipMeta.InputCount(kind));
            Outputs = Fill(ChipMeta.OutputCount(kind));
        }

        // Construtor para uma instância de chip COMPOSTO (contagens vêm da definição).
        public EditorChip(int id, string compositeName, int inCount, int outCount, double x, double y)
        {
            Id = id; Kind = EditorChipKind.Composite; CompositeName = compositeName; X = x; Y = y;
            Inputs  = Fill(inCount);
            Outputs = Fill(outCount);
        }

        public string Label =>
            Kind == EditorChipKind.Composite ? (CompositeName ?? "?") : ChipMeta.Label(Kind);

        private static Trit[] Fill(int n)
        {
            var a = new Trit[n];
            for (int i = 0; i < n; i++) a[i] = Trit.Zero;
            return a;
        }

        public void CycleInput()
        {
            if (Kind != EditorChipKind.Input) return;
            Outputs[0] = Outputs[0] switch
            {
                Trit.Negative => Trit.Zero,
                Trit.Zero     => Trit.Positive,
                _             => Trit.Negative
            };
        }
    }

    public sealed class EditorWire
    {
        public int FromChip, FromPin;
        public int ToChip, ToPin;
        public EditorWire(int fc, int fp, int tc, int tp)
        { FromChip = fc; FromPin = fp; ToChip = tc; ToPin = tp; }
    }

    // =========================================================================
    //  Biblioteca de chips definidos pelo usuário
    // =========================================================================

    public sealed class ChipLibrary
    {
        public readonly Dictionary<string, CompositeDefinition> Defs = new();
        public CompositeDefinition Get(string name) =>
            name != null && Defs.TryGetValue(name, out var d) ? d : null;
        public void Add(CompositeDefinition d) { if (d != null) Defs[d.Name] = d; }
        public IEnumerable<string> Names => Defs.Keys;
        public void Clear() => Defs.Clear();
    }

    public sealed class CompositeNodeDef
    {
        public string Kind;          // nome de EditorChipKind, ou "Composite"
        public string CompositeName; // se Kind == "Composite"
    }

    public sealed class CompositeWireDef
    {
        public int FromNode, FromPin, ToNode, ToPin;
    }

    /// <summary>
    /// A "planta" de um chip composto: nós + fios, com a ordem dos pinos
    /// externos (entradas/saídas). Sabe se AVALIAR sozinha (recursivamente).
    /// </summary>
    public sealed class CompositeDefinition
    {
        public string Name;
        public List<CompositeNodeDef> Nodes = new();
        public List<CompositeWireDef> Wires = new();
        public List<int> InputOrder = new();   // índices em Nodes que são IN (ordem dos pinos)
        public List<int> OutputOrder = new();  // índices em Nodes que são OUT

        public int InputCount => InputOrder.Count;
        public int OutputCount => OutputOrder.Count;

        /// <summary>Cria uma definição a partir do grafo atual do editor.</summary>
        public static CompositeDefinition CreateFrom(EditorGraph g, string name)
        {
            var def = new CompositeDefinition { Name = name };
            var idToIndex = new Dictionary<int, int>();

            foreach (var c in g.Chips)
            {
                idToIndex[c.Id] = def.Nodes.Count;
                def.Nodes.Add(new CompositeNodeDef
                {
                    Kind = c.Kind == EditorChipKind.Composite ? "Composite" : c.Kind.ToString(),
                    CompositeName = c.CompositeName
                });
            }

            foreach (var w in g.Wires)
                def.Wires.Add(new CompositeWireDef
                {
                    FromNode = idToIndex[w.FromChip], FromPin = w.FromPin,
                    ToNode = idToIndex[w.ToChip], ToPin = w.ToPin
                });

            // Pinos externos: IN/OUT ordenados de cima p/ baixo (Y, depois X) — ordem estável.
            foreach (var c in g.Chips.Where(c => c.Kind == EditorChipKind.Input)
                                     .OrderBy(c => c.Y).ThenBy(c => c.X))
                def.InputOrder.Add(idToIndex[c.Id]);
            foreach (var c in g.Chips.Where(c => c.Kind == EditorChipKind.Output)
                                     .OrderBy(c => c.Y).ThenBy(c => c.X))
                def.OutputOrder.Add(idToIndex[c.Id]);

            return def;
        }

        /// <summary>Avalia a definição com os trits de entrada e devolve as saídas.</summary>
        public Trit[] Run(Trit[] inputs, ChipLibrary lib)
        {
            var g = new EditorGraph { Library = lib };
            var ids = new int[Nodes.Count];

            for (int i = 0; i < Nodes.Count; i++)
            {
                var nd = Nodes[i];
                if (nd.Kind == "Composite")
                {
                    var d = lib?.Get(nd.CompositeName);
                    ids[i] = g.AddComposite(nd.CompositeName, d?.InputCount ?? 0, d?.OutputCount ?? 0, 0, 0).Id;
                }
                else
                {
                    var kind = System.Enum.Parse<EditorChipKind>(nd.Kind);
                    ids[i] = g.Add(kind, 0, 0).Id;
                }
            }

            foreach (var w in Wires)
                g.Connect(ids[w.FromNode], w.FromPin, ids[w.ToNode], w.ToPin);

            for (int k = 0; k < InputOrder.Count && k < inputs.Length; k++)
            {
                var inChip = g.ById(ids[InputOrder[k]]);
                if (inChip != null && inChip.Outputs.Length > 0) inChip.Outputs[0] = inputs[k];
            }

            g.Evaluate();

            var outs = new Trit[OutputCount];
            for (int k = 0; k < OutputOrder.Count; k++)
            {
                var outChip = g.ById(ids[OutputOrder[k]]);
                outs[k] = (outChip != null && outChip.Inputs.Length > 0) ? outChip.Inputs[0] : Trit.Zero;
            }
            return outs;
        }
    }

    // =========================================================================
    //  O grafo do editor + avaliador
    // =========================================================================

    public sealed class EditorGraph
    {
        public readonly List<EditorChip> Chips = new();
        public readonly List<EditorWire> Wires = new();
        public ChipLibrary Library;   // necessário para avaliar chips compostos
        private int _nextId;

        public EditorChip Add(EditorChipKind kind, double x, double y)
        {
            var c = new EditorChip(_nextId++, kind, x, y);
            Chips.Add(c);
            return c;
        }

        public EditorChip AddComposite(string name, int inCount, int outCount, double x, double y)
        {
            var c = new EditorChip(_nextId++, name, inCount, outCount, x, y);
            Chips.Add(c);
            return c;
        }

        public EditorChip ById(int id) => Chips.Find(c => c.Id == id);

        public void RemoveChip(int id)
        {
            Wires.RemoveAll(w => w.FromChip == id || w.ToChip == id);
            Chips.RemoveAll(c => c.Id == id);
        }

        public bool InputOccupied(int chipId, int pin) =>
            Wires.Exists(w => w.ToChip == chipId && w.ToPin == pin);

        public void Connect(int fc, int fp, int tc, int tp)
        {
            if (InputOccupied(tc, tp)) return;
            Wires.Add(new EditorWire(fc, fp, tc, tp));
        }

        public void Clear()
        {
            Chips.Clear();
            Wires.Clear();
            _nextId = 0;
        }

        public void Evaluate(int maxIterations = 200)
        {
            for (int iter = 0; iter < maxIterations; iter++)
            {
                bool changed = false;

                // 1) Propaga fios.
                foreach (var w in Wires)
                {
                    var from = ById(w.FromChip);
                    var to   = ById(w.ToChip);
                    if (from == null || to == null) continue;
                    if (w.FromPin >= from.Outputs.Length || w.ToPin >= to.Inputs.Length) continue;

                    Trit v = from.Outputs[w.FromPin];
                    if (to.Inputs[w.ToPin] != v) { to.Inputs[w.ToPin] = v; changed = true; }
                }

                // 2) Reavalia cada chip.
                foreach (var c in Chips)
                {
                    if (c.Kind == EditorChipKind.Input || c.Kind == EditorChipKind.Output)
                        continue;

                    if (c.Kind == EditorChipKind.Composite)
                    {
                        var def = Library?.Get(c.CompositeName);
                        if (def == null) continue;
                        var outs = def.Run(c.Inputs, Library);
                        for (int i = 0; i < c.Outputs.Length && i < outs.Length; i++)
                            if (c.Outputs[i] != outs[i]) { c.Outputs[i] = outs[i]; changed = true; }
                    }
                    else if (ChipMeta.TryToBuiltin(c.Kind, out var bt))
                    {
                        Trit before = c.Outputs.Length > 0 ? c.Outputs[0] : Trit.Zero;
                        BuiltinChip.Evaluate(bt, c.Inputs, c.Outputs);
                        if (c.Outputs.Length > 0 && c.Outputs[0] != before) changed = true;
                    }
                }

                if (!changed) break;
            }
        }
    }
}
