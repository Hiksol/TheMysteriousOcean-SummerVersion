using TMPro;
using UnityEngine;

public class InventoryRendererCell : MonoBehaviour
{
    public TMP_Text itemText;
    public TMP_Text annotationText;

    void Start() {
        itemText.text = "";
        annotationText.text = "";
    }

    public void SetCellText(string text) {
        itemText.text = text;
    }

    public void SetAnnotationText(string text) {
        annotationText.text = text;
    }
}
