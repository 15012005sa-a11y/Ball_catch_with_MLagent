using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PeriodDropdownInit : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    void Start()
    {
        if (!dropdown) dropdown = GetComponent<TMP_Dropdown>();
        if (!dropdown) return;

        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "7D", "30D", "All" });
        dropdown.value = 0; // 7D
        dropdown.RefreshShownValue();
    }
}
