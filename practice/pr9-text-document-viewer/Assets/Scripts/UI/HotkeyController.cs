using UnityEngine;

public class HotkeyController : MonoBehaviour
{
    [Header("Связи")]
    public DocumentManager documentManager;
    public SearchController searchController;

    void Update()
    {
        if (documentManager == null)
        {
            return;
        }

        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (!ctrl)
        {
            return;
        }

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Ctrl+N — новый документ
        if (Input.GetKeyDown(KeyCode.N))
        {
            documentManager.CreateNewDocument();
        }

        // Ctrl+O — открыть документ
        if (Input.GetKeyDown(KeyCode.O))
        {
            documentManager.LoadDocument();
        }

        // Ctrl+S — сохранить, Ctrl+Shift+S — сохранить как
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (shift)
            {
                documentManager.SaveDocumentAs();
            }
            else
            {
                documentManager.SaveDocument();
            }
        }

        // Ctrl+F — панель поиска
        if (Input.GetKeyDown(KeyCode.F) && searchController != null)
        {
            searchController.ToggleSearchPanel();
        }

        // Ctrl+Q — выход с проверкой несохранённых изменений
        if (Input.GetKeyDown(KeyCode.Q))
        {
            documentManager.QuitApplication();
        }
    }
}
