using System.Collections.Generic;
using ChiefComplaint.Bureaucracy;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Core;
using NUnit.Framework;
using UnityEngine;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>Pure-logic tests for the bureaucracy layer (citations, shift sequences, cost modifiers).</summary>
    [Category("Bureaucracy")]
    public class BureaucracyTests
    {
        // ---------------------------------------------------------------- Citations

        [Test]
        public void CitationLedger_EscalatesWarningWarningFineThenRedTape()
        {
            var ledger = new CitationLedger { warningsPerShift = 2 };

            Assert.AreEqual(CitationPenalty.Warning, ledger.Issue());
            Assert.AreEqual(CitationPenalty.Warning, ledger.Issue());
            Assert.AreEqual(CitationPenalty.Fine, ledger.Issue());
            Assert.AreEqual(CitationPenalty.RedTape, ledger.Issue());
            Assert.AreEqual(CitationPenalty.RedTape, ledger.Issue());
        }

        [Test]
        public void CitationLedger_ShiftResetRestoresWarningsButKeepsPeriodCount()
        {
            var ledger = new CitationLedger { warningsPerShift = 2 };
            ledger.Issue(); ledger.Issue(); ledger.Issue();

            ledger.ResetShift();

            Assert.AreEqual(CitationPenalty.Warning, ledger.Issue());
            Assert.AreEqual(4, ledger.PeriodCount, "Period count spans shifts until the next inspection.");
            Assert.AreEqual(1, ledger.ShiftCount);
        }

        // ---------------------------------------------------------------- Shift sequence

        [Test]
        public void ShiftSequence_RunsStepsInOrderThenCompletes()
        {
            var seq = new ShiftSequence();
            var log = new List<string>();
            seq.Register(20, next => { log.Add("b"); next(); });
            seq.Register(10, next => { log.Add("a"); next(); });

            seq.Run(() => log.Add("done"));

            CollectionAssert.AreEqual(new[] { "a", "b", "done" }, log);
        }

        [Test]
        public void ShiftSequence_WaitsForAStepToContinue()
        {
            var seq = new ShiftSequence();
            System.Action pending = null;
            bool done = false;
            seq.Register(0, next => pending = next);

            seq.Run(() => done = true);
            Assert.IsFalse(done, "The sequence must wait while a dialog step is open.");

            pending();
            Assert.IsTrue(done);
        }

        [Test]
        public void ShiftSequence_StopsWhenToldTo()
        {
            var seq = new ShiftSequence();
            bool secondRan = false, done = false;
            bool stop = false;
            seq.Register(0, next => { stop = true; next(); });
            seq.Register(1, next => { secondRan = true; next(); });

            seq.Run(() => done = true, () => stop);

            Assert.IsFalse(secondRan);
            Assert.IsFalse(done);
        }

        // ---------------------------------------------------------------- Cost modifiers

        [Test]
        public void CardCostModifiers_AddsProvidersAndNeverGoesNegative()
        {
            var card = ScriptableObject.CreateInstance<ActionCardSO>();
            card.apCost = 1;
            System.Func<ActionCardSO, int> plusOne = c => 1;
            System.Func<ActionCardSO, int> minusFive = c => -5;
            try
            {
                CardCostModifiers.Add(plusOne);
                Assert.AreEqual(2, CardCostModifiers.GetCost(card));

                CardCostModifiers.Add(minusFive);
                Assert.AreEqual(0, CardCostModifiers.GetCost(card));
            }
            finally
            {
                CardCostModifiers.Remove(plusOne);
                CardCostModifiers.Remove(minusFive);
                Object.DestroyImmediate(card);
            }
        }
    }
}
