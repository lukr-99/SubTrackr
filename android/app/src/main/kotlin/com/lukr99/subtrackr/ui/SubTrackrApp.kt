package com.lukr99.subtrackr.ui

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.fillMaxSize
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
import androidx.compose.ui.ExperimentalComposeUiApi
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.testTagsAsResourceId
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.ui.dashboard.DashboardScreen
import com.lukr99.subtrackr.ui.editor.EditSubscriptionScreen
import com.lukr99.subtrackr.ui.settings.SettingsRoute
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
import com.lukr99.subtrackr.ui.update.UpdatePrompt
import com.lukr99.subtrackr.ui.update.UpdateViewModel
import com.lukr99.subtrackr.ui.whatif.WhatIfScreen

/**
 * App shell: bottom navigation, the editor overlay, and the update prompt. The root exposes test
 * tags as resource ids so uiautomator, Android CLI, and Maestro can select elements by tag.
 */
@OptIn(ExperimentalComposeUiApi::class)
@Composable
fun SubTrackrApp(viewModelFactory: ViewModelProvider.Factory) {
    val vm: SubTrackrViewModel = viewModel(factory = viewModelFactory)
    val updates: UpdateViewModel = viewModel(factory = viewModelFactory)
    val update by updates.uiState.collectAsStateWithLifecycle()
    var tab by remember { mutableStateOf(AppTab.DASHBOARD) }
    var editorOpen by remember { mutableStateOf(false) }
    var editorSub by remember { mutableStateOf<Subscription?>(null) }

    // Back closes the editor first, then returns to Home, before exiting the app.
    BackHandler(enabled = editorOpen) { editorOpen = false }
    BackHandler(enabled = !editorOpen && tab != AppTab.DASHBOARD) { tab = AppTab.DASHBOARD }

    Box(Modifier.fillMaxSize().semantics { testTagsAsResourceId = true }) {
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
        } else {
            Scaffold(
                containerColor = SubTrackrTheme.colors.background,
                bottomBar = {
                    NavigationBar(containerColor = SubTrackrTheme.colors.surface) {
                        NavItem(tab == AppTab.DASHBOARD, "Home", AppTags.NAV_DASHBOARD, { tab = AppTab.DASHBOARD }) {
                            Icon(Icons.Filled.Dashboard, null)
                        }
                        NavItem(tab == AppTab.WHATIF, "What-if", AppTags.NAV_WHATIF, { tab = AppTab.WHATIF }) {
                            Icon(Icons.Filled.Calculate, null)
                        }
                        NavItem(tab == AppTab.SETTINGS, "Settings", AppTags.NAV_SETTINGS, { tab = AppTab.SETTINGS }) {
                            Icon(Icons.Filled.Settings, null)
                        }
                    }
                },
                floatingActionButton = {
                    if (tab == AppTab.DASHBOARD) {
                        FloatingActionButton(
                            onClick = {
                                editorSub = null
                                editorOpen = true
                            },
                            containerColor = SubTrackrTheme.colors.accent,
                            modifier = Modifier.testTag(AppTags.ADD),
                        ) { Icon(Icons.Filled.Add, "Add", tint = SubTrackrTheme.colors.onAccent) }
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
                            today = vm.today,
                            showServiceLogos = vm.showServiceLogos,
                        )
                        AppTab.WHATIF -> WhatIfScreen(
                            baseCurrency = vm.baseCurrency,
                            currentMonthly = vm.summary.monthlyBase,
                            rates = vm.rates,
                            worthThreshold = vm.worthThreshold,
                        )
                        AppTab.SETTINGS -> SettingsRoute(app = vm, viewModelFactory = viewModelFactory)
                    }
                }
            }
        }
    }

    UpdatePrompt(update, onInstall = updates::downloadAndInstall, onLater = updates::dismissPrompt)
}

@Composable
private fun RowScope.NavItem(
    selected: Boolean,
    label: String,
    tag: String,
    onClick: () -> Unit,
    icon: @Composable () -> Unit,
) {
    NavigationBarItem(
        selected = selected,
        onClick = onClick,
        icon = icon,
        label = { Text(label) },
        modifier = Modifier.testTag(tag),
        colors = NavigationBarItemDefaults.colors(
            selectedIconColor = SubTrackrTheme.colors.accent,
            unselectedIconColor = SubTrackrTheme.colors.textSecondary,
            selectedTextColor = SubTrackrTheme.colors.accent,
            unselectedTextColor = SubTrackrTheme.colors.textSecondary,
            indicatorColor = SubTrackrTheme.colors.surfaceAlt,
        ),
    )
}
