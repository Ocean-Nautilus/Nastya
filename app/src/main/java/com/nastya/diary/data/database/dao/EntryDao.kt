package com.nastya.diary.data.database.dao

import androidx.room.Dao
import androidx.room.Delete
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import androidx.room.Transaction
import androidx.room.Update
import com.nastya.diary.data.database.entity.EntryEntity
import com.nastya.diary.data.database.relation.EntryWithRelations
import kotlinx.coroutines.flow.Flow

/**
 * Доступ к таблице `entries` — полный CRUD главной сущности приложения.
 *
 * Методы, возвращающие запись вместе с категорией и тегами, помечены
 * аннотацией [Transaction]: Room выполняет для них несколько запросов, и без
 * транзакции между ними могли бы вклиниться чужие изменения — тогда запись
 * приехала бы со старой категорией или с чужими тегами.
 */
@Dao
interface EntryDao {

    // ---------- Чтение (Read) ----------

    /** Все записи, сначала новые. Поток сам обновляется при любых изменениях. */
    @Transaction
    @Query("SELECT * FROM entries ORDER BY entry_date DESC, id DESC")
    fun observeAll(): Flow<List<EntryWithRelations>>

    /** Одна запись со связанными данными; `null`, если запись уже удалена. */
    @Transaction
    @Query("SELECT * FROM entries WHERE id = :entryId")
    fun observeById(entryId: Long): Flow<EntryWithRelations?>

    @Transaction
    @Query("SELECT * FROM entries WHERE id = :entryId")
    suspend fun getById(entryId: Long): EntryWithRelations?

    /** Все записи разом — используется при экспорте дневника в PDF. */
    @Transaction
    @Query("SELECT * FROM entries ORDER BY entry_date DESC, id DESC")
    suspend fun getAll(): List<EntryWithRelations>

    @Query("SELECT COUNT(*) FROM entries")
    fun observeTotalCount(): Flow<Int>

    /**
     * Записи, отобранные по поиску, фильтрам и порядку сортировки.
     *
     * Все условия описаны одним запросом, а не собираются из кусков строк:
     * так параметры подставляются безопасно, а Room проверяет запрос при
     * компиляции. Приём с `:параметр IS NULL OR ...` означает «фильтр не
     * задан — условие не применяется».
     *
     * Сортировка выбирается выражениями `CASE` внутри `ORDER BY`: сработает
     * ровно одна ветка, остальные вернут NULL и на порядок не повлияют.
     * Дополнительное `e.id DESC` в конце нужно, чтобы записи с одинаковой
     * датой не меняли порядок между обновлениями списка.
     *
     * @param query строка поиска; пустая строка отключает поиск
     * @param mood имя настроения или `null`
     * @param categoryId идентификатор категории или `null`
     * @param fromDate начало периода в миллисекундах или `null`
     * @param toDate конец периода в миллисекундах или `null`
     * @param sortOrder номер режима сортировки, см. [com.nastya.diary.data.model.SortOrder]
     */
    @Transaction
    @Query(
        """
        SELECT * FROM entries e
        WHERE (:query = '' OR e.title LIKE '%' || :query || '%'
                          OR e.content LIKE '%' || :query || '%')
          AND (:mood IS NULL OR e.mood = :mood)
          AND (:categoryId IS NULL OR e.category_id = :categoryId)
          AND (:fromDate IS NULL OR e.entry_date >= :fromDate)
          AND (:toDate IS NULL OR e.entry_date <= :toDate)
        ORDER BY
            CASE WHEN :sortOrder = 0 THEN e.entry_date END DESC,
            CASE WHEN :sortOrder = 1 THEN e.entry_date END ASC,
            CASE WHEN :sortOrder = 2 THEN e.title END COLLATE NOCASE ASC,
            e.id DESC
        """
    )
    fun observeFiltered(
        query: String,
        mood: String?,
        categoryId: Long?,
        fromDate: Long?,
        toDate: Long?,
        sortOrder: Int
    ): Flow<List<EntryWithRelations>>

    // ---------- Создание, изменение, удаление (Create / Update / Delete) ----------

    /**
     * Вставляет запись и возвращает присвоенный ей идентификатор.
     *
     * Стратегия [OnConflictStrategy.ABORT] выбрана осознанно: молча заменять
     * чужую запись при конфликте первичного ключа — худшее, что может сделать
     * дневник, поэтому конфликт лучше превратить в ошибку.
     */
    @Insert(onConflict = OnConflictStrategy.ABORT)
    suspend fun insert(entry: EntryEntity): Long

    @Update
    suspend fun update(entry: EntryEntity)

    @Delete
    suspend fun delete(entry: EntryEntity)

    /** Удаление по идентификатору — когда на руках нет всей сущности. */
    @Query("DELETE FROM entries WHERE id = :entryId")
    suspend fun deleteById(entryId: Long)

    /** Переключает признак «избранное» без перезаписи остальных полей. */
    @Query("UPDATE entries SET is_favorite = :isFavorite, updated_at = :updatedAt WHERE id = :entryId")
    suspend fun setFavorite(entryId: Long, isFavorite: Boolean, updatedAt: Long)
}
