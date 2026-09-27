package com.lukr99.subtrackr.ui.dashboard

import android.app.Application
import androidx.compose.ui.test.assertIsDisplayed
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onNodeWithTag
import coil.Coil
import coil.ImageLoader
import coil.intercept.Interceptor
import coil.request.ErrorResult
import coil.request.ImageResult
import com.lukr99.subtrackr.model.Subscription
import com.lukr99.subtrackr.ui.theme.SubTrackrTheme
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Before
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.RuntimeEnvironment
import org.robolectric.annotation.Config
import java.util.Collections

/** With logos off Coil is never asked for anything; with them on it is asked for the favicon. */
@RunWith(RobolectricTestRunner::class)
@Config(sdk = [35], application = Application::class)
class ServiceIconTest {
    @get:Rule
    val compose = createComposeRule()

    private val requested: MutableList<Any> = Collections.synchronizedList(mutableListOf())
    private val subscription = Subscription(name = "Video", iconRef = "🎬", website = "example.com")

    @Before
    fun recordEveryImageRequest() {
        // Every request fails after it is recorded, so nothing touches the network.
        val recorder = object : Interceptor {
            override suspend fun intercept(chain: Interceptor.Chain): ImageResult {
                requested += chain.request.data
                return ErrorResult(null, chain.request, IllegalStateException("offline test"))
            }
        }
        Coil.setImageLoader(
            ImageLoader.Builder(RuntimeEnvironment.getApplication())
                .components { add(recorder) }
                .build(),
        )
    }

    private fun show(showLogos: Boolean) = compose.setContent {
        SubTrackrTheme(darkTheme = false) { ServiceIcon(subscription, showLogos) }
    }

    @Test
    fun logosHidden_neverAsksCoil_andShowsTheEmoji() {
        show(showLogos = false)
        compose.waitForIdle()

        compose.onNodeWithTag(DashboardTags.SERVICE_EMOJI).assertIsDisplayed()
        compose.onNodeWithTag(DashboardTags.SERVICE_LOGO).assertDoesNotExist()
        assertEquals(emptyList<Any>(), requested.toList())
    }

    @Test
    fun logosShown_asksForTheFavicon_andKeepsTheEmojiWhenItFails() {
        show(showLogos = true)
        compose.waitUntil(timeoutMillis = 5_000) { requested.isNotEmpty() }
        compose.waitForIdle()

        assertEquals(listOf<Any>("https://www.google.com/s2/favicons?domain=example.com&sz=64"), requested.toList())
        compose.onNodeWithTag(DashboardTags.SERVICE_EMOJI).assertIsDisplayed()
    }

    @Test
    fun faviconUrl_isNullWithoutAWebsite() {
        assertNull(faviconUrl(""))
        assertNull(faviconUrl("   "))
        assertEquals("https://www.google.com/s2/favicons?domain=example.com&sz=64", faviconUrl(" example.com "))
    }
}
