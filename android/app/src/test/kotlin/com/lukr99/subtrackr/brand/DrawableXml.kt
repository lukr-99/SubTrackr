package com.lukr99.subtrackr.brand

import org.w3c.dom.Element
import java.io.File
import javax.xml.parsers.DocumentBuilderFactory

/** A resource XML file under src/main/res (Gradle runs unit tests from the module directory). */
class DrawableXml(path: String) {
    val root: Element = DocumentBuilderFactory.newInstance()
        .apply { isNamespaceAware = true }
        .newDocumentBuilder()
        .parse(File("src/main/res/$path"))
        .documentElement

    companion object {
        const val ANDROID = "http://schemas.android.com/apk/res/android"

        fun Element.android(name: String): String = getAttributeNS(ANDROID, name)

        fun Element.androidDouble(name: String): Double = android(name).toDouble()

        /** Every descendant element with the given tag, in document order. */
        fun Element.all(tag: String): List<Element> {
            val nodes = getElementsByTagName(tag)
            return (0 until nodes.length).map { nodes.item(it) as Element }
        }

        fun Element.single(tag: String): Element = all(tag).single()
    }
}
