package com.lukr99.subtrackr.brand

import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.int
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive

/**
 * contracts/design/logo.json and the brand colors of tokens.json (both on the classpath through
 * the main Java resources), with the logo's geometry mapped onto a vector drawable's viewport.
 */
class LogoSpec private constructor(logo: JsonObject, brand: JsonObject) {
    val canvas: Int = logo.int("canvas")
    val cornerRadius: Int = logo.int("cornerRadius")
    val barWidth: Int = logo.int("barWidth")
    val barGap: Int = logo.int("barGap")
    val barCornerRadius: Int = logo.int("barCornerRadius")
    val baseline: Int = logo.int("baseline")
    val barHeights: List<Int> = logo.getValue("barHeights").jsonArray.map { it.jsonPrimitive.int }
    val barDurationMs: Int = logo.getValue("animation").jsonObject.int("barDurationMs")
    val staggerMs: Int = logo.getValue("animation").jsonObject.int("staggerMs")
    val easing: String = logo.getValue("animation").jsonObject.getValue("easing").jsonPrimitive.content

    val gradientStart: String = brand.getValue("gradientStart").jsonPrimitive.content.uppercase()
    val gradientEnd: String = brand.getValue("gradientEnd").jsonPrimitive.content.uppercase()
    val mark: String = brand.getValue("mark").jsonPrimitive.content.uppercase()

    /** The whole launch animation: the last bar starts after every stagger and then grows. */
    val totalAnimationMs: Int get() = staggerMs * (barHeights.size - 1) + barDurationMs

    /** The bars on the 256 square, left to right. */
    fun bars(): List<Rect> {
        val total = barHeights.size * barWidth + (barHeights.size - 1) * barGap
        val left = (canvas - total) / 2.0
        return barHeights.mapIndexed { i, height ->
            val x = left + i * (barWidth + barGap)
            Rect(x, (baseline - height).toDouble(), x + barWidth, baseline.toDouble())
        }
    }

    /** Maps the 256 square onto [size] viewport units starting at [offset] on both axes. */
    class Mapping(val offset: Double, val size: Double, canvas: Int) {
        val scale: Double = size / canvas
        fun map(value: Double): Double = offset + value * scale
        fun map(rect: Rect): Rect = Rect(map(rect.left), map(rect.top), map(rect.right), map(rect.bottom))
    }

    data class Rect(val left: Double, val top: Double, val right: Double, val bottom: Double) {
        val centerX: Double get() = (left + right) / 2
        val centerY: Double get() = (top + bottom) / 2
    }

    companion object {
        private fun JsonObject.int(key: String): Int = getValue(key).jsonPrimitive.int

        private fun resource(name: String): JsonObject = Json.parseToJsonElement(
            LogoSpec::class.java.getResourceAsStream("/$name")!!.bufferedReader().use { it.readText() },
        ).jsonObject

        fun load(): LogoSpec = LogoSpec(resource("logo.json"), resource("tokens.json").getValue("brand").jsonObject)
    }
}
