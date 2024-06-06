using JakePerry.Collections;
using System;
using UnityEngine;

namespace JakePerry.Unity
{
    // TODO: Custom drawer for this type.
    [Serializable]
    public struct ColorSwatch
    {
        [SerializeField]
        private Color24[] m_colors;

        public ReadOnlyArray<Color24> Colors => m_colors;
    }
}
