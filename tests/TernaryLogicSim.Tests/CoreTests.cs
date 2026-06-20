// =============================================================================
//  CoreTests.cs  —  Testes do núcleo em xUnit
// =============================================================================
//  Rodar:  dotnet test
//  Reproduz, em C#, a mesma bateria exaustiva validada em Python
//  (tests/python/validate_core.py).
// =============================================================================

using System;
using System.Collections.Generic;
using Xunit;
using TernaryLogicSim.Simulation;

namespace TernaryLogicSim.Tests
{
    public class CoreTests
    {
        private static readonly Trit[] All = { Trit.Negative, Trit.Zero, Trit.Positive };

        [Fact]
        public void Not_MatchesTable_AndIsInvolution()
        {
            Assert.Equal(Trit.Positive, TernaryGates.Not(Trit.Negative));
            Assert.Equal(Trit.Zero,     TernaryGates.Not(Trit.Zero));
            Assert.Equal(Trit.Negative, TernaryGates.Not(Trit.Positive));
            foreach (var a in All)
                Assert.Equal(a, TernaryGates.Not(TernaryGates.Not(a)));
        }

        [Fact]
        public void Nti_Pti_MatchTernaryDefinitions()
        {
            // NTI: N→P, O→N, P→N
            Assert.Equal(Trit.Positive, TernaryGates.Nti(Trit.Negative));
            Assert.Equal(Trit.Negative, TernaryGates.Nti(Trit.Zero));
            Assert.Equal(Trit.Negative, TernaryGates.Nti(Trit.Positive));
            // PTI: N→P, O→P, P→N
            Assert.Equal(Trit.Positive, TernaryGates.Pti(Trit.Negative));
            Assert.Equal(Trit.Positive, TernaryGates.Pti(Trit.Zero));
            Assert.Equal(Trit.Negative, TernaryGates.Pti(Trit.Positive));
        }

        [Fact]
        public void MinMax_MatchReference_Exhaustive()
        {
            foreach (var a in All)
            foreach (var b in All)
            {
                Assert.Equal(Math.Min(a.ToInt(), b.ToInt()), TernaryGates.Min(a, b).ToInt());
                Assert.Equal(Math.Max(a.ToInt(), b.ToInt()), TernaryGates.Max(a, b).ToInt());
            }
        }

        [Fact]
        public void DeMorgan_Ternary_Holds()
        {
            foreach (var a in All)
            foreach (var b in All)
                Assert.Equal(
                    TernaryGates.Max(TernaryGates.Not(a), TernaryGates.Not(b)),
                    TernaryGates.Not(TernaryGates.Min(a, b)));
        }

        [Fact]
        public void FullAdder_Exhaustive_27()
        {
            foreach (var a in All)
            foreach (var b in All)
            foreach (var c in All)
            {
                var (sum, carry) = TernaryArithmetic.FullAdder(a, b, c);
                Assert.Equal(a.ToInt() + b.ToInt() + c.ToInt(), carry.ToInt() * 3 + sum.ToInt());
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(42)]
        [InlineData(-200)]
        [InlineData(200)]
        public void IntConversion_RoundTrips(int v)
        {
            Assert.Equal(v, TernaryArithmetic.ToInt(TernaryArithmetic.FromInt(v)));
        }

        [Fact]
        public void IntConversion_RoundTrips_FullRange()
        {
            for (int v = -200; v <= 200; v++)
                Assert.Equal(v, TernaryArithmetic.ToInt(TernaryArithmetic.FromInt(v)));
        }

        [Fact]
        public void TritBus_PackUnpack_RoundTrips()
        {
            var bus = new TritBus();
            var rng = new Random(42);
            var seq = new Trit[TritBus.Capacity];
            for (int i = 0; i < seq.Length; i++)
            {
                seq[i] = All[rng.Next(3)];
                bus.Set(i, seq[i]);
            }
            for (int i = 0; i < seq.Length; i++)
                Assert.Equal(seq[i], bus.Get(i));
        }

        [Fact]
        public void Circuit_NotNot_IsIdentity()
        {
            foreach (var x in All)
            {
                var c = new Circuit(1, 1);
                int n1 = c.AddNode(BuiltinChipType.Not);
                int n2 = c.AddNode(BuiltinChipType.Not);
                c.Connect(new PinRef(-1, 0), new PinRef(n1, 0));
                c.Connect(new PinRef(n1, 0), new PinRef(n2, 0));
                c.Connect(new PinRef(n2, 0), new PinRef(-1, 0));
                c.SetInput(0, x);
                c.Run();
                Assert.Equal(x, c.GetOutput(0));
            }
        }

        [Fact]
        public void Circuit_ThreeTritAdder_MatchesArithmetic()
        {
            for (int x = -13; x <= 13; x++)
            for (int y = -13; y <= 13; y++)
                Assert.Equal(x + y, RunAdder(x, y));
        }

        private static int RunAdder(int x, int y)
        {
            var xt = Pad(TernaryArithmetic.FromInt(x), 3);
            var yt = Pad(TernaryArithmetic.FromInt(y), 3);

            var c = new Circuit(6, 4);
            int zero = c.AddNode(BuiltinChipType.ConstZero);
            var fas = new int[3];
            for (int k = 0; k < 3; k++) fas[k] = c.AddNode(BuiltinChipType.FullAdder);

            for (int k = 0; k < 3; k++)
            {
                c.Connect(new PinRef(-1, k),     new PinRef(fas[k], 0));
                c.Connect(new PinRef(-1, 3 + k), new PinRef(fas[k], 1));
                if (k == 0) c.Connect(new PinRef(zero, 0),       new PinRef(fas[0], 2));
                else        c.Connect(new PinRef(fas[k - 1], 1), new PinRef(fas[k], 2));
                c.Connect(new PinRef(fas[k], 0), new PinRef(-1, k));
            }
            c.Connect(new PinRef(fas[2], 1), new PinRef(-1, 3));

            for (int k = 0; k < 3; k++) { c.SetInput(k, xt[k]); c.SetInput(3 + k, yt[k]); }
            c.Run();

            var res = new List<Trit> { c.GetOutput(0), c.GetOutput(1), c.GetOutput(2), c.GetOutput(3) };
            return TernaryArithmetic.ToInt(res);
        }

        private static List<Trit> Pad(List<Trit> t, int width)
        {
            while (t.Count < width + 1) t.Add(Trit.Zero);
            return t;
        }
    }
}
