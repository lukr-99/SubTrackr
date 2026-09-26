package com.lukr99.subtrackr.ui

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Calculate
import androidx.compose.material.icons.filled.Dashboard
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationBarItemDefaults
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalUriHandler
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.lukr99.subtrackr.domain.update.UpdateChannel
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.ui.dashboard.DashboardScreen
import com.lukr99.subtrackr.ui.editor.EditSubscriptionScreen
import com.lukr99.subtrackr.ui.settings.SettingsScreen
import com.lukr99.subtrackr.ui.theme.Palette
import com.lukr99.subtrackr.ui.update.UpdatePrompt
import com.lukr99.subtrackr.ui.update.UpdateViewModel
import com.lukr99.subtrackr.ui.whatif.WhatIfScreen

/** App shell: bottom navigation, the editor overlay, and the update prompt. */
@Composable
fun SubTrackrApp(viewModelFactory: ViewModelProvider.Factory) {
    val vm: SubTrackrViewModel = viewModel(factory = viewModelFactory)
    val updates: UpdateViewModel = viewModel(factory = viewModelFactory)
    val update by updates.uiState.collectAsStateWithLifecycle()
    val uriHandler = LocalUriHandler.current
    var tab by remember { mutableStateOf(AppTab.DASHBOARD) }
    var editorOpen by remember { mutableStateOf(false) }
    var editorSub by remember { mutableStateOf<Subscription?>(null) }

    // Back closes the editor first, then returns to Home, before exiting the app.
    BackHandler(enabled = editorOpen) { editorOpen = false }
    BackHandler(enabled = !editorOpen && tab != AppTab.DASHBOARD) { tab = AppTab.DASHBOARD }

    if (editorOpen) {
        EditSubscriptionScreen(
            initial = editorSub,
            onSave = {
                vm.upsert(it)
                editorOpen = false
            },
            onCancel = { editorOpen = false },
            onDelete = {
                vm.delete(it)
                editorOpen = false
            },
        )
        return
    }

    Scaffold(
        containerColor = Palette.Bg,
        bottomBar = {
            NavigationBar(containerColor = Palette.Surface) {
                NavItem(tab == AppTab.DASHBOARD, "Home", { tab = AppTab.DASHBOARD }) { Icon(Icons.Filled.Dashboard, null) }
                NavItem(tab == AppTab.WHATIF, "What-if", { tab = AppTab.WHATIF }) { Icon(Icons.Filled.Calculate, null) }
                NavItem(tab == AppTab.SETTINGS, "Settings", { tab = AppTab.SETTINGS }) { Icon(Icons.Filled.Settings, null) }
            }
        },
        floatingActionButton = {
            if (tab == AppTab.DASHBOARD) {
                FloatingActionButton(
                    onClick = {
                        editorSub = null
                        editorOpen = true
                    },
                    containerColor = Palette.Accent,
                ) { Icon(Icons.Filled.Add, "Add", tint = Color.White) }
            }
        },
    ) { padding ->
        Box(Modifier.padding(padding)) {
            when (tab) {
                AppTab.DASHBOARD -> DashboardScreen(
                    summary = vm.summary,
                    baseCurrency = vm.baseCurrency,
                    rates = vm.rates,
                    budget = vm.monthlyBudget,
                    onEdit = {
                        editorSub = it
                        editorOpen = true
                    },
                )
                AppTab.WHATIF -> WhatIfScreen(
                    baseCurrency = vm.baseCurrency,
                    currentMonthly = vm.summary.monthlyBase,
                    rates = vm.rates,
                    worthThreshold = vm.worthThreshold,
                )
                AppTab.SETTINGS -> SettingsScreen(
                    baseCurrency = vm.baseCurrency,
                    worthThreshold = vm.worthThreshold,
                    monthlyBudget = vm.monthlyBudget,
                    ratesLabel = vm.ratesLabel,
                    syncUrl = vm.syncUrl,
                    syncKey = vm.syncKey,
                    update = update,
                    onSetBaseCurrency = vm::changeBaseCurrency,
                    onSetThreshold = vm::setWorthThreshold,
                    onSetBudget = vm::setMonthlyBudget,
                    onRefreshRates = { vm.refreshRates() },
                    onSync = { u, k, cb ->
                        vm.saveSyncConfig(u, k)
                        vm.syncNow(cb)
                    },
                    onCheckUpdate = updates::check,
                    onInstallUpdate = updates::downloadAndInstall,
                    onOpenReleases = { uriHandler.openUri(UpdateChannel.RELEASES_PAGE) },
                )
            }
        }
    }

    UpdatePrompt(update, onInstall = updates::downloadAndInstall, onLater = updates::dismissPrompt)
}

@Composable
private fun RowScope.NavItem(
    selected: Boolean,
    label: String,
    onClick: () -> Unit,
    icon: @Composable () -> Unit,
) {
    NavigationBarItem(
        selected = selected,
        onClick = onClick,
        icon = icon,
        label = { Text(label) },
        colors = NavigationBarItemDefaults.colors(
            selectedIconColor = Palette.Accent,
            unselectedIconColor = Palette.TextSecondary,
            selectedTextColor = Palette.Accent,
            unselectedTextColor = Palette.TextSecondary,
            indicatorColor = Palette.SurfaceAlt,
        ),
    )
}
