package com.lukr99.subtrackr.domain.sync

/**
 * The one-time code Supabase Auth emails (SPEC.md section 8.2). Projects send 6 digits by default
 * and can be configured for up to 10.
 */
@JvmInline
value class SignInCode private constructor(val value: String) {
    companion object {
        const val MIN_LENGTH = 6
        const val MAX_LENGTH = 10

        /** Accepts digits with optional spaces ("123 456"); returns null otherwise. */
        fun parse(text: String): SignInCode? =
            text.filterNot(Char::isWhitespace)
                .takeIf { it.length in MIN_LENGTH..MAX_LENGTH && it.all { c -> c in '0'..'9' } }
                ?.let(::SignInCode)
    }
}
