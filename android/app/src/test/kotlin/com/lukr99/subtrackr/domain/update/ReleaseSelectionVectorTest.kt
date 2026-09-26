package com.lukr99.subtrackr.domain.update

import com.lukr99.subtrackr.readVector
import com.lukr99.subtrackr.vectorJson
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class ReleaseSelectionVectorTest {

    @Serializable
    data class Asset(val name: String, @SerialName("browser_download_url") val url: String)

    @Serializable
    data class Release(@SerialName("tag_name") val tagName: String, val assets: List<Asset>)

    @Serializable
    data class Expected(
        val offer: Boolean,
        val version: String? = null,
        val asset: String? = null,
        val checksumAsset: String? = null,
    )

    @Serializable
    data class Case(
        val id: String,
        val platform: String,
        val current: String,
        val release: Release,
        val expected: Expected,
    )

    @Serializable
    data class ChecksumCase(val id: String, val text: String, val expected: String?)

    @Serializable
    data class File(val cases: List<Case>, val checksumFiles: List<ChecksumCase>)

    private val doc = vectorJson.decodeFromString<File>(readVector("release-selection.json"))

    @Test
    fun select_matchesEveryVectorCase() {
        for (c in doc.cases) {
            val release = ReleaseInfo(c.release.tagName, c.release.assets.map { ReleaseAsset(it.name, it.url) })
            val platform = UpdatePlatform.valueOf(c.platform.uppercase())

            val offer = ReleaseSelector.select(release, c.current, platform)

            if (!c.expected.offer) {
                assertNull("case ${c.id}", offer)
                continue
            }
            requireNotNull(offer) { "case ${c.id}: expected an offer" }
            assertEquals("case ${c.id} version", c.expected.version, offer.version.core)
            assertEquals("case ${c.id} asset", c.expected.asset, offer.asset.name)
            assertEquals("case ${c.id} checksum", c.expected.checksumAsset, offer.checksumAsset.name)
        }
    }

    @Test
    fun checksumFile_parse_matchesEveryVectorCase() {
        for (c in doc.checksumFiles) {
            assertEquals("checksum ${c.id}", c.expected, ChecksumFile.parse(c.text))
        }
    }
}
