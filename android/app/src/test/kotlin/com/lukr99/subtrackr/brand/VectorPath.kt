package com.lukr99.subtrackr.brand

/**
 * The absolute path commands the logo drawables use (M, L, H, V, A, Z), reduced to the path's
 * bounds and its arc radii. Anything else fails, so a drawable cannot drift into a shape the
 * geometry checks do not understand.
 */
class VectorPath private constructor(val bounds: LogoSpec.Rect, val arcRadii: List<Double>) {
    companion object {
        private val token = Regex("""[MLHVAZ]|-?\d*\.?\d+(?:[eE][-+]?\d+)?""")

        fun parse(data: String): VectorPath {
            val tokens = token.findAll(data).map { it.value }.toList()
            require(tokens.joinToString("").length == data.replace(Regex("[\\s,]"), "").length) {
                "Unsupported path data: $data"
            }
            val xs = mutableListOf<Double>()
            val ys = mutableListOf<Double>()
            val radii = mutableListOf<Double>()
            var i = 0
            var x = 0.0
            var y = 0.0
            fun next(): Double = tokens[i++].toDouble()
            while (i < tokens.size) {
                when (val command = tokens[i++]) {
                    "M", "L" -> { x = next(); y = next() }
                    "H" -> x = next()
                    "V" -> y = next()
                    "A" -> {
                        radii += next(); radii += next()
                        repeat(3) { next() } // rotation, large-arc flag, sweep flag
                        x = next(); y = next()
                    }
                    "Z" -> continue
                    else -> error("Unsupported command $command in $data")
                }
                xs += x
                ys += y
            }
            return VectorPath(LogoSpec.Rect(xs.min(), ys.min(), xs.max(), ys.max()), radii)
        }
    }
}
