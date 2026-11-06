using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// A standard event with conditions, actions, comments, and nested sub-events.
    /// </summary>
    public sealed class SheetEvent {
        /// <summary>Creates an event. Every list is copied; a null list is treated as empty.</summary>
        /// <param name="conditions">The conditions that must all hold for the event to run.</param>
        /// <param name="actions">The actions run, in order, when the conditions hold.</param>
        /// <param name="eventsSub">The events run after this event's actions, in order.</param>
        /// <param name="comment">The author-facing comment for this event.</param>
        /// <param name="flagOnce">True when the actions run only on the first frame the conditions hold.</param>
        public SheetEvent(
            IEnumerable<SheetCondition> conditions = null,
            IEnumerable<SheetAction> actions = null,
            IEnumerable<SheetEvent> eventsSub = null,
            string comment = "",
            bool flagOnce = false
        ) {
            if (comment == null) {
                throw new ArgumentNullException(nameof(comment));
            }
            Conditions = Copy(conditions);
            Actions = Copy(actions);
            SubEvents = Copy(eventsSub);
            Comment = comment;
            Once = flagOnce;
        }
        
        /// <summary>Gets the conditions that must all hold for this event to run.</summary>
        public IReadOnlyList<SheetCondition> Conditions { get; }
        
        /// <summary>Gets the actions run, in order, when the conditions hold.</summary>
        public IReadOnlyList<SheetAction> Actions { get; }
        
        /// <summary>Gets the sub-events run after this event's actions.</summary>
        public IReadOnlyList<SheetEvent> SubEvents { get; }
        
        /// <summary>Gets the author-facing comment for this event.</summary>
        public string Comment { get; }
        
        /// <summary>Gets whether the actions run only on the first frame the conditions hold.</summary>
        public bool Once { get; }
        
        private static T[] Copy<T>(IEnumerable<T> values) {
            if (values == null) {
                return new T[0];
            }
            return new List<T>(values).ToArray();
        }
    }
}
