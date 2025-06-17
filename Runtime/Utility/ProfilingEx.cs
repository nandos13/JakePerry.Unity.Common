using Unity.Profiling;

namespace JakePerry.Unity
{
    public static class ProfilingEx
    {
        public static class Categories
        {
            private static readonly ProfilerCategory _editorOnlyCategory = new("Editor Only");
            private static readonly ProfilerCategory _debugOnlyCategory = new("Debug Only");

            /// <summary>
            /// <see cref="ProfilerCategory"/> for regions of code that only run in the editor.
            /// </summary>
            public static ProfilerCategory EditorOnly => _editorOnlyCategory;

            /// <summary>
            /// <see cref="ProfilerCategory"/> for regions of code that only run in Debug/Developer mode.
            /// </summary>
            public static ProfilerCategory DebugOnly => _debugOnlyCategory;
        }

        public static class Markers
        {
            private static readonly ProfilerMarker _editorOnlyMarker = new(Categories.EditorOnly, "[Editor Only]");
            private static readonly ProfilerMarker _debugOnlyMarker = new(Categories.DebugOnly, "[Debug Only]");

            /// <summary>
            /// <see cref="ProfilerMarker"/> for regions of code that only run in the editor.
            /// </summary>
            public static ProfilerMarker EditorOnly => _editorOnlyMarker;

            /// <summary>
            /// <see cref="ProfilerMarker"/> for regions of code that only run in Debug/Developer mode.
            /// </summary>
            public static ProfilerMarker DebugOnly => _debugOnlyMarker;
        }
    }
}
