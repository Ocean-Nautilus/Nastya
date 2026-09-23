using UnityEngine;
using TMPro;

public class SearchController : MonoBehaviour
{
    public DocumentManager documentManager;
    public TMP_InputField searchInputField;

    // Кнопка "Поиск" и Enter в поле поиска (On Submit).
    // Повторное нажатие переходит к следующему совпадению.
    public void OnSearchButtonClicked()
    {
        if (documentManager != null && searchInputField != null)
        {
            documentManager.SearchText(searchInputField.text);
        }
    }

    // Изменился искомый текст - следующий поиск начнётся с начала документа
    public void OnSearchTextChanged(string searchText)
    {
        if (documentManager != null)
        {
            documentManager.ResetSearch();
        }
    }
}
