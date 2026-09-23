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
    public int wordCount;
    public int lineCount;

    public TextDocument()
    {
        documentName = "Новый документ";
        content = "";
        createdDate = DateTime.Now;
        modifiedDate = DateTime.Now;
        UpdateStatistics();
    }

    public void UpdateStatistics()
    {
        characterCount = content.Length;
        wordCount = string.IsNullOrEmpty(content) ? 0 : content.Split(new char[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        lineCount = string.IsNullOrEmpty(content) ? 0 : content.Split('\n').Length;
        modifiedDate = DateTime.Now;
    }

    public void Clear()
    {
        content = "";
        documentName = "Новый документ";
        UpdateStatistics();
    }
}
