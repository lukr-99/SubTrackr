package com.lukr99.subtrackr.ui.dashboard

import com.lukr99.subtrackr.domain.spend.SubscriptionSpend
import com.lukr99.subtrackr.domain.worth.WorthVerdict
import com.lukr99.subtrackr.model.Subscription
import org.junit.Assert.assertEquals
import org.junit.Assert.assertSame
import org.junit.Test
import java.math.BigDecimal

class SubscriptionFilterTest {
    private fun spend(name: String, category: String, monthly: Int, renewal: String = "") = SubscriptionSpend(
        subscription = Subscription(name = name, category = category, nextRenewal = renewal),
        monthlyOwn = BigDecimal(monthly),
        monthlyBase = BigDecimal(monthly),
        yearlyBase = BigDecimal(monthly * 12),
        costPerUse = BigDecimal.ZERO,
        verdict = WorthVerdict.UNKNOWN,
    )

    private val rows = listOf(
        spend("music", "Entertainment", 150, "2026-10-05"),
        spend("Cloud storage", "", 60, ""),
        spend("Video", "Entertainment", 300, "2026-09-30"),
        spend("Notes app", "Productivity", 90, "2026-10-01"),
    )

    private fun names(filter: SubscriptionFilter) = filter.apply(rows).map { it.subscription.name }

    @Test
    fun categories_areAllThenSortedDistinctWithBlankAsUncategorized() {
        assertEquals(
            listOf("All categories", "Entertainment", "Productivity", "Uncategorized"),
            SubscriptionFilter.categoriesOf(rows),
        )
    }

    @Test
    fun defaultFilter_showsEverythingByMonthlySpendDescending() {
        assertEquals(listOf("Video", "music", "Notes app", "Cloud storage"), names(SubscriptionFilter()))
    }

    @Test
    fun sortByName_ignoresCase() {
        assertEquals(
            listOf("Cloud storage", "music", "Notes app", "Video"),
            names(SubscriptionFilter(sort = SubscriptionFilter.SORT_NAME)),
        )
    }

    @Test
    fun sortByRenewal_putsMissingDatesLast() {
        assertEquals(
            listOf("Video", "Notes app", "music", "Cloud storage"),
            names(SubscriptionFilter(sort = SubscriptionFilter.SORT_RENEWAL)),
        )
    }

    @Test
    fun category_matchesUncategorizedForBlankCategories() {
        assertEquals(listOf("Cloud storage"), names(SubscriptionFilter(category = "Uncategorized")))
        assertEquals(listOf("Video", "music"), names(SubscriptionFilter(category = "Entertainment")))
    }

    @Test
    fun search_matchesNameOrCategoryIgnoringCase() {
        assertEquals(listOf("Video", "music"), names(SubscriptionFilter(search = "ENTER")))
        assertEquals(listOf("Notes app"), names(SubscriptionFilter(search = "notes")))
    }

    @Test
    fun within_fallsBackToAllCategoriesWhenTheChosenOneIsGone() {
        val kept = SubscriptionFilter(category = "Productivity")
        assertSame(kept, kept.within(SubscriptionFilter.categoriesOf(rows)))
        assertEquals(
            SubscriptionFilter.ALL_CATEGORIES,
            SubscriptionFilter(category = "Games").within(SubscriptionFilter.categoriesOf(rows)).category,
        )
    }
}
