package com.nastya.diary.utils

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.Typeface
import android.graphics.pdf.PdfDocument
import com.nastya.diary.data.model.DiaryEntry
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.File
import java.time.LocalDate

/**
 * Экспорт дневника в PDF — вторая часть изюминки проекта.
 *
 * Дневник живёт в базе на телефоне: потерялся телефон — потерялись записи.
 * Экспорт превращает дневник в обычный файл, который можно сохранить,
 * отправить себе на почту или распечатать.
 *
 * Документ собирается средствами системы через [PdfDocument], без внешних
 * библиотек: для текстового отчёта их возможности избыточны.
 */
class PdfExporter(private val context: Context) {

    /**
     * Формирует PDF со всеми записями дневника.
     *
     * Работа идёт на [Dispatchers.IO]: отрисовка страниц и запись файла —
     * операции долгие, и в главном потоке они подвесили бы интерфейс.
     *
     * @param entries записи в том порядке, в каком они попадут в документ
     * @return файл документа
     */
    suspend fun export(entries: List<DiaryEntry>): File = withContext(Dispatchers.IO) {
        val document = PdfDocument()
        try {
            renderDocument(document, entries)
            writeToFile(document)
        } finally {
            // close обязателен в любом случае: PdfDocument держит память
            // под страницы, и без закрытия она не освободится.
            document.close()
        }
    }

    /** Рисует все страницы документа. */
    private fun renderDocument(document: PdfDocument, entries: List<DiaryEntry>) {
        var page = startPage(document, pageNumber = 1)
        var canvas = page.canvas
        var y = MARGIN + drawHeader(canvas, entries.size)

        entries.forEach { entry ->
            val entryHeight = measureEntry(entry)

            // Запись не должна разрываться между страницами на середине
            // заголовка, поэтому решение о переносе принимается заранее.
            if (y + entryHeight > PAGE_HEIGHT - MARGIN) {
                document.finishPage(page)
                page = startPage(document, document.pages.size + 1)
                canvas = page.canvas
                y = MARGIN
            }
            y = drawEntry(canvas, entry, y)
        }

        document.finishPage(page)
    }

    private fun startPage(document: PdfDocument, pageNumber: Int): PdfDocument.Page {
        val pageInfo = PdfDocument.PageInfo
            .Builder(PAGE_WIDTH, PAGE_HEIGHT, pageNumber)
            .create()
        return document.startPage(pageInfo)
    }

    /** Шапка документа: название, дата выгрузки и число записей. */
    private fun drawHeader(canvas: Canvas, entryCount: Int): Float {
        canvas.drawText("Мой дневник", MARGIN, MARGIN, titlePaint)

        val subtitle = "Выгружено ${DateFormatter.full(LocalDate.now())} · записей: $entryCount"
        canvas.drawText(subtitle, MARGIN, MARGIN + LINE_HEIGHT * 1.4f, captionPaint)

        val lineY = MARGIN + LINE_HEIGHT * 2.2f
        canvas.drawLine(MARGIN, lineY, PAGE_WIDTH - MARGIN, lineY, dividerPaint)

        return LINE_HEIGHT * 3.4f
    }

    /**
     * Рисует одну запись и возвращает координату для следующей.
     *
     * @return вертикальная позиция, с которой можно рисовать дальше
     */
    private fun drawEntry(canvas: Canvas, entry: DiaryEntry, startY: Float): Float {
        var y = startY

        val header = "${DateFormatter.full(entry.date)} · ${entry.category.name}"
        canvas.drawText(header, MARGIN, y, captionPaint)
        y += LINE_HEIGHT

        // Эмодзи здесь намеренно нет: стандартный шрифт PDF рисует его
        // пустым прямоугольником. В документе настроение обозначается словом.
        canvas.drawText("Настроение: ${moodLabel(entry)}", MARGIN, y, moodPaint)
        y += LINE_HEIGHT * 1.2f

        canvas.drawText(entry.title, MARGIN, y, entryTitlePaint)
        y += LINE_HEIGHT * 1.3f

        wrapText(entry.content, bodyPaint, PAGE_WIDTH - MARGIN * 2).forEach { line ->
            canvas.drawText(line, MARGIN, y, bodyPaint)
            y += LINE_HEIGHT
        }

        if (entry.tags.isNotEmpty()) {
            val tags = entry.tags.joinToString(separator = ", ") { "#${it.name}" }
            canvas.drawText(tags, MARGIN, y, captionPaint)
            y += LINE_HEIGHT
        }

        y += LINE_HEIGHT * 0.4f
        canvas.drawLine(MARGIN, y, PAGE_WIDTH - MARGIN, y, dividerPaint)
        return y + LINE_HEIGHT
    }

