package com.nastya.diary.viewmodels

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.nastya.diary.data.model.DiaryEntry
import com.nastya.diary.data.repository.DiaryRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.catch
import kotlinx.coroutines.launch

/**
 * Состояние экрана списка записей.
 *
 * Экран описан одним неизменяемым объектом: так исключается ситуация, когда
 * на нём одновременно видны индикатор загрузки, заглушка «записей нет»
 * и сам список.
 *
 * @property entries записи, которые нужно показать
 * @property isLoading идёт ли первая загрузка данных
 * @property errorMessage текст ошибки, если прочитать данные не удалось
 */
data class EntryListUiState(
    val entries: List<DiaryEntry> = emptyList(),
    val isLoading: Boolean = true,
    val errorMessage: String? = null
) {

    /** Показывать ли заглушку «Записей пока нет». */
    val isEmpty: Boolean get() = !isLoading && errorMessage == null && entries.isEmpty()

    /** Общее количество записей — выводится под заголовком экрана. */
    val totalCount: Int get() = entries.size
}

/**
 * ViewModel экрана списка записей.
 *
 * Подписывается на поток записей из репозитория: когда пользователь создаёт,
 * меняет или удаляет запись на другом экране, список обновляется сам —
 * перечитывать данные вручную не нужно.
 */
class EntryListViewModel(
    private val repository: DiaryRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(EntryListUiState())

    /** Состояние экрана, на которое подписан фрагмент. */
    val uiState: StateFlow<EntryListUiState> = _uiState.asStateFlow()

    init {
        observeEntries()
    }

    /**
     * Подписка на записи дневника.
     *
     * Оператор `catch` перехватывает ошибку чтения и превращает её в текст
     * на экране: приложение сообщит о проблеме, но не упадёт.
     */
    private fun observeEntries() {
        viewModelScope.launch {
            repository.observeEntries()
                .catch { error ->
                    _uiState.value = EntryListUiState(
                        isLoading = false,
                        errorMessage = error.message ?: UNKNOWN_ERROR
                    )
                }
                .collect { entries ->
                    _uiState.value = EntryListUiState(
                        entries = entries,
                        isLoading = false,
                        errorMessage = null
                    )
                }
        }
    }

    /** Помечает запись избранной или снимает пометку. */
    fun toggleFavorite(entry: DiaryEntry) {
        viewModelScope.launch {
            repository.setFavorite(entry.id, !entry.isFavorite)
                .onFailure { error -> showError(error.message ?: UNKNOWN_ERROR) }
        }
    }

    /** Убирает сообщение об ошибке после того, как пользователь его увидел. */
    fun onErrorShown() {
        _uiState.value = _uiState.value.copy(errorMessage = null)
    }

    private fun showError(message: String) {
        _uiState.value = _uiState.value.copy(errorMessage = message)
    }

    private companion object {
        const val UNKNOWN_ERROR = "Не удалось загрузить записи"
    }
}
