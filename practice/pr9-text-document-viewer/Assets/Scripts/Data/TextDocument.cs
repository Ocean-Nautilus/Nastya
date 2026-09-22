using System;
using UnityEngine;

[Serializable]
public class TextDocument
{
    public string documentName;
    public string filePath;
    public string content;
    public DateTime createdDate;
    public DateTime modifiedDate;
    public int characterCount;
    public int charactersWithoutSpaces;
    public int wordCount;
    public int lineCount;
    public int paragraphCount;

    public TextDocument()
    {
        documentName = "Новый документ";
        filePath = "";
        content = "";
        createdDate = DateTime.Now;
        modifiedDate = DateTime.Now;
        UpdateStatistics();
    }

    // Пересчёт статистики документа
    public void UpdateStatistics()
    {
        if (content == null)
        {
            content = "";
        }

        characterCount = content.Length;
        charactersWithoutSpaces = content.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "").Length;

        wordCount = string.IsNullOrEmpty(content)
            ? 0
            : content.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        lineCount = string.IsNullOrEmpty(content) ? 0 : content.Split('\n').Length;
        paragraphCount = CountParagraphs();

        modifiedDate = DateTime.Now;
    }

    // Абзац — группа непустых строк, отделённая пустой строкой
    private int CountParagraphs()
    {
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        int paragraphs = 0;
        bool insideParagraph = false;

        foreach (string line in content.Split('\n'))
        {
            if (line.Trim().Length == 0)
            {
                insideParagraph = false;
            }
            else if (!insideParagraph)
            {
                insideParagraph = true;
                paragraphs++;
            }
        }

        return paragraphs;
    }

    // Примерное время чтения: средняя скорость — 180 слов в минуту
    public int GetReadingTimeMinutes()
    {
        if (wordCount == 0)
        {
            return 0;
        }

        return Mathf.Max(1, Mathf.CeilToInt(wordCount / 180f));
    }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(content);
    }

    public void Clear()
    {
        content = "";
        documentName = "Новый документ";
        filePath = "";
        createdDate = DateTime.Now;
        UpdateStatistics();
    }
}
