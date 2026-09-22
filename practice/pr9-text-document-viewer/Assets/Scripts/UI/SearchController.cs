using UnityEngine;
using UnityEngine.UI;

public class SearchController : MonoBehaviour
{
    [Header("Связи")]
    public DocumentManager documentManager;

    [Header("UI Components")]
    public GameObject searchPanel;
    public InputField searchInputField;
    public InputField replaceInputField;
    public Text resultText;

    void Start()
    {
        // Панель поиска скрыта до первого нажатия кнопки «Поиск»
        if (searchPanel != null)
        {
            searchPanel.SetActive(false);
        }
    }

    // Кнопка «Поиск» на панели инструментов
    public void ToggleSearchPanel()
    {
        if (searchPanel == null)
        {
            return;
        }

        bool willBeVisible = !searchPanel.activeSelf;
        searchPanel.SetActive(willBeVisible);

        if (willBeVisible && searchInputField != null)
        {
            searchInputField.Select();
            searchInputField.ActivateInputField();
        }
    }

    public void CloseSearchPanel()
    {
        if (searchPanel != null)
        {
            searchPanel.SetActive(false);
        }
    }

    // Кнопка «Найти»
    public void OnSearchButtonClicked()
    {
        if (documentManager == null || searchInputField == null)
        {
            return;
        }

        string searchTerm = searchInputField.text;
        documentManager.SearchText(searchTerm);
        ShowResult(searchTerm);
    }

    // Кнопка «Найти далее»
    public void OnFindNextClicked()
    {
        if (documentManager == null || searchInputField == null)
        {
            return;
        }

        string searchTerm = searchInputField.text;
        bool found = documentManager.FindNext(searchTerm);

        if (found)
        {
            ShowResult(searchTerm);
        }
        else
        {
            SetResultText("Совпадений не найдено");
        }
    }

    // Кнопка «Заменить»
    public void OnReplaceClicked()
    {
        if (documentManager == null || searchInputField == null || replaceInputField == null)
        {
            return;
        }

        bool replaced = documentManager.ReplaceCurrent(searchInputField.text, replaceInputField.text);
        SetResultText(replaced ? "Заменено одно вхождение" : "Совпадений не найдено");
    }

    // Кнопка «Заменить все»
    public void OnReplaceAllClicked()
    {
        if (documentManager == null || searchInputField == null || replaceInputField == null)
        {
            return;
        }

        int count = documentManager.ReplaceAll(searchInputField.text, replaceInputField.text);
        SetResultText("Выполнено замен: " + count);
    }

    // Живой поиск: вызывается при изменении текста в поле поиска
    public void OnSearchTextChanged(string searchText)
    {
        if (documentManager == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(searchText))
        {
            SetResultText("");
            return;
        }

        ShowResult(searchText);
    }

    private void ShowResult(string searchTerm)
    {
        int count = documentManager.CountOccurrences(searchTerm);
        SetResultText(count > 0 ? "Найдено совпадений: " + count : "Совпадений не найдено");
    }

    private void SetResultText(string message)
    {
        if (resultText != null)
        {
            resultText.text = message;
        }
    }
}
