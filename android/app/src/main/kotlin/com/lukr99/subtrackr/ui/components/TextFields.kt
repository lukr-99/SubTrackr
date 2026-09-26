package com.lukr99.subtrackr.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowDropDown
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Icon
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextFieldColors
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme

@Composable
fun fieldColors(): TextFieldColors = OutlinedTextFieldDefaults.colors(
    focusedContainerColor = SubTrackrTheme.colors.surfaceAlt,
    unfocusedContainerColor = SubTrackrTheme.colors.surfaceAlt,
    disabledContainerColor = SubTrackrTheme.colors.surfaceAlt,
    focusedBorderColor = SubTrackrTheme.colors.accent,
    unfocusedBorderColor = SubTrackrTheme.colors.border,
    disabledBorderColor = SubTrackrTheme.colors.border,
    focusedTextColor = SubTrackrTheme.colors.textPrimary,
    unfocusedTextColor = SubTrackrTheme.colors.textPrimary,
    disabledTextColor = SubTrackrTheme.colors.textPrimary,
    cursorColor = SubTrackrTheme.colors.textPrimary,
    focusedLabelColor = SubTrackrTheme.colors.textSecondary,
    unfocusedLabelColor = SubTrackrTheme.colors.textSecondary,
)

@Composable
fun LabeledTextField(
    label: String,
    value: String,
    onValueChange: (String) -> Unit,
    modifier: Modifier = Modifier,
    keyboardOptions: KeyboardOptions = KeyboardOptions.Default,
    enabled: Boolean = true,
) {
    OutlinedTextField(
        value = value,
        onValueChange = onValueChange,
        label = { Text(label) },
        singleLine = true,
        enabled = enabled,
        keyboardOptions = keyboardOptions,
        colors = fieldColors(),
        shape = RoundedCornerShape(10.dp),
        modifier = modifier.fillMaxWidth(),
    )
}

/** Read-only field that opens a dropdown of [options] when tapped. */
@Composable
fun PickerField(
    label: String,
    value: String,
    options: List<String>,
    onSelect: (String) -> Unit,
    modifier: Modifier = Modifier,
) {
    var expanded by remember { mutableStateOf(false) }
    Box(modifier) {
        OutlinedTextField(
            value = value,
            onValueChange = {},
            readOnly = true,
            label = { Text(label) },
            trailingIcon = { Icon(Icons.Filled.ArrowDropDown, null, tint = SubTrackrTheme.colors.textSecondary) },
            colors = fieldColors(),
            shape = RoundedCornerShape(10.dp),
            modifier = Modifier.fillMaxWidth(),
        )
        Box(
            Modifier
                .matchParentSize()
                .clickable { expanded = true },
        )
        DropdownMenu(
            expanded = expanded,
            onDismissRequest = { expanded = false },
            modifier = Modifier.background(SubTrackrTheme.colors.surface),
        ) {
            options.forEach { opt ->
                DropdownMenuItem(
                    text = { Text(opt, color = SubTrackrTheme.colors.textPrimary) },
                    onClick = { onSelect(opt); expanded = false },
                )
            }
        }
    }
}
