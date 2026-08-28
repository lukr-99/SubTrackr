# kotlinx.serialization: keep generated serializers.
-keepattributes *Annotation*, InnerClasses
-dontnote kotlinx.serialization.**
-keepclassmembers class com.lukr99.subtrackr.** {
    *** Companion;
}
-keep class com.lukr99.subtrackr.**$$serializer { *; }
