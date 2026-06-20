// =============================================================================
//  EditorCanvas.cs  —  A superfície visual interativa do simulador
// =============================================================================
//
//  Tudo é desenhado "na mão" via Render(DrawingContext): chips como caixas,
//  pinos como bolinhas e fios como curvas. A interação (arrastar, ligar fios,
//  alternar entradas, apagar) é feita com hit-testing manual nos eventos de
//  ponteiro. Esse estilo (sem controles-filho) é simples de entender e é o que
//  dá ao editor o "feeling" de um editor de nós.
//
//  COMO USAR (no app):
//   • Clique numa porta na paleta → ela aparece no canvas.
//   • Arraste o corpo de um chip para movê-lo.
//   • Clique num pino de SAÍDA (direita) e depois num pino de ENTRADA (esquerda)
//     para criar um fio.
//   • Clique no corpo de um chip IN para alternar seu valor: N → O → P.
//   • Botão direito sobre um chip o apaga.
//  A simulação roda sozinha a cada mudança e recolore os fios:
//     🔴 vermelho = N (-1)   ⚫ cinza = O (0)   🟢 verde = P (+1)
// =============================================================================

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering;
using TernaryLogicSim.Simulation;

namespace TernaryLogicSim.App.Editor
{
    public sealed class EditorCanvas : Control, ICustomHitTest
    {
        public readonly EditorGraph Graph = new();
        public readonly ChipLibrary Library = new();

        /// <summary>Avisado quando a biblioteca muda (criar chip / abrir projeto).</summary>
        public System.Action OnLibraryChanged;

        // ---- Constantes de geometria ----
        private const double ChipWidth = 66;
        private const double PinSpacing = 24;
        private const double TopPad = 8;
        private const double PinRadius = 6;
        private const double PinHitRadius = 11;

        // ---- Cores ----
        private static readonly IBrush BgBrush     = new SolidColorBrush(Color.FromRgb(0x1E, 0x1F, 0x24));
        private static readonly IBrush GridBrush   = new SolidColorBrush(Color.FromRgb(0x2A, 0x2C, 0x33));
        private static readonly IBrush ChipFill    = new SolidColorBrush(Color.FromRgb(0x32, 0x36, 0x40));
        private static readonly IBrush ChipStroke  = new SolidColorBrush(Color.FromRgb(0x5A, 0x60, 0x70));
        private static readonly IBrush TextBrush   = Brushes.White;
        private static readonly IBrush NegBrush    = new SolidColorBrush(Color.FromRgb(0xE0, 0x4F, 0x4F)); // vermelho
        private static readonly IBrush ZeroBrush   = new SolidColorBrush(Color.FromRgb(0x55, 0x59, 0x63)); // cinza
        private static readonly IBrush PosBrush    = new SolidColorBrush(Color.FromRgb(0x46, 0xC9, 0x66)); // verde
        private static readonly IBrush PinStroke   = new SolidColorBrush(Color.FromRgb(0x20, 0x22, 0x28));

        // ---- Estado de interação ----
        private int _placeCounter;
        private EditorChip _dragChip;
        private Point _dragOffset;
        private bool _wiring;
        private int _wireFromChip, _wireFromPin;
        private Point _mouse;
        private Point _dragStart;     // onde o arraste começou
        private bool _dragMoved;      // houve movimento real? (distingue clique de arraste)

        public EditorCanvas()
        {
            Focusable = true;
            ClipToBounds = true;     // não desenha/recebe cliques fora da própria área
            Graph.Library = Library; // o grafo precisa da biblioteca p/ avaliar compostos
        }

        // Hit-test LIMITADO aos próprios limites. (Retornar 'true' sempre fazia o
        // canvas roubar os cliques da janela inteira — paleta e barra incluídas.)
        public bool HitTest(Point point) =>
            point.X >= 0 && point.Y >= 0 && point.X <= Bounds.Width && point.Y <= Bounds.Height;

        // =====================================================================
        //  API pública (chamada pela janela)
        // =====================================================================

        /// <summary>Adiciona um chip em posição cascateada e re-simula.</summary>
        public void AddChip(EditorChipKind kind)
        {
            var (x, y) = NextSpot();
            Graph.Add(kind, x, y);
            Reevaluate();
        }

        /// <summary>Coloca uma instância de um chip composto da biblioteca.</summary>
        public void AddComposite(string name)
        {
            var def = Library.Get(name);
            if (def == null) return;
            var (x, y) = NextSpot();
            Graph.AddComposite(name, def.InputCount, def.OutputCount, x, y);
            Reevaluate();
        }

