using JakePerry.Collections;
using UnityEngine;

namespace JakePerry.Unity
{
    [CreateAssetMenu(fileName = "ColorSwatch", menuName = "JakePerry/ColorSwatch")]
    public sealed class ColorSwatch : ScriptableObject, IValueFromUnityObject<Color24[]>
    {
        [SerializeField]
        private Color24[] m_colors;

        public ReadOnlyArray<Color24> Colors => m_colors;

        Color24[] IValueFromUnityObject<Color24[]>.Value => m_colors;
    }
}
