using System;
using System.Collections.Generic;
using MonoGameLibrary.Core.Lifecycle;

namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// The host's schedule algebra: the admission entry, the total order over entries, and the
    /// immutable snapshot a traversal iterates.
    /// </summary>
    /// <remarks>
    /// A participant is stored with the order it was admitted under and the position at which the
    /// composition root admitted it. The host reads that order once, from <see cref="ScheduleOptions"/>,
    /// and never reads an order from the participant again, so the entry is the authority for the life
    /// of the admission. The two keys together make the comparison total: without the admission index a
    /// shared order would leave the pair unordered, and the runtime's list sort is unstable, so the
    /// traversal order of the tied participants would follow the order the list happened to be built in.
    /// <para>
    /// No member of this class takes a lock. Every caller holds the host lock, which is what makes the
    /// dirty flag, the generation counter and the published snapshot one consistent state: a rebuild
    /// copies the entry list and captures the generation under the lock, sorts its private copy outside
    /// the lock, and publishes by a single reference assignment only when the generation has not moved.
    /// A published list is never sorted, cleared or reused, so a traversal that captured one keeps a
    /// stable snapshot for its whole loop.
    /// </para>
    /// </remarks>
    internal sealed class Schedule {
        private readonly List<ScheduleUpdateEntry> _listUpdateEntries = new List<ScheduleUpdateEntry>();
        private readonly List<ScheduleDrawEntry> _listDrawEntries = new List<ScheduleDrawEntry>();
        
        // The published schedules. A rebuild assigns a fresh list here; nothing mutates a published list.
        private List<ScheduleUpdateEntry> _listUpdateEntriesPublished = new List<ScheduleUpdateEntry>();
        private List<ScheduleDrawEntry> _listDrawEntriesPublished = new List<ScheduleDrawEntry>();
        
        private bool _flagUpdateOrderDirty = true;
        private bool _flagDrawOrderDirty = true;
        
        // Membership generation, bumped whenever either entry list changes. A traversal captures it with
        // its copy and commits only if it is unchanged: the dirty flag alone cannot tell "dirty when I
        // took the copy" from "dirty after I took it", and committing in the second case would publish
        // the pre-admission order and clear the flag, dropping the new participant until the next
        // unrelated membership change.
        private int _generation = 0;
        
        /// <summary>
        /// Gets the membership generation. Callers hold the host lock.
        /// </summary>
        public int Generation {
            get { return _generation; }
        }
        
        /// <summary>
        /// Gets a value indicating whether the update schedule must be rebuilt before it is iterated.
        /// Callers hold the host lock.
        /// </summary>
        public bool IsUpdateDirty {
            get { return _flagUpdateOrderDirty; }
        }
        
        /// <summary>
        /// Gets a value indicating whether the draw schedule must be rebuilt before it is iterated.
        /// Callers hold the host lock.
        /// </summary>
        public bool IsDrawDirty {
            get { return _flagDrawOrderDirty; }
        }
        
        /// <summary>
        /// Adds one update participant under the order and admission index it is admitted with.
        /// </summary>
        /// <param name="participantUpdateable">The update participant. </param>
        /// <param name="order">The update order supplied at admission. </param>
        /// <param name="indexAdmission">The position at which the participant was admitted. </param>
        public void AddUpdate(IUpdateable participantUpdateable, int order, int indexAdmission) {
            if (participantUpdateable == null) {
                throw new ArgumentNullException(nameof(participantUpdateable));
            }
            _listUpdateEntries.Add(new ScheduleUpdateEntry(participantUpdateable, order, indexAdmission));
            _flagUpdateOrderDirty = true;
            _generation += 1;
        }
        
        /// <summary>
        /// Adds one draw participant under the order and admission index it is admitted with.
        /// </summary>
        /// <param name="participantDrawable">The draw participant. </param>
        /// <param name="order">The draw order supplied at admission. </param>
        /// <param name="indexAdmission">The position at which the participant was admitted. </param>
        public void AddDraw(IDrawable participantDrawable, int order, int indexAdmission) {
            if (participantDrawable == null) {
                throw new ArgumentNullException(nameof(participantDrawable));
            }
            _listDrawEntries.Add(new ScheduleDrawEntry(participantDrawable, order, indexAdmission));
            _flagDrawOrderDirty = true;
            _generation += 1;
        }
        
        /// <summary>
        /// Removes every entry that schedules the supplied object, and invalidates both schedules.
        /// </summary>
        /// <param name="module">The participant to remove. </param>
        /// <remarks>
        /// Both schedules are invalidated unconditionally rather than only when an entry was removed,
        /// because a rollback that leaves a published schedule holding a withdrawn participant is
        /// exactly the state this method exists to prevent. The generation is bumped once, so a copy
        /// taken before the removal cannot be committed after it.
        /// </remarks>
        public void Remove(object module) {
            if (module == null) {
                throw new ArgumentNullException(nameof(module));
            }
            if (module is IUpdateable) {
                _listUpdateEntries.RemoveAll(delegate(ScheduleUpdateEntry entry) {
                    return ReferenceEquals(entry.UpdateableParticipant, module);
                });
            }
            if (module is IDrawable) {
                _listDrawEntries.RemoveAll(delegate(ScheduleDrawEntry entry) {
                    return ReferenceEquals(entry.DrawableParticipant, module);
                });
            }
            _flagUpdateOrderDirty = true;
            _flagDrawOrderDirty = true;
            _generation += 1;
        }
        
        /// <summary>
        /// Copies the current update entries so the caller can sort them outside the host lock.
        /// </summary>
        /// <returns>A private copy of the update entries. </returns>
        public List<ScheduleUpdateEntry> CopyUpdateEntries() {
            return new List<ScheduleUpdateEntry>(_listUpdateEntries);
        }
        
        /// <summary>
        /// Copies the current draw entries so the caller can sort them outside the host lock.
        /// </summary>
        /// <returns>A private copy of the draw entries. </returns>
        public List<ScheduleDrawEntry> CopyDrawEntries() {
            return new List<ScheduleDrawEntry>(_listDrawEntries);
        }
        
        /// <summary>
        /// Publishes a sorted update schedule if no membership change happened while it was sorted.
        /// </summary>
        /// <param name="listSortedEntries">The sorted copy the caller owns. </param>
        /// <param name="generationSnapshot">The generation captured with the copy. </param>
        /// <remarks>
        /// When the generation moved, the copy is dropped and the dirty flag stays set: the next
        /// traversal rebuilds from the current entry list, so an admission that raced the sort is
        /// delayed by a frame instead of lost.
        /// </remarks>
        public void PublishUpdate(List<ScheduleUpdateEntry> listSortedEntries, int generationSnapshot) {
            if (listSortedEntries == null) {
                throw new ArgumentNullException(nameof(listSortedEntries));
            }
            if (_generation != generationSnapshot) {
                return;
            }
            _listUpdateEntriesPublished = listSortedEntries;
            _flagUpdateOrderDirty = false;
        }
        
        /// <summary>
        /// Publishes a sorted draw schedule if no membership change happened while it was sorted.
        /// </summary>
        /// <param name="listSortedEntries">The sorted copy the caller owns. </param>
        /// <param name="generationSnapshot">The generation captured with the copy. </param>
        public void PublishDraw(List<ScheduleDrawEntry> listSortedEntries, int generationSnapshot) {
            if (listSortedEntries == null) {
                throw new ArgumentNullException(nameof(listSortedEntries));
            }
            if (_generation != generationSnapshot) {
                return;
            }
            _listDrawEntriesPublished = listSortedEntries;
            _flagDrawOrderDirty = false;
        }
        
        /// <summary>
        /// Gets the published update schedule a traversal iterates.
        /// </summary>
        /// <returns>The published update entries. </returns>
        public List<ScheduleUpdateEntry> PublishedUpdateEntries() {
            return _listUpdateEntriesPublished;
        }
        
        /// <summary>
        /// Gets the published draw schedule a traversal iterates.
        /// </summary>
        /// <returns>The published draw entries. </returns>
        public List<ScheduleDrawEntry> PublishedDrawEntries() {
            return _listDrawEntriesPublished;
        }
        
        /// <summary>
        /// Empties the schedule and replaces both published lists with empty ones.
        /// </summary>
        /// <remarks>
        /// Used when the host detaches its admissions: the entries must not keep a participant alive
        /// after the host has handed ownership on, and a traversal must see an empty schedule rather
        /// than a stale snapshot.
        /// </remarks>
        public void Clear() {
            _listUpdateEntries.Clear();
            _listDrawEntries.Clear();
            _listUpdateEntriesPublished = new List<ScheduleUpdateEntry>();
            _listDrawEntriesPublished = new List<ScheduleDrawEntry>();
        }
        
        /// <summary>
        /// Orders two update entries: ascending admitted order, and on a tie the admission index.
        /// </summary>
        /// <param name="left">The left entry. </param>
        /// <param name="right">The right entry. </param>
        /// <returns>A negative number if <paramref name="left"/> precedes, otherwise its comparison. </returns>
        public static int CompareUpdate(ScheduleUpdateEntry left, ScheduleUpdateEntry right) {
            int comparison = left.Order.CompareTo(right.Order);
            if (comparison != 0) {
                return comparison;
            }
            return left.AdmissionIndex.CompareTo(right.AdmissionIndex);
        }
        
        /// <summary>
        /// Orders two draw entries: ascending admitted order, and on a tie the admission index.
        /// </summary>
        /// <param name="left">The left entry. </param>
        /// <param name="right">The right entry. </param>
        /// <returns>A negative number if <paramref name="left"/> precedes, otherwise its comparison. </returns>
        public static int CompareDraw(ScheduleDrawEntry left, ScheduleDrawEntry right) {
            int comparison = left.Order.CompareTo(right.Order);
            if (comparison != 0) {
                return comparison;
            }
            return left.AdmissionIndex.CompareTo(right.AdmissionIndex);
        }
    }
    
    /// <summary>
    /// One update participant together with the fixed keys the host admitted it under.
    /// </summary>
    internal readonly struct ScheduleUpdateEntry {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleUpdateEntry"/> struct.
        /// </summary>
        /// <param name="participantUpdateable">The update participant. </param>
        /// <param name="order">The update order supplied at admission. </param>
        /// <param name="indexAdmission">The position at which the participant was admitted. </param>
        public ScheduleUpdateEntry(IUpdateable participantUpdateable, int order, int indexAdmission) {
            if (participantUpdateable == null) {
                throw new ArgumentNullException(nameof(participantUpdateable));
            }
            UpdateableParticipant = participantUpdateable;
            Order = order;
            AdmissionIndex = indexAdmission;
        }
        
        /// <summary>
        /// Gets the update participant.
        /// </summary>
        public IUpdateable UpdateableParticipant { get; }
        
        /// <summary>
        /// Gets the update order the participant was admitted under.
        /// </summary>
        public int Order { get; }
        
        /// <summary>
        /// Gets the position at which the participant was admitted.
        /// </summary>
        public int AdmissionIndex { get; }
    }
    
    /// <summary>
    /// One draw participant together with the fixed keys the host admitted it under.
    /// </summary>
    internal readonly struct ScheduleDrawEntry {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleDrawEntry"/> struct.
        /// </summary>
        /// <param name="participantDrawable">The draw participant. </param>
        /// <param name="order">The draw order supplied at admission. </param>
        /// <param name="indexAdmission">The position at which the participant was admitted. </param>
        public ScheduleDrawEntry(IDrawable participantDrawable, int order, int indexAdmission) {
            if (participantDrawable == null) {
                throw new ArgumentNullException(nameof(participantDrawable));
            }
            DrawableParticipant = participantDrawable;
            Order = order;
            AdmissionIndex = indexAdmission;
        }
        
        /// <summary>
        /// Gets the draw participant.
        /// </summary>
        public IDrawable DrawableParticipant { get; }
        
        /// <summary>
        /// Gets the draw order the participant was admitted under.
        /// </summary>
        public int Order { get; }
        
        /// <summary>
        /// Gets the position at which the participant was admitted.
        /// </summary>
        public int AdmissionIndex { get; }
    }
}