        private (double, double) NextSpot()
        {
            double x = 170 + (_placeCounter % 6) * 92;
            double y = 70 + (_placeCounter % 5) * 84;
            _placeCounter++;
            return (x, y);
        }

        // ---- Salvar / carregar ----
        public string ToJson() => CircuitIO.Serialize(Graph, Library);

        public void LoadJson(string json)
        {
            CircuitIO.Deserialize(json, Graph, Library);
            Graph.Library = Library;
            _placeCounter = Graph.Chips.Count;
            OnLibraryChanged?.Invoke();
            Reevaluate();
        }

        /// <summary>
        /// Agrupa o circuito atual num NOVO chip reutilizável. Retorna false com
        /// uma mensagem se algo impedir (nome vazio/duplicado, sem saídas).
        /// </summary>
        public bool CreateCompositeFromCurrent(string name, out string error)
        {
            error = null;
            name = (name ?? "").Trim();
            if (name.Length == 0) { error = "Dê um nome ao chip."; return false; }
            if (Library.Get(name) != null) { error = $"Já existe um chip chamado \"{name}\"."; return false; }

            int outs = Graph.Chips.FindAll(c => c.Kind == EditorChipKind.Output).Count;
            if (outs == 0) { error = "O circuito precisa de pelo menos um chip OUT."; return false; }

            var def = CompositeDefinition.CreateFrom(Graph, name);
            Library.Add(def);
            OnLibraryChanged?.Invoke();
            return true;
        }

        public void ClearAll()
        {
            Graph.Chips.Clear();
            Graph.Wires.Clear();
            _placeCounter = 0;
            Reevaluate();
        }

        public void Reevaluate()
        {
            Graph.Evaluate();
            InvalidateVisual();
        }

        /// <summary>Monta um exemplo pronto: meio-somador ternário (a, b → soma, vai-um).</summary>
        public void LoadDemoHalfAdder()
        {
            ClearAll();
            var a   = Graph.Add(EditorChipKind.Input, 80, 80);
            var b   = Graph.Add(EditorChipKind.Input, 80, 200);
            var z   = Graph.Add(EditorChipKind.ConstZero, 80, 320);
            var add = Graph.Add(EditorChipKind.FullAdder, 280, 150);
            var sum = Graph.Add(EditorChipKind.Output, 480, 130);
            var car = Graph.Add(EditorChipKind.Output, 480, 210);
            a.Outputs[0] = Trit.Positive;
            b.Outputs[0] = Trit.Positive;
            Graph.Connect(a.Id, 0, add.Id, 0);
            Graph.Connect(b.Id, 0, add.Id, 1);
            Graph.Connect(z.Id, 0, add.Id, 2);
            Graph.Connect(add.Id, 0, sum.Id, 0);
            Graph.Connect(add.Id, 1, car.Id, 0);
            Reevaluate();
        }

        // =====================================================================
        //  Geometria
        // =====================================================================

        // Usa o tamanho real dos arrays — funciona para primitivos E compostos.
        private static int Rows(EditorChip c) =>
            Math.Max(1, Math.Max(c.Inputs.Length, c.Outputs.Length));

        private static double ChipHeight(EditorChip c) => Rows(c) * PinSpacing + TopPad;

        private static Rect ChipRect(EditorChip c) =>
            new Rect(c.X, c.Y, ChipWidth, ChipHeight(c));

        private static Point InputPinPos(EditorChip c, int i) =>
            new Point(c.X, c.Y + TopPad / 2 + i * PinSpacing + PinSpacing / 2);

        private static Point OutputPinPos(EditorChip c, int i) =>
            new Point(c.X + ChipWidth, c.Y + TopPad / 2 + i * PinSpacing + PinSpacing / 2);

        private static IBrush TritBrush(Trit t) => t switch
        {
            Trit.Negative => NegBrush,
            Trit.Positive => PosBrush,
            _             => ZeroBrush
        };

        // =====================================================================
        //  Desenho
        // =====================================================================

