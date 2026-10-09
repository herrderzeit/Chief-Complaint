using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Board
{
    public class BoardView : Singleton<BoardView>
    {
        [Header("Prefabs")]
        public GameObject patientCardPrefab;
        public GameObject symptomSubCardPrefab;

        [Header("Board Slots")]
        public Transform[] triageSlots;
        public Transform[] wardSlots;
        public DepartmentSlot[] departmentSlots;

        private IObjectPool<PatientCardView> patientPool;
        private IObjectPool<SymptomSubCardView> symptomPool;

        protected override void Awake()
        {
            base.Awake();
            
            patientPool = new ObjectPool<PatientCardView>(
                CreatePatient,
                OnGetPatient,
                OnReleasePatient,
                OnDestroyPatient,
                false, 10, 20
            );

            symptomPool = new ObjectPool<SymptomSubCardView>(
                CreateSymptom,
                OnGetSymptom,
                OnReleaseSymptom,
                OnDestroySymptom,
                false, 30, 60
            );
        }

        private PatientCardView CreatePatient()
        {
            return Instantiate(patientCardPrefab).GetComponent<PatientCardView>();
        }

        private void OnGetPatient(PatientCardView p) => p.gameObject.SetActive(true);
        private void OnReleasePatient(PatientCardView p) => p.gameObject.SetActive(false);
        private void OnDestroyPatient(PatientCardView p) => Destroy(p.gameObject);

        private SymptomSubCardView CreateSymptom()
        {
            return Instantiate(symptomSubCardPrefab).GetComponent<SymptomSubCardView>();
        }

        private void OnGetSymptom(SymptomSubCardView s) => s.gameObject.SetActive(true);
        private void OnReleaseSymptom(SymptomSubCardView s) => s.gameObject.SetActive(false);
        private void OnDestroySymptom(SymptomSubCardView s) => Destroy(s.gameObject);

        public PatientCardView GetPatientCard() => patientPool.Get();
        
        public void ReleasePatientCard(PatientCardView patient)
        {
            patient.ReleaseAllSubCards();
            patient.ClearGhost();
            patient.assignedWardSlot = null;
            patient.transform.SetParent(null);
            patientPool.Release(patient);
        }

        public SymptomSubCardView GetSymptomSubCard(Transform parent)
        {
            SymptomSubCardView symptom = symptomPool.Get();
            symptom.transform.SetParent(parent);
            symptom.transform.localPosition = Vector3.zero;
            return symptom;
        }

        public void ReleaseSymptomSubCard(SymptomSubCardView symptom)
        {
            symptom.transform.SetParent(null);
            symptomPool.Release(symptom);
        }

        public Transform GetEmptyTriageSlot(IReadOnlyList<PatientCardView> activePatients)
        {
            if (triageSlots == null) return null;
            for (int i = triageSlots.Length - 1; i >= 0; i--)
            {
                bool occupied = false;
                foreach (var p in activePatients)
                {
                    if (p != null && !p.isRemoved && p.transform.parent == triageSlots[i])
                    {
                        occupied = true;
                        break;
                    }
                }
                if (!occupied) return triageSlots[i];
            }
            return null;
        }

        public void ResetDepartmentZones()
        {
            if (departmentSlots != null)
            {
                foreach (var slot in departmentSlots)
                {
                    if (slot != null) slot.ResetZone();
                }
            }
        }
    }
}
