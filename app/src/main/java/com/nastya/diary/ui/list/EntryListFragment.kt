package com.nastya.diary.ui.list

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.core.view.isVisible
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import androidx.navigation.fragment.findNavController
import androidx.recyclerview.widget.LinearLayoutManager
import com.nastya.diary.R
import com.nastya.diary.data.model.DiaryEntry
import com.nastya.diary.databinding.FragmentEntryListBinding
import com.nastya.diary.ui.adapters.EntryAdapter
import com.nastya.diary.utils.ViewModelFactory
import com.nastya.diary.utils.diaryApp
import com.nastya.diary.utils.showMessage
import com.nastya.diary.viewmodels.EntryListUiState
import com.nastya.diary.viewmodels.EntryListViewModel
import kotlinx.coroutines.launch

/**
 * Экран 1 — главный экран приложения со списком записей дневника.
 *
 * Показывает записи, отсортированные от новых к старым, количество записей
 * под заголовком и заглушку, если дневник пока пуст. Кнопка «+» открывает
 * экран создания записи, нажатие на карточку — детальный просмотр.
 */
class EntryListFragment : Fragment() {

    private var _binding: FragmentEntryListBinding? = null
    private val binding get() = requireNotNull(_binding)

    private val viewModel: EntryListViewModel by viewModels {
        ViewModelFactory { EntryListViewModel(diaryApp.diaryRepository) }
    }

    private lateinit var adapter: EntryAdapter

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentEntryListBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        setupRecyclerView()
        setupListeners()
        observeUiState()
    }

    private fun setupRecyclerView() {
        adapter = EntryAdapter(
            onEntryClick = ::openEntry,
            onFavoriteClick = viewModel::toggleFavorite
        )
        binding.recyclerEntries.apply {
            layoutManager = LinearLayoutManager(requireContext())
            adapter = this@EntryListFragment.adapter
            // Размер карточек не зависит от содержимого списка, поэтому
            // RecyclerView может не пересчитывать разметку при каждой вставке.
            setHasFixedSize(true)
        }
    }

    private fun setupListeners() {
        binding.fabAddEntry.setOnClickListener { openEditor() }
        binding.emptyStateContainer.btnEmptyAction.setOnClickListener { openEditor() }
    }

    /**
     * Подписка на состояние экрана.
     *
     * `repeatOnLifecycle(STARTED)` останавливает получение обновлений, когда
     * экран уходит в фон: невидимый список незачем перерисовывать.
     */
    private fun observeUiState() {
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                viewModel.uiState.collect(::render)
            }
        }
    }

    /** Приводит экран в соответствие с текущим состоянием. */
    private fun render(state: EntryListUiState) {
        binding.progressIndicator.isVisible = state.isLoading
        binding.recyclerEntries.isVisible = !state.isLoading && state.entries.isNotEmpty()
        binding.emptyStateContainer.root.isVisible = state.isEmpty

        binding.tvEntryCount.text = resources.getQuantityString(
            R.plurals.entries_count,
            state.totalCount,
            state.totalCount
        )

        if (state.isEmpty) {
            showEmptyState()
        }

        adapter.submitList(state.entries)

        state.errorMessage?.let { message ->
            showMessage(message)
            viewModel.onErrorShown()
        }
    }

    /** Заполняет заглушку, которая видна, пока в дневнике нет ни одной записи. */
    private fun showEmptyState() = with(binding.emptyStateContainer) {
        tvEmptyTitle.text = getString(R.string.empty_entries_title)
        tvEmptyMessage.text = getString(R.string.empty_entries_message)
        btnEmptyAction.text = getString(R.string.empty_entries_action)
        btnEmptyAction.isVisible = true
    }

    /** Открывает детальный просмотр записи. */
    private fun openEntry(entry: DiaryEntry) {
        findNavController().navigate(
            EntryListFragmentDirections.actionListToDetail(entry.id)
        )
    }

    /** Открывает экран создания новой записи. */
    private fun openEditor() {
        findNavController().navigate(
            EntryListFragmentDirections.actionListToEditor()
        )
    }

    override fun onDestroyView() {
        super.onDestroyView()
        // Адаптер держит ссылку на View, поэтому его нужно отвязать вместе
        // с разметкой, иначе фрагмент удержит уничтоженный экран в памяти.
        binding.recyclerEntries.adapter = null
        _binding = null
    }
}
