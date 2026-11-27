using System;

namespace MonoGameLibrary.Core.Time {
    /// <summary>
    /// Represents a snapshot of timing values for the current frame. 
    /// </summary>
    public readonly struct FrameTime : IEquatable<FrameTime> {
        /// <summary>
        /// Gets the total elapsed time since the start of the game. 
        /// </summary>
        public TimeSpan TotalTimeSpan { get; }
        
        /// <summary>
        /// Gets the elapsed time since the previous frame.
        /// <para>
        /// <b>Warning:</b> May be <see cref="TimeSpan.Zero"/> in special states
        /// (e.g., pause). Always check before using in division. 
        /// </para>
        /// </summary>
        public TimeSpan DeltaTimeSpan { get; }
        
        /// <summary>
        /// Creates a zero-initialized FrameTime, 
        /// a <see cref="FrameTime"/> with both <see cref="TotalTimeSpan"/>
        /// and <see cref="DeltaTimeSpan"/> set to zero. 
        /// </summary>
        public static FrameTime Zero { get; } = new FrameTime(TimeSpan.Zero, TimeSpan.Zero);
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FrameTime"/> struct.
        /// </summary>
        /// <param name="spanTotalTime">Total elapsed time span since start. </param>
        /// <param name="spanDeltaTime">Elapsed time span since the previous frame. </param>
        public FrameTime(TimeSpan spanTotalTime, TimeSpan spanDeltaTime) {
            TotalTimeSpan = spanTotalTime;
            DeltaTimeSpan = spanDeltaTime;
        }
        
        /// <summary>
        /// Indicates whether the current <see cref="FrameTime"/> is equal to another <see cref="FrameTime"/>. 
        /// </summary>
        /// <param name="other">A <see cref="FrameTime"/> to compare with this instance. </param>
        /// <returns><c>true</c> if both <see cref="TotalTimeSpan"/> and <see cref="DeltaTimeSpan"/>
        /// are equal; otherwise, <c>false</c>. </returns>
        public bool Equals(FrameTime other) {
            // bool flagTotalEqual = TotalTimeSpan.Equals(other.TotalTimeSpan);
            // bool flagDeltaEqual = DeltaTimeSpan.Equals(other.DeltaTimeSpan);
            return (TotalTimeSpan.Equals(other.TotalTimeSpan) && DeltaTimeSpan.Equals(other.DeltaTimeSpan));
        }
        
        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="FrameTime"/>. 
        /// </summary>
        /// <param name="obj">The object to compare with the current instance. </param>
        /// <returns><c>true</c> if <paramref name="obj"/> is a <see cref="FrameTime"/>
        /// and has the same values; otherwise, <c>false</c>. </returns>
        public override bool Equals(object obj) {
            if (obj is FrameTime other) {
                return Equals(other);
            } else {
                return false;
            }
        }
        
        /// <summary>
        /// Returns a hash code for this <see cref="FrameTime"/>. 
        /// </summary>
        /// <returns>A hash code computed from <see cref="TotalTimeSpan"/> and <see cref="DeltaTimeSpan"/>. </returns>
        public override int GetHashCode() {
            return HashCode.Combine(TotalTimeSpan, DeltaTimeSpan);
        }
        
        /// <summary>
        /// Compares two <see cref="FrameTime"/> values for equality. 
        /// </summary>
        /// <param name="left">The first <see cref="FrameTime"/>. </param>
        /// <param name="right">The second <see cref="FrameTime"/>. </param>
        /// <returns><c>true</c> if the two instances are equal; otherwise, <c>false</c>. </returns>
        public static bool operator ==(FrameTime left, FrameTime right) {
            return left.Equals(right);
        }
        
        /// <summary>
        /// Compares two <see cref="FrameTime"/> values for inequality.
        /// </summary>
        /// <param name="left">The first <see cref="FrameTime"/>. </param>
        /// <param name="right">The second <see cref="FrameTime"/>. </param>
        /// <returns><c>true</c> if the two instances are not equal; otherwise, <c>false</c>. </returns>
        public static bool operator !=(FrameTime left, FrameTime right) {
            return !left.Equals(right);
        }
        
        /// <summary>
        /// Returns a string that represents the current <see cref="FrameTime"/>. 
        /// </summary>
        /// <returns>A string in the format "Total: {TotalTimeSpan}, Delta: {DeltaTimeSpan}". </returns>
        public override string ToString() {
            return $"Total: {TotalTimeSpan}, Delta: {DeltaTimeSpan}";
        }
    }
}