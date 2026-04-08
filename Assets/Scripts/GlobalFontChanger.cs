using UnityEngine;
using TMPro;

public class GlobalFontChanger : MonoBehaviour
{
    public TMP_FontAsset newFont;

    [ContextMenu("Change All Fonts")] // Inspector mein right-click se chalanay ke liye
    public void ChangeAllFonts()
    {
        TMP_Text[] allText = FindObjectsOfType<TMP_Text>(true); // Saare text objects dhundo
        foreach (TMP_Text text in allText)
        {
            text.font = newFont;
        }
        Debug.Log("Saray fonts change ho gaye hain!");
    }
}