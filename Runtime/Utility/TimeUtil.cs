using System;
using System.Collections.Generic;
using UnityEngine;

namespace JakePerry.Unity
{
    /// <summary>
    /// Provides a means for multiple separate code systems to interact with the
    /// timescale without conflicts.
    /// <para>
    /// All modifications to timescale should be handled through this class.
    /// </para>
    /// </summary>
    public static class TimeUtil
    {
        /// <summary>
        /// A disposable object that represents the lifetime of a pause session.
        /// </summary>
        private sealed class PauseToken : IDisposable
        {
            public PauseToken() { _tokens.Add(this); UpdateTimeScale(); }

            public void Dispose()
            {
                if (_tokens.Remove(this))
                    UpdateTimeScale();
            }
        }

        /// <summary>
        /// Responsible for running validation logic in LateUpdate to ensure the timescale
        /// is not erroneously changed by external code.
        /// </summary>
        private sealed class UpdateLoopTether : MonoBehaviour
        {
            private bool m_appQuitting;

            private static void OnTimeScaleChangedUnexpectedly()
            {
                bool printWarning = true;

#if UNITY_EDITOR
                // Don't force timescale to the expected value while running in editor, as the user
                // may wish to directly modify timescale in editor for testing purposes (eg. via project preferences).
                var timeScale = Time.timeScale;

                printWarning = timeScale != _editorWarningTimescale;
                _editorWarningTimescale = timeScale;
#endif

                if (printWarning)
                    Debug.LogWarning($"An external source has set timescale to {Time.timeScale.ToString()} when it should be {_expected.ToString()}.");

                // In builds, force timescale to match the expected value.
                // This corrects erroneous behaviour that may arise when a third party plugin
                // directly modifies timesecale.
                // Example:
                // the Google Mobile Ads plugin directly sets timescale to 1 after playing an advert,
                // which can cause issues if this occurs while the game should be paused.
#if !UNITY_EDITOR
                UpdateTimeScale();
#endif
            }

            private void OnApplicationQuitting() => m_appQuitting = true;

            private void Awake()
            {
                Application.quitting += new Action(OnApplicationQuitting);
            }

            private void LateUpdate()
            {
                if (Time.timeScale != _expected)
                    OnTimeScaleChangedUnexpectedly();
            }

            private void OnDestroy()
            {
                if (!m_appQuitting)
                    Debug.LogError($"{typeof(UpdateLoopTether).FullName} instance was destroyed prematurely. This may cause subsequent issues.");
            }
        }

        private static readonly HashSet<PauseToken> _tokens = new HashSet<PauseToken>();

        private static float _timescale = 1f;
        private static float _expected = 1f;

#if UNITY_EDITOR
        private static float _editorWarningTimescale = float.MinValue;
#endif

        public static event Action<float> OnTimescaleChanged;

        /// <summary>
        /// Indicates whether any pause tokens are currently active.
        /// </summary>
        public static bool IsPaused => _tokens.Count > 0;

        /// <summary>
        /// The desired timescale value when time is not paused by a pause token.
        /// </summary>
        public static float Timescale
        {
            get => _timescale;
            set
            {
                value = Mathf.Abs(value);
                if (value == 0f)
                    Debug.LogWarning($"Timescale directly set to zero. Consider using a pause token.");

                _timescale = value;
                UpdateTimeScale();
            }
        }

        private static void UpdateTimeScale()
        {
            var value = _tokens.Count > 0 ? 0f : _timescale;

            if (AssignValueUtility.SetValueType(ref _expected, value))
            {
                OnTimescaleChanged?.Invoke(value);
            }

            Time.timeScale = _expected;
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            _timescale = _expected = Time.timeScale;

            var tether = new GameObject(typeof(UpdateLoopTether).FullName).AddComponent<UpdateLoopTether>();
            UnityEngine.Object.DontDestroyOnLoad(tether.gameObject);
            tether.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>
        /// Pauses the game &amp; provides a pause token.
        /// <para>
        /// This token must be disposed once the pause session ends.
        /// The game will remain paused as long as any token object remains active.
        /// </para>
        /// </summary>
        public static IDisposable GetPauseToken()
        {
            return new PauseToken();
        }
    }
}
