using TMPro;
using UnityEngine;

public class InventoryRendererCell : MonoBehaviour
{
    public TMP_Text itemText;

    void Start() {
        itemText.text = "";
    }

    public void SetCellText(string text) {
        itemText.text = text;
    }
}
