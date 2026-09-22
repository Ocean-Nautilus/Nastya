using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FormatController : MonoBehaviour
{
    [Header("UI Components")]
    public InputField textInputField;
    public Dropdown fontDropdown;
    public InputField fontSizeInput;

    [Header("Ограничения размера шрифта")]
    public int minFontSize = 8;
    public int maxFontSize = 72;

    private Font[] availableFonts;

    void Start()
    {
        InitializeFonts();
        InitializeFontSize();
    }

    // Заполняем выпадающий список шрифтами из папки Assets/Resources/Fonts
    private void InitializeFonts()
    {
        availableFonts = Resources.LoadAll<Font>("Fonts");

        if (fontDropdown == null)
        {
            return;
        }

        List<string> fontNames = new List<string>();

        foreach (Font font in availableFonts)
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

        fontSizeInput.contentType = InputField.ContentType.IntegerNumber;
        fontSizeInput.text = textInputField.textComponent.fontSize.ToString();
        fontSizeInput.onEndEdit.AddListener(OnFontSizeChanged);
    }

    // Смена шрифта
    public void OnFontSelected(int index)
    {
        if (availableFonts == null || index < 0 || index >= availableFonts.Length)
        {
            return;
        }

        ApplyFont(availableFonts[index]);
    }

    private void ApplyFont(Font font)
    {
        if (textInputField == null || font == null)
        {
            return;
        }

        textInputField.textComponent.font = font;

        if (textInputField.placeholder != null)
        {
            Text placeholder = textInputField.placeholder.GetComponent<Text>();
            if (placeholder != null)
            {
                placeholder.font = font;
            }
        }

        textInputField.ForceLabelUpdate();
    }

    // Смена размера шрифта
    public void OnFontSizeChanged(string value)
    {
        if (textInputField == null)
        {
            return;
        }

        int size;
        if (!int.TryParse(value, out size))
        {
            // Некорректный ввод — возвращаем текущее значение
            if (fontSizeInput != null)
            {
                fontSizeInput.text = textInputField.textComponent.fontSize.ToString();
            }
            return;
        }

        size = Mathf.Clamp(size, minFontSize, maxFontSize);
        textInputField.textComponent.fontSize = size;

        if (textInputField.placeholder != null)
        {
            Text placeholder = textInputField.placeholder.GetComponent<Text>();
            if (placeholder != null)
            {
                placeholder.fontSize = size;
            }
        }

        textInputField.ForceLabelUpdate();

        if (fontSizeInput != null)
        {
            fontSizeInput.text = size.ToString();
        }
    }

    // Кнопки увеличения и уменьшения размера
    public void IncreaseFontSize()
    {
        ChangeFontSize(1);
    }

    public void DecreaseFontSize()
    {
        ChangeFontSize(-1);
    }

    private void ChangeFontSize(int delta)
    {
        if (textInputField == null)
        {
            return;
        }

        OnFontSizeChanged((textInputField.textComponent.fontSize + delta).ToString());
    }

    // Кнопки форматирования выделенного текста
    public void OnBoldButtonClicked()
    {
        ApplyFormatting("**", "**");
    }

    public void OnItalicButtonClicked()
    {
        ApplyFormatting("*", "*");
    }

    public void OnUnderlineButtonClicked()
    {
        ApplyFormatting("__", "__");
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
            string newText = prefix + selectedText + suffix;
            ReplaceSelectedText(newText);
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
            textInputField.text = currentText.Substring(0, start) + newText + currentText.Substring(end);

            // Ставим курсор после вставленного фрагмента
            textInputField.caretPosition = start + newText.Length;
            textInputField.ForceLabelUpdate();
        }
    }
}
