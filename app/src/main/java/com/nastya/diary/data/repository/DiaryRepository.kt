package com.nastya.diary.data.repository

import androidx.room.withTransaction
import com.nastya.diary.data.database.AppDatabase
import com.nastya.diary.data.database.dao.CategoryUsage
import com.nastya.diary.data.database.entity.CategoryEntity
import com.nastya.diary.data.database.entity.EntryEntity
import com.nastya.diary.data.database.entity.EntryTagCrossRef
import com.nastya.diary.data.database.entity.TagEntity
import com.nastya.diary.data.model.Category
import com.nastya.diary.data.model.DiaryEntry
import com.nastya.diary.data.model.EntryFilter
import com.nastya.diary.data.model.Tag
import kotlinx.coroutines.CoroutineDispatcher
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.withContext
import java.time.LocalDate
import java.time.ZoneOffset

/**
 * Единая точка доступа к данным дневника.
 *
 * Репозиторий — средний слой архитектуры MVVM: ViewModel обращается только
 * сюда и работает с доменными моделями, ничего не зная ни про Room, ни про
 * устройство таблиц. Если завтра базу заменить, менять придётся один этот
 * класс, а не экраны.
 *
 * Все операции выполняются на [Dispatchers.IO], поэтому главный поток
 * не блокируется — требование ТЗ «работа с БД в фоновом потоке».
 *
 * @param database база данных приложения
 * @param dispatcher поток для операций ввода-вывода; подменяется в тестах
 */
