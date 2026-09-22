using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class AutoSaveController : MonoBehaviour
{
    [Header("Связи")]
    public DocumentManager documentManager;
    public Text autoSaveStatusText;

    [Header("Настройки")]
    public bool autoSaveEnabled = true;
    public float autoSaveInterval = 60f;

    public const string RecoveryFileName = "autosave_recovery.txt";

    void Start()
    {
        StartCoroutine(AutoSaveRoutine());
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);

            if (autoSaveEnabled)
            {
                PerformAutoSave();
            }
        }
    }

    private void PerformAutoSave()
    {
        if (documentManager == null || !documentManager.HasUnsavedChanges)
        {
            return;
        }

        if (!string.IsNullOrEmpty(documentManager.CurrentFilePath))
        {
            // У документа уже есть файл — просто сохраняем его
            documentManager.SaveDocument();
            SetStatus("Автосохранение: " + System.DateTime.Now.ToString("HH:mm:ss"));
            return;
        }

        // Документ ещё ни разу не сохраняли — пишем во временный файл
        try
        {
            string recoveryPath = Path.Combine(DocumentPaths.DocumentsFolder, RecoveryFileName);
            File.WriteAllText(recoveryPath, documentManager.CurrentDocument.content);
            SetStatus("Черновик сохранён: " + System.DateTime.Now.ToString("HH:mm:ss"));
        }
        catch (System.Exception e)
        {
            Debug.LogError("Ошибка автосохранения: " + e.Message);
        }
    }

    // Переключатель автосохранения (Toggle на панели инструментов)
    public void SetAutoSaveEnabled(bool enabled)
    {
        autoSaveEnabled = enabled;
        SetStatus(enabled ? "Автосохранение включено" : "Автосохранение выключено");
    }

    private void SetStatus(string message)
    {
        if (autoSaveStatusText != null)
        {
            autoSaveStatusText.text = message;
        }

        Debug.Log(message);
    }
}
