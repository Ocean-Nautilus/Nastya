using System.Collections;
using UnityEngine;
using TMPro;

// Общие методы для работы с выделением в TMP_InputField.
// Позиции везде - индексы в строке text (вместе с тегами форматирования),
// поэтому используются selectionString*Position. Свойства selection*Position
// считают только видимые символы и "съезжают", как только в тексте появляются теги.
public static class InputFieldSelection
{
    public static void GetRange(TMP_InputField field, out int start, out int end)
    {
        start = field.selectionStringAnchorPosition;
        end = field.selectionStringFocusPosition;

        if (start > end)
        {
            int temp = start;
            start = end;
            end = temp;
        }

        start = Mathf.Clamp(start, 0, field.text.Length);
        end = Mathf.Clamp(end, 0, field.text.Length);
    }

    // Поле активируется не сразу, а в LateUpdate, и при активации сбрасывает
    // выделение. Поэтому сначала ждём, пока поле получит фокус, и только потом выделяем.
    public static IEnumerator Select(TMP_InputField field, int start, int end)
    {
        if (!field.isFocused)
        {
            field.Select();
            field.ActivateInputField();

            for (int i = 0; i < 10 && !field.isFocused; i++)
                yield return null;
        }

        field.selectionStringAnchorPosition = start;
        field.selectionStringFocusPosition = end;
        field.ForceLabelUpdate();
    }
}
