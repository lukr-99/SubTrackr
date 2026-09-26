package com.lukr99.subtrackr.domain.sync

/** An email address that is at least shaped like one; the Auth server decides the rest. */
@JvmInline
value class EmailAddress private constructor(val value: String) {
    companion object {
        private val SHAPE = Regex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$")

        /** Trims surrounding spaces; returns null when the text is not an address. */
        fun parse(text: String): EmailAddress? = text.trim().takeIf { SHAPE.matches(it) }?.let(::EmailAddress)
    }
}
