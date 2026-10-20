using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ChiefComplaint.Cards.Data;
using NUnit.Framework;
using UnityEditor;

namespace ChiefComplaint.Tests
{
    /// <summary>
    /// Validates the stored card assets under Assets/Data. Patient symptom counts are read from the
    /// raw asset text because <see cref="PatientCardSO"/>.OnValidate truncates the list in memory.
    /// </summary>
    [TestFixture]
    [Category("Data")]
    public class CardDataValidationTests
    {
        private const int MaxSymptomsPerPatient = 3;

        private static string[] FindAssetPaths<T>() where T : UnityEngine.Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/Data" })
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .ToArray();

        [Test]
        public void PatientAssets_AreFound()
        {
            Assert.IsNotEmpty(FindAssetPaths<PatientCardSO>(), "No PatientCardSO assets found under Assets/Data.");
        }

        [Test]
        public void PatientAssets_StoreNoMoreThanThreeSymptomCategories()
        {
            var offenders = FindAssetPaths<PatientCardSO>()
                .Select(path => (path, count: CountStoredCategories(path)))
                .Where(x => x.count > MaxSymptomsPerPatient)
                .Select(x => $"{x.path} ({x.count})")
                .ToArray();

            Assert.IsEmpty(offenders, "Patients over the " + MaxSymptomsPerPatient + "-symptom limit:\n" + string.Join("\n", offenders));
        }

        [Test]
        public void PatientAssets_HaveValidTriageAndStability()
        {
            foreach (var path in FindAssetPaths<PatientCardSO>())
            {
                var patient = AssetDatabase.LoadAssetAtPath<PatientCardSO>(path);
                Assert.That(patient.triageLevel, Is.InRange(1, 5), $"{path}: triage level out of range.");
                Assert.Greater(patient.startingStability, 0, $"{path}: starting stability must be positive.");
                Assert.IsNotEmpty(patient.patientName, $"{path}: missing patient name.");
            }
        }

        [Test]
        public void SymptomAssets_HaveNamesForEveryTier()
        {
            foreach (var path in FindAssetPaths<SymptomCardSO>())
            {
                var symptom = AssetDatabase.LoadAssetAtPath<SymptomCardSO>(path);
                Assert.IsNotEmpty(symptom.tier1Name, $"{path}: missing tier 1 name.");
                Assert.IsNotEmpty(symptom.tier2Name, $"{path}: missing tier 2 name.");
                Assert.IsNotEmpty(symptom.tier3Name, $"{path}: missing tier 3 name.");
            }
        }

        [Test]
        public void ActionAssets_HaveSaneCostsAndTiers()
        {
            foreach (var path in FindAssetPaths<ActionCardSO>())
            {
                var card = AssetDatabase.LoadAssetAtPath<ActionCardSO>(path);
                Assert.That(card.apCost, Is.InRange(0, 5), $"{path}: AP cost out of range.");
                Assert.That(card.powerTier, Is.InRange(1, 3), $"{path}: power tier out of range.");
                Assert.That(card.successChance, Is.InRange(0f, 1f), $"{path}: success chance out of range.");
            }
        }

        /// <summary>Counts the list entries under <c>incomingCategories:</c> in the raw YAML.</summary>
        private static int CountStoredCategories(string assetPath)
        {
            var lines = File.ReadAllLines(assetPath);
            int start = System.Array.FindIndex(lines, l => l.TrimStart().StartsWith("incomingCategories:"));
            if (start < 0) return 0;
            if (Regex.IsMatch(lines[start], @"incomingCategories:\s*\[\]")) return 0;

            int count = 0;
            for (int i = start + 1; i < lines.Length && lines[i].TrimStart().StartsWith("- "); i++)
                count++;
            return count;
        }
    }
}