    /** Подпись настроения читается из ресурсов, чтобы не дублировать тексты. */
    private fun moodLabel(entry: DiaryEntry): String = context.getString(entry.mood.labelRes)

    /**
     * Оценивает высоту записи до отрисовки.
     *
     * Нужна, чтобы понять, поместится ли запись на текущей странице.
     */
    private fun measureEntry(entry: DiaryEntry): Float {
        val bodyLines = wrapText(entry.content, bodyPaint, PAGE_WIDTH - MARGIN * 2).size
        val tagLines = if (entry.tags.isEmpty()) 0 else 1
        return LINE_HEIGHT * (3.5f + bodyLines + tagLines + 1.4f)
    }

    /**
     * Разбивает текст на строки по ширине страницы.
     *
     * [Canvas.drawText] не переносит текст сам: длинная строка просто ушла бы
     * за край листа. Перенос делается по границам слов, а слово длиннее
     * строки разрывается принудительно — иначе оно тоже вышло бы за поля.
     */
    private fun wrapText(text: String, paint: Paint, maxWidth: Float): List<String> {
        if (text.isBlank()) return emptyList()

        val lines = mutableListOf<String>()
        text.split("\n").forEach { paragraph ->
            var current = StringBuilder()
            paragraph.split(" ").filter { it.isNotEmpty() }.forEach { word ->
                val candidate = if (current.isEmpty()) word else "$current $word"
                when {
                    paint.measureText(candidate) <= maxWidth -> {
                        current = StringBuilder(candidate)
                    }
                    current.isNotEmpty() -> {
                        lines += current.toString()
                        current = StringBuilder(word)
                    }
                    else -> {
                        // Одно слово шире строки — режем посимвольно.
                        // Хвост слова становится началом текущей строки,
                        // чтобы следующие слова могли встать за ним.
                        current = StringBuilder(splitLongWord(word, paint, maxWidth, lines))
                    }
                }
            }
            if (current.isNotEmpty()) lines += current.toString()
        }
        return lines
    }

    /**
     * Разрывает слово, которое само по себе не помещается в строку.
     *
     * Целые куски дописываются в [target], а последний, неполный, кусок
     * возвращается — он станет началом следующей строки.
     */
    private fun splitLongWord(
        word: String,
        paint: Paint,
        maxWidth: Float,
        target: MutableList<String>
    ): String {
        var chunk = StringBuilder()
        word.forEach { symbol ->
            if (paint.measureText(chunk.toString() + symbol) > maxWidth && chunk.isNotEmpty()) {
                target += chunk.toString()
                chunk = StringBuilder()
            }
            chunk.append(symbol)
        }
        return chunk.toString()
    }

    /**
     * Сохраняет документ в кеш приложения.
     *
     * Каталог `exports` внутри кеша объявлен в `res/xml/file_paths.xml`:
     * только оттуда FileProvider имеет право отдать файл другому приложению
     * при нажатии «Поделиться».
     */
    private fun writeToFile(document: PdfDocument): File {
        val directory = File(context.cacheDir, EXPORT_DIRECTORY).apply { mkdirs() }
        val file = File(directory, FILE_NAME)
        file.outputStream().use { stream -> document.writeTo(stream) }
        return file
    }

    // ---------- Оформление документа ----------

    private val titlePaint = Paint().apply {
        color = Color.BLACK
        textSize = 24f
        typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        isAntiAlias = true
    }

    private val entryTitlePaint = Paint().apply {
        color = Color.BLACK
        textSize = 14f
        typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        isAntiAlias = true
    }

    private val bodyPaint = Paint().apply {
        color = Color.DKGRAY
        textSize = 12f
        isAntiAlias = true
    }

    private val captionPaint = Paint().apply {
        color = Color.GRAY
        textSize = 10f
        isAntiAlias = true
    }

    private val moodPaint = Paint().apply {
        color = Color.rgb(139, 124, 246)
        textSize = 11f
        isAntiAlias = true
    }

    private val dividerPaint = Paint().apply {
        color = Color.LTGRAY
        strokeWidth = 0.5f
    }

    private companion object {
        /** Размер листа A4 в точках при 72 dpi — стандарт для PDF. */
        const val PAGE_WIDTH = 595
        const val PAGE_HEIGHT = 842

        const val MARGIN = 40f
        const val LINE_HEIGHT = 16f

        const val EXPORT_DIRECTORY = "exports"
        const val FILE_NAME = "diary.pdf"
    }
}
