using UnityEngine;
using TMPro;

public class PatientCardBinder : MonoBehaviour
{
    public PatientSelectButton selectButton; // component on the card
    public TMP_Text nameText;                // optional label

    public void Bind(Patient p)
    {
        if (selectButton) selectButton.patientId = p.id;
        if (nameText) nameText.text = p.displayName;
    }
}
