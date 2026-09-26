package com.lukr99.subtrackr.domain.backup

import com.lukr99.subtrackr.model.BillingCycle
import com.lukr99.subtrackr.model.Money
import com.lukr99.subtrackr.model.SubStatus
import com.lukr99.subtrackr.model.ThemeMode
import com.lukr99.subtrackr.model.WorthMode
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.jsonObject
import org.junit.Assert.assertEquals
import org.junit.Assert.assertThrows
import org.junit.Test

class ProtoJsonTest {
    private fun obj(text: String) = Json.parseToJsonElement(text).jsonObject

    @Test
    fun decodeSubscription_missingFields_takeProtoDefaultsNotKotlinDefaults() {
        val sub = ProtoJson.decodeSubscription(obj("""{"id":"a"}"""))

        assertEquals(Money("", 0, 0), sub.cost)
        assertEquals(BillingCycle.BILLING_CYCLE_UNSPECIFIED, sub.billingCycle)
        assertEquals(SubStatus.SUB_STATUS_UNSPECIFIED, sub.status)
        assertEquals(WorthMode.AUTO, sub.worthMode)
        assertEquals(false, sub.autoPay)
        assertEquals("", sub.deletedAt)
    }

    @Test
    fun decodeSettings_missing_isAllDefaults() {
        val settings = ProtoJson.decodeSettings(null)

        assertEquals("", settings.baseCurrency)
        assertEquals("", settings.schemaVersion)
        assertEquals(ThemeMode.SYSTEM, settings.themeMode)
        assertEquals(0.0, settings.monthlyBudget, 0.0)
    }

    @Test
    fun decode_acceptsProtoNamesNumbersAsStringsAndEnumNumbers() {
        val sub = ProtoJson.decodeSubscription(
            obj("""{"id":"a","cost":{"currency":"EUR","minor_units":999,"exponent":"2"},"billing_cycle":5,"status":"PAUSED","usesPerMonth":"8","autoPay":true,"worthMode":"SOMETHING_NEW","notes":null}"""),
        )

        assertEquals(Money("EUR", 999, 2), sub.cost)
        assertEquals(BillingCycle.ANNUAL, sub.billingCycle)
        assertEquals(SubStatus.PAUSED, sub.status)
        assertEquals(8.0, sub.usesPerMonth, 0.0)
        assertEquals(true, sub.autoPay)
        assertEquals(WorthMode.AUTO, sub.worthMode)
        assertEquals("", sub.notes)
    }

    @Test
    fun decode_wrongTypes_throw() {
        assertThrows(ProtoJsonException::class.java) { ProtoJson.decodeSubscription(obj("""{"name":5}""")) }
        assertThrows(ProtoJsonException::class.java) { ProtoJson.decodeSubscription(obj("""{"cost":"EUR"}""")) }
        assertThrows(ProtoJsonException::class.java) { ProtoJson.decodeSubscription(obj("""{"cost":{"minorUnits":"1.5"}}""")) }
        assertThrows(ProtoJsonException::class.java) { ProtoJson.decodeSubscription(obj("""{"autoPay":"yes"}""")) }
        assertThrows(ProtoJsonException::class.java) { ProtoJson.decodeDatabase(obj("""{"subscriptions":{}}""")) }
    }

    @Test
    fun encodeSettings_writesWholeNumbersWithoutFraction() {
        val json = ProtoJson.encodeSettings(com.lukr99.subtrackr.model.Settings(monthlyBudget = 120.0, worthThreshold = 2.5))

        assertEquals("120", json.getValue("monthlyBudget").toString())
        assertEquals("2.5", json.getValue("worthThreshold").toString())
    }
}
