using UnityEngine;
using UnityEngine.UI; // для LayoutRebuilder

[DisallowMultipleComponent]
public class PatientsSectionUI : MonoBehaviour
{
    // CardsContent (контейнер с Horizontal Layout Group)
    public Transform cardsRow;

    // Префаб карточки (ваш Patient_1_Card из Project)
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

        // 1) Полностью очищаем контейнер
        for (int i = cardsRow.childCount - 1; i >= 0; i--)
        {
            var ch = cardsRow.GetChild(i);
            Destroy(ch.gameObject);
        }

        // 2) Создаём карточки заново по данным
        foreach (var p in data)
        {
            var go = Instantiate(cardPrefab, cardsRow);
            go.transform.localScale = Vector3.one;

            var view = go.GetComponent<PatientCardView>();
            if (view != null) view.Bind(p);
        }

        // 3) Принудительно обновить лэйаут, чтобы ширина контента пересчиталась
        var rt = cardsRow as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
}
