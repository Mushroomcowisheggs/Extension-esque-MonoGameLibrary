namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Supplies the fixed schedule keys for a participant at admission. 
    /// </summary>
    /// <remarks>
    /// Order is a host-owned scheduling input, not a readable member of <see cref="Lifecycle.IUpdateable"/>
    /// or <see cref="Lifecycle.IDrawable"/>. The host copies the values into its schedule entry when the
    /// participant is admitted and never reads them from the participant again, so a participant cannot
    /// change its position by mutating a property the host does not observe. Lower values run first;
    /// participants that share a value are ordered by their admission index. The values are fixed for
    /// the lifetime of that admission.
    /// </remarks>
    public readonly struct ScheduleOptions {
        /// <summary>
        /// Initializes a new set of schedule keys. 
        /// </summary>
        /// <param name="orderUpdate">The update order, used only when the participant implements <see cref="Lifecycle.IUpdateable"/>. </param>
        /// <param name="orderDraw">The draw order, used only when the participant implements <see cref="Lifecycle.IDrawable"/>. </param>
        public ScheduleOptions(int orderUpdate = 0, int orderDraw = 0) {
            UpdateOrder = orderUpdate;
            DrawOrder = orderDraw;
        }
        
        /// <summary>
        /// Gets the order used when the participant is updated. 
        /// </summary>
        public int UpdateOrder { get; }
        
        /// <summary>
        /// Gets the order used when the participant is drawn. 
        /// </summary>
        public int DrawOrder { get; }
        
        /// <summary>
        /// Gets the default options: zero for both phases, resolved by admission order. 
        /// </summary>
        public static ScheduleOptions Default { get { return default(ScheduleOptions); } }
    }
}
