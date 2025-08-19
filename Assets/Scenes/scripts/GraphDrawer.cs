using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// ������ ������ � UI-���������� (RectTransform) � ������� Image-�����.
[RequireComponent(typeof(RectTransform))]
public class GraphDrawer : MonoBehaviour
{
    [Header("Look")]
    public Color series1Color = new Color(0.2f, 0.8f, 1f, 1f);   // ���� 1-� ����� (��������, ������ ���� / accuracy)
    public Color series2Color = new Color(1f, 0.4f, 0.8f, 1f);   // ���� 2-� ����� (��������, ����� ����)
    public float lineThickness = 3f;
    public float topPaddingPercent = 0.1f;                      // ����� ������ (10%)

    // ���������� ������
    private RectTransform _rt;
    private RectTransform _root;  // ����, � ������� ����� ��� �������� � ����� �� ����� �������

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        EnsureRoot();
    }

    void EnsureRoot()
    {
        if (_root != null) return;
        var go = new GameObject("__GraphRoot", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        _root = go.GetComponent<RectTransform>();
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = _root.offsetMax = Vector2.zero;
    }

    /// �������� ���������� �������.
    public void Clear()
    {
        if (_root == null) EnsureRoot();
        for (int i = _root.childCount - 1; i >= 0; i--)
            Destroy(_root.GetChild(i).gameObject);
    }

    /// ���������� 1 ��� 2 ����� ������ (�������� >= 0).
    /// ������: Draw(accuracyList, null) ��� Draw(rightAngles, leftAngles, 0, 180);
    public void Draw(IReadOnlyList<float> data1, IReadOnlyList<float> data2 = null, float minY = 0f, float maxY = -1f)
    {
        EnsureRoot();
        Clear();

        if ((data1 == null || data1.Count < 2) &&
            (data2 == null || data2.Count < 2))
        {
            // ������ ��������
            return;
        }

        var w = _rt.rect.width;
        var h = _rt.rect.height;

        // ����� �������� �� ���� ������
        float maxVal = 0f;
        if (data1 != null && data1.Count > 0) maxVal = Mathf.Max(maxVal, data1.Max());
        if (data2 != null && data2.Count > 0) maxVal = Mathf.Max(maxVal, data2.Max());

        if (maxY > 0f) maxVal = maxY;           // ��������������� �����
        if (maxVal <= 0f) maxVal = 1f;          // ������ �� ������� �� 0
        float yPad = maxVal * topPaddingPercent;

        // ��������� �������: ������� �������� � UI-����������
        Vector2 Pos(int i, int count, float value)
        {
            float x = (count <= 1) ? 0f : (i / (float)(count - 1)) * w;
            float y = Mathf.InverseLerp(minY, maxVal + yPad, Mathf.Max(minY, value)) * h;
            return new Vector2(x, y);
        }

        // ����� 1
        if (data1 != null && data1.Count >= 2)
        {
            for (int i = 0; i < data1.Count - 1; i++)
            {
                CreateLine(Pos(i, data1.Count, data1[i]),
                           Pos(i + 1, data1.Count, data1[i + 1]),
                           series1Color);
            }
        }

        // ����� 2 (�����������)
        if (data2 != null && data2.Count >= 2)
        {
            for (int i = 0; i < data2.Count - 1; i++)
            {
                CreateLine(Pos(i, data2.Count, data2[i]),
                           Pos(i + 1, data2.Count, data2[i + 1]),
                           series2Color);
            }
        }
    }

    // ������ UI-����� ��� �������������, ���������� ��� ������ ����
    void CreateLine(Vector2 a, Vector2 b, Color col)
    {
        var go = new GameObject("seg", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_root, false);

        var img = go.GetComponent<Image>();
        img.color = col;
        img.raycastTarget = false; // �� ����������� ������

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        Vector2 dir = (b - a);
        float len = dir.magnitude;
        rt.sizeDelta = new Vector2(len, lineThickness);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = a;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
