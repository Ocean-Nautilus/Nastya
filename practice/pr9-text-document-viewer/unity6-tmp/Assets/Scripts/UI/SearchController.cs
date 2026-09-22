using UnityEngine;
using TMPro;

public class SearchController : MonoBehaviour
{
    public DocumentManager documentManager;
    public TMP_InputField searchInputField;

    public void OnSearchButtonClicked()
    {
        if (documentManager != null && searchInputField != null)
        {
            documentManager.SearchText(searchInputField.text);
        }
    }

    public void OnSearchTextChanged(string searchText)
    {
        // Живой поиск: включите, если нужно искать прямо во время ввода
        // if (documentManager != null) documentManager.SearchText(searchText);
    }
}
