package com.lukr99.subtrackr.ui.dashboard

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.lukr99.subtrackr.ui.components.PickerField
import com.lukr99.subtrackr.ui.components.fieldColors
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme

/** Search field with the category and sort pickers above the subscription list. */
@Composable
internal fun FilterBar(filter: SubscriptionFilter, categories: List<String>, onChange: (SubscriptionFilter) -> Unit) {
    Column {
        OutlinedTextField(
            value = filter.search,
            onValueChange = { onChange(filter.copy(search = it)) },
            placeholder = { Text("Search subscriptions…", color = SubTrackrTheme.colors.textMuted) },
            singleLine = true,
            colors = fieldColors(),
            shape = RoundedCornerShape(10.dp),
            modifier = Modifier.fillMaxWidth(),
        )
        Spacer(Modifier.height(8.dp))
        Row {
            PickerField("Category", filter.category, categories, { onChange(filter.copy(category = it)) }, Modifier.weight(1f))
            Spacer(Modifier.width(10.dp))
            PickerField("Sort", filter.sort, SubscriptionFilter.SORTS, { onChange(filter.copy(sort = it)) }, Modifier.weight(1f))
        }
    }
}
