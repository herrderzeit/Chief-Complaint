using System.Collections.Generic;
using ChiefComplaint.Cards.Data;
using NUnit.Framework;
using UnityEngine;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>
    /// Covers <see cref="PatientCardSO.HasOwnDeathPenalty"/>: only an OnDeath + LoseReputation trigger
    /// replaces the flat death penalty.
    /// </summary>
    [Category("Data")]
    public class PatientCardSOTests
    {
        private PatientCardSO patient;

        [SetUp]
        public void Setup() => patient = ScriptableObject.CreateInstance<PatientCardSO>();

        [TearDown]
        public void Teardown() => Object.DestroyImmediate(patient);

        private static PatientTrigger Trigger(PatientTriggerCondition condition, PatientTriggerEffect effect, int value = 4) =>
            new PatientTrigger { condition = condition, effect = effect, effectValue = value };

        [Test]
        public void HasOwnDeathPenalty_NullTriggers_IsFalse()
        {
            patient.triggers = null;
            Assert.IsFalse(patient.HasOwnDeathPenalty);
        }

        [Test]
        public void HasOwnDeathPenalty_NoTriggers_IsFalse()
        {
            patient.triggers = new List<PatientTrigger>();
            Assert.IsFalse(patient.HasOwnDeathPenalty);
        }

        [Test]
        public void HasOwnDeathPenalty_OnDeathLoseReputation_IsTrue()
        {
            patient.triggers = new List<PatientTrigger> { Trigger(PatientTriggerCondition.OnDeath, PatientTriggerEffect.LoseReputation) };
            Assert.IsTrue(patient.HasOwnDeathPenalty);
        }

        [Test]
        public void HasOwnDeathPenalty_OnDeathLoseReputationAmongOtherTriggers_IsTrue()
        {
            patient.triggers = new List<PatientTrigger>
            {
                Trigger(PatientTriggerCondition.OnTurnEnd, PatientTriggerEffect.LoseAP),
                Trigger(PatientTriggerCondition.OnDeath, PatientTriggerEffect.LoseReputation),
            };
            Assert.IsTrue(patient.HasOwnDeathPenalty);
        }

        [TestCase(PatientTriggerEffect.DrawCard)]
        [TestCase(PatientTriggerEffect.GainReputation)]
        [TestCase(PatientTriggerEffect.LoseAP)]
        [TestCase(PatientTriggerEffect.AddCardToDiscard)]
        public void HasOwnDeathPenalty_OnDeathWithOtherEffect_IsFalse(PatientTriggerEffect effect)
        {
            patient.triggers = new List<PatientTrigger> { Trigger(PatientTriggerCondition.OnDeath, effect) };
            Assert.IsFalse(patient.HasOwnDeathPenalty, $"An OnDeath {effect} trigger must not waive the flat death penalty.");
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-5)]
        public void HasOwnDeathPenalty_OnDeathLoseReputationOfZeroOrLess_IsFalse(int value)
        {
            patient.triggers = new List<PatientTrigger> { Trigger(PatientTriggerCondition.OnDeath, PatientTriggerEffect.LoseReputation, value) };
            Assert.IsFalse(patient.HasOwnDeathPenalty, $"A trigger of {value} must not waive the flat penalty (a death is never free).");
        }

        [Test]
        public void HasOwnDeathPenalty_OnDeathLoseReputationOfOne_IsTrue()
        {
            patient.triggers = new List<PatientTrigger> { Trigger(PatientTriggerCondition.OnDeath, PatientTriggerEffect.LoseReputation, 1) };
            Assert.IsTrue(patient.HasOwnDeathPenalty);
        }

        [TestCase(PatientTriggerCondition.OnTurnEnd)]
        [TestCase(PatientTriggerCondition.OnSpawn)]
        [TestCase(PatientTriggerCondition.OnCured)]
        [TestCase(PatientTriggerCondition.OnDiagnosed)]
        public void HasOwnDeathPenalty_LoseReputationOnOtherCondition_IsFalse(PatientTriggerCondition condition)
        {
            patient.triggers = new List<PatientTrigger> { Trigger(condition, PatientTriggerEffect.LoseReputation) };
            Assert.IsFalse(patient.HasOwnDeathPenalty, $"A {condition} LoseReputation trigger must not waive the flat death penalty.");
        }
    }
}