        public override void Render(DrawingContext ctx)
        {
            // Control não pinta fundo sozinho: cobrimos toda a área.
            ctx.DrawRectangle(BgBrush, null, new Rect(Bounds.Size));

            DrawGrid(ctx);

            // Fios primeiro (ficam atrás dos chips)
            foreach (var w in Graph.Wires)
            {
                var from = Graph.ById(w.FromChip);
                var to   = Graph.ById(w.ToChip);
                if (from == null || to == null) continue;
                if (w.FromPin >= from.Outputs.Length) continue;
                var p0 = OutputPinPos(from, w.FromPin);
                var p3 = InputPinPos(to, w.ToPin);
                var brush = TritBrush(from.Outputs[w.FromPin]);
                DrawWire(ctx, p0, p3, new Pen(brush, 3));
            }

            // Fio sendo arrastado (preview)
            if (_wiring)
            {
                var from = Graph.ById(_wireFromChip);
                if (from != null && _wireFromPin < from.Outputs.Length)
                {
                    var p0 = OutputPinPos(from, _wireFromPin);
                    DrawWire(ctx, p0, _mouse, new Pen(GridBrush, 2, dashStyle: new DashStyle(new double[] { 3, 3 }, 0)));
                }
            }

            // Chips
            var typeface = Typeface.Default;
            foreach (var c in Graph.Chips)
            {
                var rect = ChipRect(c);

                // Cor do corpo: IN/OUT refletem o valor; demais usam cinza neutro.
                IBrush body = ChipFill;
                if (c.Kind == EditorChipKind.Input)
                    body = TritBrush(c.Outputs.Length > 0 ? c.Outputs[0] : Trit.Zero);
                else if (c.Kind == EditorChipKind.Output)
                    body = TritBrush(c.Inputs.Length > 0 ? c.Inputs[0] : Trit.Zero);

                ctx.DrawRectangle(body, new Pen(ChipStroke, 1.5), rect, 6, 6);

                // Rótulo (compostos mostram o próprio nome)
                string label = c.Label;
                if (c.Kind == EditorChipKind.Input && c.Outputs.Length > 0)
                    label = c.Outputs[0].Symbol().ToString();
                var ft = new FormattedText(label, CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, typeface, 14, TextBrush);
                ctx.DrawText(ft, new Point(
                    c.X + (ChipWidth - ft.Width) / 2,
                    c.Y + (ChipHeight(c) - ft.Height) / 2));

                // Pinos de entrada
                for (int i = 0; i < c.Inputs.Length; i++)
                {
                    var p = InputPinPos(c, i);
                    ctx.DrawEllipse(TritBrush(c.Inputs[i]), new Pen(PinStroke, 1.5), p, PinRadius, PinRadius);
                }
                // Pinos de saída
                for (int i = 0; i < c.Outputs.Length; i++)
                {
                    var p = OutputPinPos(c, i);
                    ctx.DrawEllipse(TritBrush(c.Outputs[i]), new Pen(PinStroke, 1.5), p, PinRadius, PinRadius);
                }
            }
        }

        private void DrawGrid(DrawingContext ctx)
        {
            var pen = new Pen(GridBrush, 1);
            double step = 32;
            for (double x = 0; x < Bounds.Width; x += step)
                ctx.DrawLine(pen, new Point(x, 0), new Point(x, Bounds.Height));
            for (double y = 0; y < Bounds.Height; y += step)
                ctx.DrawLine(pen, new Point(0, y), new Point(Bounds.Width, y));
        }

        private static void DrawWire(DrawingContext ctx, Point p0, Point p3, IPen pen)
        {
            double dx = Math.Max(30, Math.Abs(p3.X - p0.X) * 0.5);
            var c1 = new Point(p0.X + dx, p0.Y);
            var c2 = new Point(p3.X - dx, p3.Y);
            var geo = new StreamGeometry();
            using (var g = geo.Open())
            {
                g.BeginFigure(p0, false);
                g.CubicBezierTo(c1, c2, p3);
                g.EndFigure(false);
            }
            ctx.DrawGeometry(null, pen, geo);
        }

        // =====================================================================
        //  Interação (hit-testing manual)
        // =====================================================================

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            var pos = e.GetPosition(this);
            _mouse = pos;
            var props = e.GetCurrentPoint(this).Properties;

            // Botão direito → apagar chip sob o cursor; se não houver chip, apagar o fio sob o cursor.
            if (props.IsRightButtonPressed)
            {
                var hit = ChipAt(pos);
                if (hit != null) { Graph.RemoveChip(hit.Id); Reevaluate(); return; }

                var wire = WireAt(pos);
                if (wire != null)
                {
                    // Reseta a entrada que o fio alimentava (senão fica com valor "preso").
                    var dst = Graph.ById(wire.ToChip);
                    if (dst != null && wire.ToPin < dst.Inputs.Length) dst.Inputs[wire.ToPin] = Trit.Zero;
                    Graph.Wires.Remove(wire);
                    Reevaluate();
                }
                return;
            }

            // 1) Pino de saída → começar a ligar um fio
            if (TryHitOutputPin(pos, out var oc, out var op))
            {
                _wiring = true; _wireFromChip = oc.Id; _wireFromPin = op;
                return;
            }

