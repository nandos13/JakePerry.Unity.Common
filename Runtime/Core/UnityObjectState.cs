using System;
using System.Runtime.CompilerServices;

namespace JakePerry.Unity
{
    /// <summary>
    /// Represents the state of an <see cref="UnityEngine.Object"/>.
    /// </summary>
    public struct UnityObjectState : IEquatable<UnityObjectState>
    {
        private const byte _Null = 0;
        private const byte _Destroyed = 1;
        private const byte _Valid = 2;
#if UNITY_EDITOR
        private const byte _Unassigned = 3;
#endif

        private readonly byte m_state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UnityObjectState(byte state) { m_state = state; }

        /// <summary>
        /// Returns <see langword="true"/> if the object is a <see langword="null"/> reference,
        /// or an unassigned deserialized reference in the editor.
        /// <para/>
        /// See: <see cref="IsUnassigned"/>.
        /// </summary>
        public readonly bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if UNITY_EDITOR
            get => m_state == _Null || m_state == _Unassigned;
#else
            get => m_state == _Null;
#endif
        }

        /// <summary>
        /// Returns <see langword="true"/> if the object is a <see langword="null"/> reference.
        /// </summary>
        public readonly bool IsNullReference
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_state == _Null;
        }

        /// <summary>
        /// When evaluated while running in the editor, returns <see langword="true"/>
        /// if the object is an unassigned reference. Outside of the editor, returns
        /// <see langword="true"/> if the object is a null reference.
        /// </summary>
        /// <remarks>
        /// <b>Explanation:</b>
        /// <para/>
        /// Unassigned references only existing within the Unity Editor. An unassigned reference
        /// is a fake object created by Unity that does not represent a valid object in unmanaged memory.
        /// Any '<c>null</c>' references to an <see cref="UnityEngine.Object"/> (or derived class)
        /// that are deserialized while running in the editor are assigned this fake object instance,
        /// with an instance ID of zero.
        /// This fake object allows Unity to alert the user if they attempt to
        /// access the referenced object with this undoubtedly familiar message:
        /// <para/>
        /// <c>The variable <i>variable_name</i> of <i>script_name</i> has not been assigned.
        /// You probably need to assign the <i>variable_name</i> variable of the <i>script_name</i>
        /// script in the inspector.</c>
        /// </remarks>
        public readonly bool IsUnassigned
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if UNITY_EDITOR
            get => m_state == _Unassigned;
#else
            get => m_state == _Null;
#endif
        }

        /// <summary>
        /// Returns <see langword="true"/> if the object is destroyed.
        /// </summary>
        public readonly bool IsDestroyed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_state == _Destroyed;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the object is not destroyed.
        /// </summary>
        public readonly bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_state == _Valid;
        }

        public readonly bool Equals(UnityObjectState other)
        {
            return m_state == other.m_state;
        }

        public readonly override bool Equals(object obj)
        {
            return obj is UnityObjectState other && this.Equals(other);
        }

        public readonly override int GetHashCode()
        {
            return m_state.GetHashCode();
        }

        public readonly override string ToString()
        {
            return m_state switch
            {
#if UNITY_EDITOR
                _Unassigned => "unassigned",
#endif
                _Destroyed => "destroyed",
                _Valid => "valid",
                _ => "null"
            };
        }

        /// <summary>
        /// Create a representation of a <see langword="null"/> reference.
        /// </summary>
        public static UnityObjectState NullReference
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_Null);
        }

        /// <summary>
        /// When running in the editor, creates a representation of an unassigned reference;
        /// Otherwise, creates a representation of a <see langword="null"/> reference.
        /// <para/>
        /// See: <see cref="IsUnassigned"/>.
        /// </summary>
        public static UnityObjectState UnassignedReference
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if UNITY_EDITOR
            get => new(_Unassigned);
#else
            get => new(_Null);
#endif
        }

        /// <summary>
        /// Create a representation of a destroyed object.
        /// </summary>
        public static UnityObjectState DestroyedObject
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_Destroyed);
        }

        /// <summary>
        /// Create a representation of a valid object that has not been destroyed.
        /// </summary>
        public static UnityObjectState ValidObject
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_Valid);
        }
    }
}
