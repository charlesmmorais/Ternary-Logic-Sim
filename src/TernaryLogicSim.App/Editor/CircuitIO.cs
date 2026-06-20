// =============================================================================
//  CircuitIO.cs  —  Salvar / carregar projeto em JSON
// =============================================================================
//
//  Um "projeto" guarda DUAS coisas:
//    1) A biblioteca de chips compostos criados pelo usuário.
//    2) O circuito atualmente na tela.
//
//  Usamos System.Text.Json com DTOs simples (propriedades) para um formato
//  estável e legível. Os fios referenciam chips por ÍNDICE na lista (não por Id),
//  o que torna o arquivo independente de como os Ids foram gerados.
// =============================================================================

using System.Collections.Generic;
using System.Text.Json;
using TernaryLogicSim.Simulation;

namespace TernaryLogicSim.App.Editor
{
    // ---- DTOs de serialização -------------------------------------------------
    internal sealed class ChipDTO
    {
        public string Kind { get; set; }       // nome de EditorChipKind ou "Composite"
        public string Composite { get; set; }  // nome do chip composto (se aplicável)
        public double X { get; set; }
        public double Y { get; set; }
        public int Val { get; set; }           // valor de um chip IN (-1/0/+1)
    }

    internal sealed class WireDTO
    {
        public int From { get; set; }      // índice do chip na lista
        public int FromPin { get; set; }
        public int To { get; set; }
        public int ToPin { get; set; }
    }

    internal sealed class GraphDTO
    {
        public List<ChipDTO> Chips { get; set; } = new();
        public List<WireDTO> Wires { get; set; } = new();
    }

    internal sealed class CompNodeDTO
    {
        public string Kind { get; set; }
        public string Composite { get; set; }
    }

    internal sealed class CompWireDTO
    {
        public int FromNode { get; set; }
        public int FromPin { get; set; }
        public int ToNode { get; set; }
        public int ToPin { get; set; }
    }

    internal sealed class CompDTO
    {
        public string Name { get; set; }
        public List<CompNodeDTO> Nodes { get; set; } = new();
        public List<CompWireDTO> Wires { get; set; } = new();
        public List<int> InputOrder { get; set; } = new();
        public List<int> OutputOrder { get; set; } = new();
    }

    internal sealed class ProjectDTO
    {
        public int Version { get; set; } = 1;
        public List<CompDTO> Library { get; set; } = new();
        public GraphDTO Circuit { get; set; } = new();
    }

    public static class CircuitIO
    {
        private static readonly JsonSerializerOptions Opt = new() { WriteIndented = true };

        public static string Serialize(EditorGraph g, ChipLibrary lib)
        {
            var p = new ProjectDTO();
            foreach (var d in lib.Defs.Values) p.Library.Add(ToDto(d));
            p.Circuit = GraphToDto(g);
            return JsonSerializer.Serialize(p, Opt);
        }

        public static void Deserialize(string json, EditorGraph g, ChipLibrary lib)
        {
            var p = JsonSerializer.Deserialize<ProjectDTO>(json, Opt) ?? new ProjectDTO();
            lib.Clear();
            foreach (var c in p.Library) lib.Add(FromDto(c));
            LoadGraph(g, lib, p.Circuit);
        }

        // ---- Conversões: grafo -> DTO ----
        private static GraphDTO GraphToDto(EditorGraph g)
        {
            var dto = new GraphDTO();
            var idToIndex = new Dictionary<int, int>();
            foreach (var c in g.Chips)
            {
                idToIndex[c.Id] = dto.Chips.Count;
                dto.Chips.Add(new ChipDTO
                {
                    Kind = c.Kind == EditorChipKind.Composite ? "Composite" : c.Kind.ToString(),
                    Composite = c.CompositeName,
                    X = c.X, Y = c.Y,
                    Val = (c.Kind == EditorChipKind.Input && c.Outputs.Length > 0) ? c.Outputs[0].ToInt() : 0
                });
            }
            foreach (var w in g.Wires)
                dto.Wires.Add(new WireDTO
                {
                    From = idToIndex[w.FromChip], FromPin = w.FromPin,
                    To = idToIndex[w.ToChip], ToPin = w.ToPin
                });
            return dto;
        }

        // ---- Conversões: DTO -> grafo ----
        private static void LoadGraph(EditorGraph g, ChipLibrary lib, GraphDTO dto)
        {
            g.Clear();
            g.Library = lib;
            var ids = new int[dto.Chips.Count];
            for (int i = 0; i < dto.Chips.Count; i++)
            {
                var cd = dto.Chips[i];
                if (cd.Kind == "Composite")
                {
                    var def = lib.Get(cd.Composite);
                    ids[i] = g.AddComposite(cd.Composite, def?.InputCount ?? 0, def?.OutputCount ?? 0, cd.X, cd.Y).Id;
                }
                else
                {
                    var kind = System.Enum.Parse<EditorChipKind>(cd.Kind);
                    var chip = g.Add(kind, cd.X, cd.Y);
                    if (kind == EditorChipKind.Input && chip.Outputs.Length > 0)
                        chip.Outputs[0] = TritOps.FromInt(cd.Val);
                    ids[i] = chip.Id;
                }
            }
            foreach (var w in dto.Wires)
                g.Connect(ids[w.From], w.FromPin, ids[w.To], w.ToPin);
        }

        // ---- Conversões de chips compostos ----
        private static CompDTO ToDto(CompositeDefinition d)
        {
            var dto = new CompDTO { Name = d.Name, InputOrder = new(d.InputOrder), OutputOrder = new(d.OutputOrder) };
            foreach (var n in d.Nodes) dto.Nodes.Add(new CompNodeDTO { Kind = n.Kind, Composite = n.CompositeName });
            foreach (var w in d.Wires) dto.Wires.Add(new CompWireDTO
                { FromNode = w.FromNode, FromPin = w.FromPin, ToNode = w.ToNode, ToPin = w.ToPin });
            return dto;
        }

        private static CompositeDefinition FromDto(CompDTO dto)
        {
            var d = new CompositeDefinition { Name = dto.Name, InputOrder = new(dto.InputOrder), OutputOrder = new(dto.OutputOrder) };
            foreach (var n in dto.Nodes) d.Nodes.Add(new CompositeNodeDef { Kind = n.Kind, CompositeName = n.Composite });
            foreach (var w in dto.Wires) d.Wires.Add(new CompositeWireDef
                { FromNode = w.FromNode, FromPin = w.FromPin, ToNode = w.ToNode, ToPin = w.ToPin });
            return d;
        }
    }
}