            // 2) Pino de entrada → completar fio (se estiver ligando)
            if (TryHitInputPin(pos, out var icc, out var ip))
            {
                if (_wiring)
                {
                    Graph.Connect(_wireFromChip, _wireFromPin, icc.Id, ip);
                    _wiring = false;
                    Reevaluate();
                }
                return;
            }

            // 3) Corpo de chip → começa arraste (TODOS, inclusive IN).
            //    Para IN, se for um clique sem mover, alterna o valor ao soltar.
            var chip = ChipAt(pos);
            if (chip != null)
            {
                _dragChip = chip;
                _dragOffset = new Point(pos.X - chip.X, pos.Y - chip.Y);
                _dragStart = pos;
                _dragMoved = false;
                return;
            }

            // 4) Clique no vazio cancela ligação em andamento
            if (_wiring) { _wiring = false; InvalidateVisual(); }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            _mouse = e.GetPosition(this);

            if (_dragChip != null)
            {
                // Só conta como "arraste" depois de um pequeno limiar (evita micro-tremores).
                double dx = _mouse.X - _dragStart.X, dy = _mouse.Y - _dragStart.Y;
                if (!_dragMoved && dx * dx + dy * dy > 16) _dragMoved = true; // ~4px
                if (_dragMoved)
                {
                    _dragChip.X = _mouse.X - _dragOffset.X;
                    _dragChip.Y = _mouse.Y - _dragOffset.Y;
                    InvalidateVisual();
                }
            }
            else if (_wiring)
            {
                InvalidateVisual();
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            // Clique (sem mover) num chip IN → alterna N → O → P.
            if (_dragChip != null && !_dragMoved && _dragChip.Kind == EditorChipKind.Input)
            {
                _dragChip.CycleInput();
                Reevaluate();
            }
            _dragChip = null;
        }

        // ---- Helpers de hit-testing ----

        private EditorChip ChipAt(Point p)
        {
            // De trás para frente (último desenhado = topo)
            for (int i = Graph.Chips.Count - 1; i >= 0; i--)
                if (ChipRect(Graph.Chips[i]).Contains(p)) return Graph.Chips[i];
            return null;
        }

        private bool TryHitOutputPin(Point p, out EditorChip chip, out int pin)
        {
            foreach (var c in Graph.Chips)
                for (int i = 0; i < c.Outputs.Length; i++)
                    if (Near(p, OutputPinPos(c, i))) { chip = c; pin = i; return true; }
            chip = null; pin = -1; return false;
        }

        private bool TryHitInputPin(Point p, out EditorChip chip, out int pin)
        {
            foreach (var c in Graph.Chips)
                for (int i = 0; i < c.Inputs.Length; i++)
                    if (Near(p, InputPinPos(c, i))) { chip = c; pin = i; return true; }
            chip = null; pin = -1; return false;
        }

        private static bool Near(Point a, Point b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            return dx * dx + dy * dy <= PinHitRadius * PinHitRadius;
        }

        /// <summary>Acha um fio próximo ao ponto (amostrando a mesma curva de Bézier do desenho).</summary>
        private EditorWire WireAt(Point p)
        {
            const double hit = 7.0;          // tolerância em pixels
            const double hit2 = hit * hit;
            foreach (var w in Graph.Wires)
            {
                var from = Graph.ById(w.FromChip);
                var to   = Graph.ById(w.ToChip);
                if (from == null || to == null) continue;
                if (w.FromPin >= from.Outputs.Length || w.ToPin >= to.Inputs.Length) continue;

                var p0 = OutputPinPos(from, w.FromPin);
                var p3 = InputPinPos(to, w.ToPin);
                double dx = Math.Max(30, Math.Abs(p3.X - p0.X) * 0.5);
                var c1 = new Point(p0.X + dx, p0.Y);
                var c2 = new Point(p3.X - dx, p3.Y);

                // Amostra a curva e mede a menor distância ao ponto.
                const int steps = 24;
                for (int i = 0; i <= steps; i++)
                {
                    double t = (double)i / steps, u = 1 - t;
                    double bx = u * u * u * p0.X + 3 * u * u * t * c1.X + 3 * u * t * t * c2.X + t * t * t * p3.X;
                    double by = u * u * u * p0.Y + 3 * u * u * t * c1.Y + 3 * u * t * t * c2.Y + t * t * t * p3.Y;
                    double ex = bx - p.X, ey = by - p.Y;
                    if (ex * ex + ey * ey <= hit2) return w;
                }
            }
            return null;
        }
    }
}
