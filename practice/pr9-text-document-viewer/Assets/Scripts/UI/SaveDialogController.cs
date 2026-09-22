using UnityEngine;
using UnityEngine.UI;

public class SaveDialogController : MonoBehaviour
{
    [Header("Связи")]
    public DocumentManager documentManager;

    [Header("UI Components")]
    public GameObject dialogPanel;
    public InputField fileNameInput;
    public Text hintText;

    void Start()
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }
    }

    // Показать диалог с подставленным именем документа
    public void Show(string suggestedName)
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
        }

        if (fileNameInput != null)
        {
            fileNameInput.text = System.IO.Path.GetFileNameWithoutExtension(suggestedName);
            fileNameInput.Select();
            fileNameInput.ActivateInputField();
        }

        if (hintText != null)
        {
            hintText.text = "Файл будет сохранён в папку: " + DocumentPaths.DocumentsFolder;
        }
    }

    // Кнопка «Сохранить» в диалоге
    public void OnConfirmClicked()
    {
        if (documentManager == null || fileNameInput == null)
        {
            return;
        }

        string fileName = fileNameInput.text.Trim();

        if (string.IsNullOrEmpty(fileName))
        {
            if (hintText != null)
            {
                hintText.text = "Введите имя файла";
            }
            return;
        }

        documentManager.SaveWithName(fileName);
        Hide();
    }

    // Кнопка «Отмена»
    public void Hide()
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }
    }
}
