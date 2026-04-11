using UnityEngine;
using TMPro;

public class GlobalFontChanger : MonoBehaviour
{
    public TMP_FontAsset newFont;

    [ContextMenu("Change All Fonts")] 
    public void ChangeAllFonts()
    {
        TMP_Text[] allText = FindObjectsOfType<TMP_Text>(true); 
        foreach (TMP_Text text in allText)
        {
            text.font = newFont;
        }
        Debug.Log("All fonts changed");
    }
}