using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FormatController : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_InputField textInputField;
    public TMP_Dropdown fontDropdown;
    public TMP_InputField fontSizeInput;

    [Header("Ограничения размера шрифта")]
    public float minFontSize = 8f;
    public float maxFontSize = 72f;

    private TMP_FontAsset[] availableFonts;

    void Start()
    {
        InitializeFonts();
        InitializeFontSize();
    }

    // Список шрифтов берём из папки Assets/Resources/Fonts
    private void InitializeFonts()
    {
        availableFonts = Resources.LoadAll<TMP_FontAsset>("Fonts");

        if (fontDropdown == null)
        {
            return;
        }

        List<string> fontNames = new List<string>();

        foreach (TMP_FontAsset font in availableFonts)
        {
            fontNames.Add(font.name);
        }

        if (fontNames.Count == 0)
        {
            fontNames.Add("Шрифты не найдены");
        }

        fontDropdown.ClearOptions();
        fontDropdown.AddOptions(fontNames);
        fontDropdown.onValueChanged.AddListener(OnFontSelected);
    }

    private void InitializeFontSize()
    {
        if (fontSizeInput == null || textInputField == null)
        {
            return;
        }

        fontSizeInput.contentType = TMP_InputField.ContentType.DecimalNumber;
        fontSizeInput.text = textInputField.textComponent.fontSize.ToString();
        fontSizeInput.onEndEdit.AddListener(OnFontSizeChanged);
    }

    // Смена шрифта
    public void OnFontSelected(int index)
    {
        if (availableFonts == null || textInputField == null)
        {
            return;
        }

        if (index < 0 || index >= availableFonts.Length)
        {
            return;
        }

        textInputField.textComponent.font = availableFonts[index];

        TMP_Text placeholder = GetPlaceholder();
        if (placeholder != null)
        {
            placeholder.font = availableFonts[index];
        }

        textInputField.ForceLabelUpdate();
    }

    // Смена размера шрифта всего поля
    public void OnFontSizeChanged(string value)
    {
        if (textInputField == null)
        {
            return;
        }

        float size;
        if (!float.TryParse(value, out size))
        {
            fontSizeInput.text = textInputField.textComponent.fontSize.ToString();
            return;
        }

        size = Mathf.Clamp(size, minFontSize, maxFontSize);
        textInputField.textComponent.fontSize = size;

        TMP_Text placeholder = GetPlaceholder();
        if (placeholder != null)
        {
            placeholder.fontSize = size;
        }

        textInputField.ForceLabelUpdate();
        fontSizeInput.text = size.ToString();
    }

    private TMP_Text GetPlaceholder()
    {
        if (textInputField == null || textInputField.placeholder == null)
        {
            return null;
        }

        return textInputField.placeholder.GetComponent<TMP_Text>();
    }

    // Кнопки форматирования: TextMeshPro понимает теги Rich Text
    public void OnBoldButtonClicked()
    {
        ApplyFormatting("<b>", "</b>");
    }

    public void OnItalicButtonClicked()
    {
        ApplyFormatting("<i>", "</i>");
    }

    public void OnUnderlineButtonClicked()
    {
        ApplyFormatting("<u>", "</u>");
    }

    private void ApplyFormatting(string prefix, string suffix)
    {
        if (textInputField == null)
        {
            return;
        }

        string selectedText = GetSelectedText();

        if (!string.IsNullOrEmpty(selectedText))
        {
            ReplaceSelectedText(prefix + selectedText + suffix);
        }
        else
        {
            Debug.Log("Сначала выделите текст мышью, потом нажмите кнопку форматирования");
        }
    }

    private string GetSelectedText()
    {
        // Получение выделенного текста
        int start = textInputField.selectionAnchorPosition;
        int end = textInputField.selectionFocusPosition;

        if (start > end)
        {
            int temp = start;
            start = end;
            end = temp;
        }

        if (start >= 0 && end <= textInputField.text.Length)
        {
            return textInputField.text.Substring(start, end - start);
        }

        return "";
    }

    private void ReplaceSelectedText(string newText)
    {
        int start = textInputField.selectionAnchorPosition;
        int end = textInputField.selectionFocusPosition;

        if (start > end)
        {
            int temp = start;
            start = end;
            end = temp;
        }

        if (start >= 0 && end <= textInputField.text.Length)
        {
            string currentText = textInputField.text;
            textInputField.text = currentText.Substring(0, start)
                + newText + currentText.Substring(end);

            // Курсор ставим после вставленного фрагмента
            textInputField.caretPosition = start + newText.Length;
            textInputField.ForceLabelUpdate();
        }
    }
}
