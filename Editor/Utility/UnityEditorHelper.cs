using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JakePerry.Unity
{
    public static class UnityEditorHelper
    {
        public static Texture2D GetMessageIcon(MessageType messageType)
        {
            var method = typeof(EditorGUIUtility).GetMethod("GetHelpIcon", BindingFlags.Static | BindingFlags.NonPublic);
            var result = method.Invoke(null, new object[] { messageType });

            return (Texture2D)result;
        }

        private static ValueMemberInfo GetMemberFromType(Type type, string memberName)
        {
            const MemberTypes kMemberFlags = MemberTypes.Field | MemberTypes.Property;
            const BindingFlags kBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            const BindingFlags kBaseTypeBindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

            var members = type.GetMember(memberName, kMemberFlags, kBindingFlags);

            // If no members are found, search up the type hierarchy for a matching private member
            while ((members?.Length ?? 0) == 0)
            {
                type = type.BaseType;

                if (type is null)
                    break;

                members = type.GetMember(memberName, kMemberFlags, kBaseTypeBindingFlags);
            }

            // Throw an exception if the member is not found (should never happen)
            if ((members?.Length ?? 0) == 0)
            {
                throw new Exception("Failed to find the serialized member via reflection.");
            }

            // Not sure if this could ever happen if we're only specifying the Field | Property flags, but detect this just in case...
            if (members.Length != 1)
            {
                throw new NotImplementedException("More than one member was found. This code will require some changes to discern the correct member.");
            }

            return ValueMemberInfo.FromMemberInfo(members[0]);
        }

        /// <returns>
        /// A <see cref="ValueMemberInfo"/> representing the appropriate <see cref="FieldInfo"/> or
        /// <see cref="PropertyInfo"/> corresponding to the member exposed by <paramref name="property"/>.
        /// </returns>
        public static ValueMemberInfo GetSerializedMember(SerializedProperty property)
        {
            // Get the object that contains the serialized member
            var targetObject = property.serializedObject.targetObject;

            var definingType = targetObject.GetType();
            var memberName = property.propertyPath;

            // This property may be nested several classes/structs deep. In this case,
            // the string returned by 'propertyPath' will split each respective member name
            // via the period character.
            if (memberName.IndexOf('.') > -1)
            {
                var splitChars = new char[] { '.' };
                var splitPath = memberName.Split(splitChars);

                var lastIndex = splitPath.Length - 1;

                // Iterate all segments except the last
                for (int i = 0; i < lastIndex; i++)
                {
                    memberName = splitPath[i];

                    var intermediateMember = GetMemberFromType(definingType, memberName);

                    definingType = intermediateMember.MemberType;
                }

                memberName = splitPath[lastIndex];
            }

            return GetMemberFromType(definingType, memberName);
        }
    }
}
