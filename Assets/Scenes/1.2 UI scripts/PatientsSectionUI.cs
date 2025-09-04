using UnityEngine;
using UnityEngine.UI; // ��� LayoutRebuilder

[DisallowMultipleComponent]
public class PatientsSectionUI : MonoBehaviour
{
    // CardsContent (��������� � Horizontal Layout Group)
    public Transform cardsRow;

    // ������ �������� (��� Patient_1_Card �� Project)
    public GameObject cardPrefab;

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
        if (pm == null || cardsRow == null || cardPrefab == null) return;

        var data = pm.patients ?? System.Array.Empty<Patient>();
        Debug.Log($"[PatientsSectionUI] Rebuild: count = {data.Length}");

        // 1) ��������� ������� ���������
        for (int i = cardsRow.childCount - 1; i >= 0; i--)
        {
            var ch = cardsRow.GetChild(i);
            Destroy(ch.gameObject);
        }

        // 2) ������ �������� ������ �� ������
        foreach (var p in data)
        {
            var go = Instantiate(cardPrefab, cardsRow);
            go.transform.localScale = Vector3.one;

            var view = go.GetComponent<PatientCardView>();
            if (view != null) view.Bind(p);
        }

        // 3) ������������� �������� ������, ����� ������ �������� �������������
        var rt = cardsRow as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
}
