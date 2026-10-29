using System;
using UnityEngine.UIElements;

namespace StreamVideo.Libs.KeyboardAvoidance
{
    /// <summary>
    /// Resolves UI Toolkit focus to the <see cref="TextField"/> that owns it.
    /// Focus often lands on an internal child of the field, so callers should not require the event target to be the field itself.
    /// </summary>
    internal static class TextFieldFocus
    {
        public static TextField FindTextField(VisualElement element)
        {
            while (element != null)
            {
                if (element is TextField field)
                {
                    return field;
                }

                element = element.parent;
            }

            return null;
        }

        public static bool IsEligible(
            TextField field,
            VisualElement focusRoot,
            VisualElement movable,
            Func<TextField, bool> ignore)
        {
            if (field == null || focusRoot == null || movable == null)
            {
                return false;
            }

            if (!focusRoot.Contains(field) || !movable.Contains(field))
            {
                return false;
            }

            if (focusRoot.panel != null && field.panel != focusRoot.panel)
            {
                return false;
            }

            // The field itself is often not focusable; focus lands on its inner text element.
            var enabled = field.panel != null ? field.enabledInHierarchy : field.enabledSelf;
            if (!enabled)
            {
                return false;
            }

            if (field.panel != null &&
                (field.resolvedStyle.display == DisplayStyle.None || field.resolvedStyle.visibility == Visibility.Hidden))
            {
                return false;
            }

            return ignore == null || !ignore(field);
        }
    }
}