class DiaryRepository(
    private val database: AppDatabase,
    private val dispatcher: CoroutineDispatcher = Dispatchers.IO
) {

    private val entryDao = database.entryDao()
    private val categoryDao = database.categoryDao()
    private val tagDao = database.tagDao()

    /**
     * Последняя удалённая запись — держится в памяти, чтобы удаление можно
     * было отменить. Переживать перезапуск приложения ей незачем: отмена
     * имеет смысл только сразу после нажатия.
     */
    private var lastDeletedEntry: DiaryEntry? = null

    // ---------- Записи ----------

    /** Поток всех записей дневника, сначала новые. */
    fun observeEntries(): Flow<List<DiaryEntry>> =
        entryDao.observeAll().map { rows -> rows.map { it.toDomain() } }

    /**
     * Поток записей, отобранных по поиску, фильтрам и сортировке.
     *
     * Даты переводятся в миллисекунды тем же способом, что и при сохранении
     * (полночь по UTC), иначе граница периода сдвигалась бы на часовой пояс
     * и записи «выпадали» бы из выборки на сутки.
     */
    fun observeEntries(filter: EntryFilter): Flow<List<DiaryEntry>> =
        entryDao.observeFiltered(
            query = filter.query.trim(),
            mood = filter.mood?.name,
            categoryId = filter.categoryId,
            fromDate = filter.fromDate?.toEpochMillis(),
            toDate = filter.toDate?.toEpochMillis(),
            sortOrder = filter.sortOrder.sqlValue
        ).map { rows -> rows.map { it.toDomain() } }

    /** Поток одной записи; отдаёт `null`, если запись удалили. */
    fun observeEntry(entryId: Long): Flow<DiaryEntry?> =
        entryDao.observeById(entryId).map { it?.toDomain() }

    /** Общее количество записей — показатель на экране статистики. */
    fun observeEntryCount(): Flow<Int> = entryDao.observeTotalCount()

    /** Разовое чтение записи, например при открытии её на редактирование. */
    suspend fun getEntry(entryId: Long): DiaryEntry? = withContext(dispatcher) {
        entryDao.getById(entryId)?.toDomain()
    }

    /** Все записи разом — используется при экспорте дневника в PDF. */
    suspend fun getAllEntries(): List<DiaryEntry> = withContext(dispatcher) {
        entryDao.getAll().map { it.toDomain() }
    }

    /**
     * Создаёт новую запись вместе с её тегами.
     *
     * Запись и связи с тегами пишутся в одной транзакции: иначе при сбое
     * посередине в базе осталась бы запись без тегов.
     *
     * @return идентификатор созданной записи или ошибка, если сохранить не удалось
     */
    suspend fun createEntry(entry: DiaryEntry): Result<Long> = runCatching {
        withContext(dispatcher) {
            database.withTransaction {
                val now = System.currentTimeMillis()
                val entryId = entryDao.insert(entry.toEntity(createdAt = now, updatedAt = now))
                linkTags(entryId, entry.tags)
                entryId
            }
        }
    }

    /**
     * Обновляет существующую запись и её набор тегов.
     *
     * Старые связи с тегами удаляются целиком и создаются заново: это проще
     * и надёжнее, чем вычислять разницу двух наборов, а строк там единицы.
     */
    suspend fun updateEntry(entry: DiaryEntry): Result<Unit> = runCatching {
        withContext(dispatcher) {
            database.withTransaction {
                entryDao.update(
                    entry.toEntity(
                        createdAt = entry.createdAt,
                        updatedAt = System.currentTimeMillis()
                    )
                )
                tagDao.clearTagsForEntry(entry.id)
                linkTags(entry.id, entry.tags)
                tagDao.deleteUnusedTags()
            }
        }
    }

    /**
     * Удаляет запись.
     *
     * Связи с тегами уходят сами — за это отвечает `ON DELETE CASCADE`
     * в промежуточной таблице.
     *
     * Перед удалением запись запоминается в [lastDeletedEntry], чтобы
     * пользователь мог отменить действие нажатием «Отменить» в сообщении.
     */
    suspend fun deleteEntry(entryId: Long): Result<Unit> = runCatching {
        withContext(dispatcher) {
            val entry = entryDao.getById(entryId)?.toDomain()
            database.withTransaction {
                entryDao.deleteById(entryId)
                tagDao.deleteUnusedTags()
            }
            lastDeletedEntry = entry
        }
    }

    /**
     * Восстанавливает последнюю удалённую запись.
     *
     * Запись создаётся заново со всеми полями и тегами, но получает новый
     * идентификатор: строка в таблице была удалена, а не скрыта. Для
     * пользователя разницы нет — на экране та же запись.
     *
     * @return `true`, если было что восстанавливать
     */
    suspend fun restoreLastDeletedEntry(): Result<Boolean> = runCatching {
        val entry = lastDeletedEntry ?: return@runCatching false
        createEntry(entry.copy(id = 0L)).getOrThrow()
        lastDeletedEntry = null
        true
    }

    /** Есть ли запись, удаление которой ещё можно отменить. */
    fun hasRestorableEntry(): Boolean = lastDeletedEntry != null

    /** Помечает запись избранной или снимает пометку. */
    suspend fun setFavorite(entryId: Long, isFavorite: Boolean): Result<Unit> = runCatching {
        withContext(dispatcher) {
            entryDao.setFavorite(entryId, isFavorite, System.currentTimeMillis())
        }
    }

    // ---------- Категории ----------

    fun observeCategories(): Flow<List<Category>> =
        categoryDao.observeAll().map { rows -> rows.map { it.toDomain() } }

    /** Количество записей в каждой категории — данные круговой диаграммы. */
    fun observeCategoryUsage(): Flow<List<CategoryUsage>> = categoryDao.observeCategoryUsage()

    suspend fun getCategories(): List<Category> = withContext(dispatcher) {
        categoryDao.getAll().map { it.toDomain() }
    }

    suspend fun createCategory(category: Category): Result<Long> = runCatching {
        withContext(dispatcher) {
            categoryDao.insert(CategoryEntity.fromDomain(category))
        }
    }

    // ---------- Теги ----------

    fun observeTags(): Flow<List<Tag>> =
        tagDao.observeAll().map { rows -> rows.map { it.toDomain() } }

    suspend fun getTags(): List<Tag> = withContext(dispatcher) {
        tagDao.getAll().map { it.toDomain() }
    }

    // ---------- Вспомогательное ----------

    /**
     * Привязывает набор тегов к записи, создавая недостающие теги.
     *
     * Пользователь вводит теги текстом, поэтому тег может как уже
     * существовать, так и появиться впервые. Существующий ищем по имени,
     * новый создаём — так в базе не заводятся два тега «спорт».
     *
     * Метод вызывается только внутри транзакции.
     */
    private suspend fun linkTags(entryId: Long, tags: List<Tag>) {
        if (tags.isEmpty()) return

        val crossRefs = tags.mapNotNull { tag ->
            val name = tag.name.trim()
            if (name.isEmpty()) return@mapNotNull null

            val existing = tagDao.findByName(name)
            val tagId = existing?.id ?: tagDao.insert(TagEntity(name = name, color = tag.color))
            // insert с OnConflictStrategy.IGNORE возвращает -1, если тег успели
            // создать параллельно, — тогда перечитываем его идентификатор.
            val resolvedId = if (tagId == -1L) tagDao.findByName(name)?.id else tagId
            resolvedId?.let { EntryTagCrossRef(entryId = entryId, tagId = it) }
        }

        tagDao.insertCrossRefs(crossRefs)
    }

    /**
     * Дата → число миллисекунд, ровно так же, как это делает
     * [com.nastya.diary.data.database.Converters] при сохранении записи.
     */
    private fun LocalDate.toEpochMillis(): Long =
        atStartOfDay(ZoneOffset.UTC).toInstant().toEpochMilli()

    /** Переводит доменную запись в строку таблицы `entries`. */
    private fun DiaryEntry.toEntity(createdAt: Long, updatedAt: Long) = EntryEntity(
        id = id,
        title = title.trim(),
        content = content.trim(),
        entryDate = date,
        mood = mood,
        categoryId = category.id,
        isFavorite = isFavorite,
        createdAt = createdAt,
        updatedAt = updatedAt
    )
}
