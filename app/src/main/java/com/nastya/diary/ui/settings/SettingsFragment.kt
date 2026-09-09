package com.nastya.diary.ui.settings

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.LinearLayout
import android.widget.RadioButton
import androidx.core.content.ContextCompat
import androidx.fragment.app.Fragment
import androidx.fragment.app.viewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.nastya.diary.BuildConfig
import com.nastya.diary.R
import com.nastya.diary.data.model.AppSettings
import com.nastya.diary.data.model.AppTheme
import com.nastya.diary.data.model.DateDisplayStyle
import com.nastya.diary.data.model.SortOrder
import com.nastya.diary.databinding.FragmentSettingsBinding
import com.nastya.diary.databinding.ItemThemeOptionBinding
import com.nastya.diary.utils.ViewModelFactory
import com.nastya.diary.utils.diaryApp
import com.nastya.diary.utils.showMessage
import com.nastya.diary.viewmodels.SettingsUiState
import com.nastya.diary.viewmodels.SettingsViewModel
import kotlinx.coroutines.launch

/**
 * Экран 5 — настройки приложения.
 *
 * Четыре настройки: тема оформления, порядок сортировки по умолчанию,
 * вид даты в списке и показ превью текста. Все они сохраняются в DataStore
 * и применяются сразу, без перезапуска приложения.
 */
class SettingsFragment : Fragment() {

    private var _binding: FragmentSettingsBinding? = null
    private val binding get() = requireNotNull(_binding)

    private val viewModel: SettingsViewModel by viewModels {
        ViewModelFactory { SettingsViewModel(diaryApp.settingsRepository) }
    }

    /** Кнопки выбора темы, чтобы не искать их заново при каждой перерисовке. */
    private val themeOptions = mutableMapOf<AppTheme, ItemThemeOptionBinding>()

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View {
        _binding = FragmentSettingsBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        buildThemeSelector()
        buildSortOptions()
        buildDateStyleOptions()
        setupPreviewSwitch()
        showAppVersion()
        observeUiState()
    }

    /** Создаёт три равные по ширине кнопки выбора темы. */
    private fun buildThemeSelector() {
        val container = binding.themeSelector
        container.removeAllViews()
        themeOptions.clear()

        AppTheme.values().forEach { theme ->
            val option = ItemThemeOptionBinding.inflate(layoutInflater, container, false)
            option.imgThemeIcon.setImageResource(theme.iconRes)
            option.tvThemeLabel.setText(theme.labelRes)
            option.root.setOnClickListener { viewModel.onThemeSelected(theme) }

            // Кнопки делят ширину поровну, между ними небольшой промежуток.
            val params = LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WRAP_CONTENT, 1f)
            val gap = resources.getDimensionPixelSize(R.dimen.spacing_xs)
            params.setMargins(gap, 0, gap, 0)

            container.addView(option.root, params)
            themeOptions[theme] = option
        }
    }

    private fun buildSortOptions() {
        val group = binding.radioGroupDefaultSort
        group.removeAllViews()

        SortOrder.values().forEach { order ->
            val button = RadioButton(requireContext()).apply {
                id = View.generateViewId()
                setText(order.labelRes)
                tag = order
                setOnClickListener { viewModel.onSortOrderSelected(order) }
            }
            group.addView(button)
        }
    }

    private fun buildDateStyleOptions() {
        val group = binding.radioGroupDateStyle
        group.removeAllViews()

        DateDisplayStyle.values().forEach { style ->
            val button = RadioButton(requireContext()).apply {
                id = View.generateViewId()
                setText(style.labelRes)
                tag = style
                setOnClickListener { viewModel.onDateStyleSelected(style) }
            }
            group.addView(button)
        }
    }

    private fun setupPreviewSwitch() {
        binding.switchShowPreview.setOnClickListener {
            viewModel.onShowPreviewChanged(binding.switchShowPreview.isChecked)
        }
    }

    private fun showAppVersion() {
        binding.tvAppVersion.text =
            getString(R.string.settings_version_template, BuildConfig.VERSION_NAME)
    }

    private fun observeUiState() {
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                viewModel.uiState.collect(::render)
            }
        }
    }

    /**
     * Приводит переключатели в соответствие с сохранёнными настройками.
     *
     * Экран рисуется по данным из хранилища, а не по последнему нажатию:
     * если запись не удалась, переключатель сам вернётся в прежнее положение.
     */
    private fun render(state: SettingsUiState) {
        if (state.isLoading) return
        renderSettings(state.settings)

        state.errorMessage?.let { message ->
            showMessage(message)
            viewModel.onErrorShown()
        }
    }

    private fun renderSettings(settings: AppSettings) {
        themeOptions.forEach { (theme, option) ->
            val isSelected = theme == settings.theme
            option.root.isSelected = isSelected
            // Выбранный режим подкрашивается цветом бренда, остальные —
            // приглушённым цветом текста.
            val tint = ContextCompat.getColor(
                requireContext(),
                if (isSelected) R.color.brand_purple else R.color.text_secondary
            )
            option.imgThemeIcon.setColorFilter(tint)
            option.tvThemeLabel.setTextColor(tint)
        }

        checkOption(binding.radioGroupDefaultSort, settings.defaultSortOrder)
        checkOption(binding.radioGroupDateStyle, settings.dateDisplayStyle)

        if (binding.switchShowPreview.isChecked != settings.showPreview) {
            binding.switchShowPreview.isChecked = settings.showPreview
        }
    }

    /** Отмечает в группе переключатель, метка которого совпадает со значением. */
    private fun checkOption(group: android.widget.RadioGroup, value: Any) {
        (0 until group.childCount)
            .mapNotNull { index -> group.getChildAt(index) as? RadioButton }
            .forEach { button -> button.isChecked = button.tag == value }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        themeOptions.clear()
        _binding = null
    }
}
