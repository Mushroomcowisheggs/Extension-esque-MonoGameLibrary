using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Records, per participant object, who owns it: the host, the caller after a retained failure,
    /// or nobody because it has been released. Every release attempt elects through this ledger, so
    /// "released once" holds per object rather than per admission.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ledger is keyed by reference identity, not by <see cref="object.Equals(object)"/>: two
    /// participants that compare equal are still two objects with two lifetimes, and releasing one
    /// must not mark the other as released. Membership in <see cref="GameHost"/> and
    /// <see cref="GameBuilder"/> uses the same identity, so the ledger and the admitted set cannot
    /// disagree about what "the same participant" means.
    /// </para>
    /// <para>
    /// Entries are added when a participant is admitted and are never removed while the host lives.
    /// That is what lets a withdrawn participant return: a withdrawal leaves the state
    /// <see cref="OwnershipState.Owned"/>, so admission succeeds again, while a release leaves
    /// <see cref="OwnershipState.Released"/>, so it cannot. The ledger holds only its own short lock
    /// and never runs foreign code, so a caller may hold a host lock across a ledger call.
    /// </para>
    /// </remarks>
    internal sealed class OwnershipLedger {
        private readonly object _lockLedger = new object();
        private readonly Dictionary<object, OwnershipState> _mapOwnership =
            new Dictionary<object, OwnershipState>(ReferenceEqualityComparer.Instance);
        
        /// <summary>
        /// Records that the host admits an object, or reports why it may not be admitted.
        /// </summary>
        /// <param name="target">The participant object. </param>
        /// <param name="statePrevious">The state the object was in, or
        /// <see cref="OwnershipState.Absent"/> when it was new. </param>
        /// <returns><c>true</c> if admission is allowed. </returns>
        internal bool TryRecordAdmission(object target, out OwnershipState statePrevious) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            lock (_lockLedger) {
                OwnershipState stateCurrent;
                if (!_mapOwnership.TryGetValue(target, out stateCurrent)) {
                    statePrevious = OwnershipState.Absent;
                    _mapOwnership.Add(target, OwnershipState.Owned);
                    return true;
                }
                statePrevious = stateCurrent;
                if (stateCurrent == OwnershipState.Owned) {
                    // A withdrawn participant is still owned and still admits: the withdrawal did not
                    // transfer it and did not release it.
                    return true;
                }
                return false;
            }
        }
        
        /// <summary>
        /// Elects exactly one release attempt for an object.
        /// </summary>
        /// <param name="target">The participant object. </param>
        /// <param name="statePrevious">The state the object was in, or
        /// <see cref="OwnershipState.Absent"/> when the ledger had no entry. </param>
        /// <returns><c>true</c> if this caller won the election and must perform the release. </returns>
        internal bool TryElectRelease(object target, out OwnershipState statePrevious) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            lock (_lockLedger) {
                OwnershipState stateCurrent;
                if (!_mapOwnership.TryGetValue(target, out stateCurrent)) {
                    // An object the host never recorded was constructed for this composition and is
                    // still unowned, so the first release claims it.
                    statePrevious = OwnershipState.Absent;
                    _mapOwnership.Add(target, OwnershipState.Released);
                    return true;
                }
                statePrevious = stateCurrent;
                if (stateCurrent == OwnershipState.Retained || stateCurrent == OwnershipState.Released) {
                    return false;
                }
                _mapOwnership[target] = OwnershipState.Released;
                return true;
            }
        }
        
        /// <summary>
        /// Records that a retained failure transferred an object to the caller.
        /// </summary>
        /// <param name="target">The participant object. </param>
        /// <remarks>
        /// A transfer to a caller who already owns the object, or to a caller after the host released
        /// it, is not a state the ledger can honour; the existing state is kept, because the host must
        /// neither claim back what it released nor take back what the caller was told to own.
        /// </remarks>
        internal void RecordTransferToCaller(object target) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            lock (_lockLedger) {
                OwnershipState stateCurrent;
                if (!_mapOwnership.TryGetValue(target, out stateCurrent)) {
                    _mapOwnership.Add(target, OwnershipState.Retained);
                    return;
                }
                if (stateCurrent == OwnershipState.Owned) {
                    _mapOwnership[target] = OwnershipState.Retained;
                }
            }
        }
        
        /// <summary>
        /// Gets the recorded ownership of an object.
        /// </summary>
        /// <param name="target">The participant object. </param>
        /// <returns>The state, or <see cref="OwnershipState.Absent"/> when the ledger has no entry. </returns>
        internal OwnershipState GetState(object target) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            lock (_lockLedger) {
                OwnershipState stateCurrent;
                if (!_mapOwnership.TryGetValue(target, out stateCurrent)) {
                    return OwnershipState.Absent;
                }
                return stateCurrent;
            }
        }
        
        /// <summary>
        /// Describes an ownership state for a diagnostic message.
        /// </summary>
        /// <param name="state">The state to describe. </param>
        /// <returns>A phrase that completes the sentence "the operation failed because ...". </returns>
        internal static string Describe(OwnershipState state) {
            if (state == OwnershipState.Owned) {
                return "this host already owns it";
            }
            if (state == OwnershipState.Retained) {
                return "a retained failure transferred it to the caller";
            }
            if (state == OwnershipState.Released) {
                return "this host has already released it";
            }
            return "the ledger has no record of it";
        }
    }
}
