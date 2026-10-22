using System.Collections.Generic;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Effects;
using ChiefComplaint.Cards.Views;
using NUnit.Framework;
using UnityEngine;

namespace ChiefComplaint.Tests
{
    /// <summary>
    /// <see cref="TrashCardEffectSO"/> targeting: with <see cref="TrashCardEffectSO.redTapeOnly"/> (Sharps Bin) only a
    /// Red Tape card in hand can be disposed of; without it any card can be trashed.
    /// </summary>
    [TestFixture]
    [Category("Effects")]
    public class TrashCardEffectSOTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        private TrashCardEffectSO Trash(bool redTapeOnly)
        {
            var effect = ScriptableObject.CreateInstance<TrashCardEffectSO>();
            effect.redTapeOnly = redTapeOnly;
            created.Add(effect);
            return effect;
        }

        private ActionCardView CardInHand(ActionCardType type, bool withData = true)
        {
            var go = new GameObject("TrashTarget_" + type);
            created.Add(go);
            var view = go.AddComponent<ActionCardView>();
            if (withData)
            {
                var data = ScriptableObject.CreateInstance<ActionCardSO>();
                data.cardName = type + " card";
                data.cardType = type;
                created.Add(data);
                view.currentCardData = data;
            }
            return view;
        }

        [Test]
        public void RedTapeOnly_RedTapeCard_CanApply()
        {
            Assert.IsTrue(Trash(true).CanApply(null, CardInHand(ActionCardType.RedTape)), "Sharps Bin should dispose of a Red Tape card.");
        }

        [TestCase(ActionCardType.Staff)]
        [TestCase(ActionCardType.Supply)]
        [TestCase(ActionCardType.Diagnostic)]
        [TestCase(ActionCardType.Treatment)]
        public void RedTapeOnly_NormalCard_CannotApply(ActionCardType type)
        {
            Assert.IsFalse(Trash(true).CanApply(null, CardInHand(type)), $"A Red-Tape-only trash must refuse a {type} card.");
        }

        [Test]
        public void RedTapeOnly_NullCard_CannotApply()
        {
            Assert.IsFalse(Trash(true).CanApply(null, null), "No target card: nothing to dispose of.");
        }

        [Test]
        public void RedTapeOnly_CardViewWithoutData_CannotApply()
        {
            Assert.IsFalse(Trash(true).CanApply(null, CardInHand(ActionCardType.RedTape, withData: false)),
                "A card view with no card data can't be confirmed as Red Tape.");
        }

        [TestCase(ActionCardType.RedTape)]
        [TestCase(ActionCardType.Staff)]
        [TestCase(ActionCardType.Supply)]
        [TestCase(ActionCardType.Treatment)]
        public void Unrestricted_AnyCard_CanApply(ActionCardType type)
        {
            Assert.IsTrue(Trash(false).CanApply(null, CardInHand(type)), $"An unrestricted trash should accept a {type} card.");
        }

        [Test]
        public void Unrestricted_NullCard_CannotApply()
        {
            Assert.IsFalse(Trash(false).CanApply(null, null), "No target card: nothing to trash.");
        }

        [Test]
        public void Describe_ReflectsTheRestriction()
        {
            Assert.AreEqual("Dispose of a Red Tape card in hand", Trash(true).Describe());
            Assert.AreEqual("Trash a card in hand", Trash(false).Describe());
        }

        [Test]
        public void Targeting_NeedsACardNotAPatient()
        {
            var effect = Trash(true);
            Assert.IsTrue(effect.RequiresCardTarget);
            Assert.IsFalse(effect.RequiresPatientTarget);
        }
    }
}
