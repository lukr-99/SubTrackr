package com.lukr99.subtrackr.ui.dashboard

import com.lukr99.subtrackr.domain.spend.SubscriptionSpend

/** Search text, category, and sort order for the dashboard's subscription list. */
internal data class SubscriptionFilter(
    val search: String = "",
    val category: String = ALL_CATEGORIES,
    val sort: String = SORT_MONTHLY,
) {
    /** This filter, or one showing all categories when the chosen category is gone. */
    fun within(categories: List<String>): SubscriptionFilter =
        if (category != ALL_CATEGORIES && category !in categories) copy(category = ALL_CATEGORIES) else this

    /** The rows that match the category and the search text, in the chosen order. */
    fun apply(rows: List<SubscriptionSpend>): List<SubscriptionSpend> {
        val matching = rows
            .filter { category == ALL_CATEGORIES || categoryOf(it) == category }
            .filter {
                search.isBlank() ||
                    it.subscription.name.contains(search, true) ||
                    it.subscription.category.contains(search, true)
            }
        return when (sort) {
            SORT_NAME -> matching.sortedBy { it.subscription.name.lowercase() }
            SORT_RENEWAL -> matching.sortedBy { it.subscription.nextRenewal.ifBlank { "9999" } }
            else -> matching.sortedByDescending { it.monthlyBase }
        }
    }

    companion object {
        const val ALL_CATEGORIES = "All categories"
        const val UNCATEGORIZED = "Uncategorized"
        const val SORT_MONTHLY = "Monthly ↓"
        const val SORT_NAME = "Name A–Z"
        const val SORT_RENEWAL = "Renewal"
        val SORTS = listOf(SORT_MONTHLY, SORT_NAME, SORT_RENEWAL)

        /** "All categories" and then every category in [rows], sorted; a blank one reads "Uncategorized". */
        fun categoriesOf(rows: List<SubscriptionSpend>): List<String> =
            listOf(ALL_CATEGORIES) + rows.map(::categoryOf).distinct().sorted()

        private fun categoryOf(spend: SubscriptionSpend) = spend.subscription.category.ifBlank { UNCATEGORIZED }
    }
}
