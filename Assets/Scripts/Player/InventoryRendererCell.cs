using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryRendererCell : MonoBehaviour
{
    public TMP_Text itemText;
    public TMP_Text annotationText;
    public Image itemIcon;

    void Start()
    {
        itemText.text = "";
        annotationText.text = "";
        if (itemIcon != null) itemIcon.enabled = false;
    }

    public void SetCellText(string text)
    {
        itemText.text = text;
    }

    public void SetAnnotationText(string text)
    {
        annotationText.text = text;
    }

    public void SetCellIcon(Sprite sprite)
    {
        if (itemIcon == null) return;

        if (sprite == null)
        {
            itemIcon.enabled = false;
            itemIcon.sprite = null;
        }
        else
        {
            itemIcon.enabled = true;
            itemIcon.sprite = sprite;
        }
    }
}
