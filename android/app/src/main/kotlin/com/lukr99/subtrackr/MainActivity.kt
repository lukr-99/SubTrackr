package com.lukr99.subtrackr

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.Box
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
import androidx.lifecycle.viewmodel.compose.viewModel
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.ui.DashboardScreen
import com.lukr99.subtrackr.ui.EditSubscriptionScreen
import com.lukr99.subtrackr.ui.Palette
import com.lukr99.subtrackr.ui.SettingsScreen
import com.lukr99.subtrackr.ui.SubTrackrTheme
import com.lukr99.subtrackr.ui.SubTrackrViewModel
import com.lukr99.subtrackr.ui.WhatIfScreen

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent { SubTrackrTheme { AppRoot() } }
    }
}

private enum class Tab { DASHBOARD, WHATIF, SETTINGS }

@Composable
private fun AppRoot(vm: SubTrackrViewModel = viewModel()) {
    var tab by remember { mutableStateOf(Tab.DASHBOARD) }
    var editorOpen by remember { mutableStateOf(false) }
    var editorSub by remember { mutableStateOf<Subscription?>(null) }

    if (editorOpen) {
        EditSubscriptionScreen(
            initial = editorSub,
            onSave = { vm.upsert(it); editorOpen = false },
            onCancel = { editorOpen = false },
            onDelete = { vm.delete(it); editorOpen = false },
        )
        return
    }

    Scaffold(
        containerColor = Palette.Bg,
        bottomBar = {
            NavigationBar(containerColor = Palette.Surface) {
                NavItem(tab == Tab.DASHBOARD, "Home", { tab = Tab.DASHBOARD }) { Icon(Icons.Filled.Dashboard, null) }
                NavItem(tab == Tab.WHATIF, "What-if", { tab = Tab.WHATIF }) { Icon(Icons.Filled.Calculate, null) }
                NavItem(tab == Tab.SETTINGS, "Settings", { tab = Tab.SETTINGS }) { Icon(Icons.Filled.Settings, null) }
            }
        },
        floatingActionButton = {
            if (tab == Tab.DASHBOARD) {
                FloatingActionButton(
                    onClick = { editorSub = null; editorOpen = true },
                    containerColor = Palette.Accent,
                ) { Icon(Icons.Filled.Add, "Add", tint = Color.White) }
            }
        },
    ) { padding ->
        Box(Modifier.padding(padding)) {
            when (tab) {
                Tab.DASHBOARD -> DashboardScreen(
                    summary = vm.summary,
                    baseCurrency = vm.baseCurrency,
                    rates = vm.rates,
                    onEdit = { editorSub = it; editorOpen = true },
                )
                Tab.WHATIF -> WhatIfScreen(
                    baseCurrency = vm.baseCurrency,
                    currentMonthly = vm.summary.monthlyBase,
                    rates = vm.rates,
                    worthThreshold = vm.worthThreshold,
                )
                Tab.SETTINGS -> SettingsScreen(
                    baseCurrency = vm.baseCurrency,
                    worthThreshold = vm.worthThreshold,
                    ratesLabel = vm.ratesLabel,
                    syncUrl = vm.syncUrl,
                    syncKey = vm.syncKey,
                    onSetBaseCurrency = vm::changeBaseCurrency,
                    onSetThreshold = vm::setWorthThreshold,
                    onRefreshRates = { vm.refreshRates() },
                    onSync = { u, k, cb -> vm.saveSyncConfig(u, k); vm.syncNow(cb) },
                )
            }
        }
    }
}

@Composable
private fun androidx.compose.foundation.layout.RowScope.NavItem(
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
