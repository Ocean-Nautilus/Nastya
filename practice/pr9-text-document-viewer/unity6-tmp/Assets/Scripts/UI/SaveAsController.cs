using UnityEngine;
using TMPro;

public class SaveAsController : MonoBehaviour
{
    [Header("Ссылки")]
    public DocumentManager documentManager;
    public GameObject panelRoot;
    public TMP_InputField fileNameInput;

    void Start()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void Show(string currentName)
    {
        // Имя файла показываем без расширения — его подставит менеджер
        if (currentName.EndsWith(".txt"))
        {
            currentName = currentName.Substring(0, currentName.Length - 4);
        }

        fileNameInput.text = currentName;
        panelRoot.SetActive(true);

        fileNameInput.Select();
        fileNameInput.ActivateInputField();
    }

    public void OnConfirmClicked()
    {
        documentManager.SaveDocumentAsNamed(fileNameInput.text);
        panelRoot.SetActive(false);
    }

    public void OnCancelClicked()
    {
        panelRoot.SetActive(false);
    }
}
