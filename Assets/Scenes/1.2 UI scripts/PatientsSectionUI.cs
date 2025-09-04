using UnityEngine;

public class PatientsSectionUI : MonoBehaviour
{
    public Transform cardsRow;      // контейнер с Horizontal Layout Group
    public GameObject cardPrefab;   // PatientCardPrefab

    void OnEnable()
    {
        var pm = PatientManager.Instance;
        if (pm != null)
        {
            pm.OnPatientsChanged += Rebuild;
            Rebuild();
        }
    }

    void OnDisable()
    {
        var pm = PatientManager.Instance;
        if (pm != null) pm.OnPatientsChanged -= Rebuild;
    }

    public void Rebuild()
    {
        var pm = PatientManager.Instance;
        Debug.Log($"[PatientsSectionUI] Rebuild: pm.patients.Length = {(pm?.patients?.Length ?? 0)}");

        if (pm == null || cardsRow == null || cardPrefab == null) return;

        // очистить только карточки
        for (int i = cardsRow.childCount - 1; i >= 0; i--)
        {
            var ch = cardsRow.GetChild(i);
            if (ch.GetComponent<PatientCardView>() != null)
                Destroy(ch.gameObject);
        }

        // создать по всем пациентам
        foreach (var p in pm.patients ?? System.Array.Empty<Patient>())
        {
            var go = Instantiate(cardPrefab, cardsRow);
            go.transform.localScale = Vector3.one;
            var view = go.GetComponent<PatientCardView>();
            if (view) view.Bind(p);
        }
    }
}
