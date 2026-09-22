Сюда кладут ассеты шрифтов TextMeshPro (TMP Font Asset).

Как сделать ассет из обычного .ttf:
1. Скопируйте .ttf в любую папку проекта.
2. Window -> TextMeshPro -> Font Asset Creator.
3. Source Font File - ваш шрифт, нажмите Generate Font Atlas, затем Save.
4. Сохраните полученный ассет в эту папку (Assets/Resources/Fonts).

Именно отсюда FormatController берёт список шрифтов
методом Resources.LoadAll<TMP_FontAsset>("Fonts").
Пока папка пуста, в выпадающем списке будет надпись "Шрифты не найдены".
