using System;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Composition helpers for the event-sheet extension.
    /// </summary>
    public static class GameBuilderExtensions {
        /// <summary>Registers the event-sheet service and module with the host.</summary>
        /// <param name="builder">The builder performing the composition fold.</param>
        /// <param name="optionsSheet">The load and schedule options; defaults are used when empty.</param>
        /// <param name="storeSheet">The JSON source; a file store below the options directory is used when empty.</param>
        /// <param name="vocabularySheet">The instruction vocabulary; the built-in vocabulary is used when empty.</param>
        /// <returns>The same builder, so the call can be chained.</returns>
        public static GameBuilder UseEventSheets(
            this GameBuilder builder,
            ISheetStore storeSheet,
            Optional<SheetOptions> optionsSheet = default,
            Optional<ISheetVocabulary> vocabularySheet = default
        ) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (storeSheet == null) {
                throw new ArgumentNullException(nameof(storeSheet));
            }
            
            SheetOptions optionsResolved;
            if (optionsSheet.HasValue) {
                optionsResolved = optionsSheet.Value;
            } else {
                optionsResolved = new SheetOptions();
            }
            optionsResolved.Validate();
            
            ISheetStore storeResolved = storeSheet;
            
            ISheetVocabulary vocabularyResolved;
            if (vocabularySheet.HasValue) {
                vocabularyResolved = vocabularySheet.Value;
            } else {
                vocabularyResolved = SheetBuiltIns.CreateVocabulary();
            }
            
            ILogger loggerResolved;
            if (builder.TryGetService<ILogger>(out loggerResolved)) {
                if (loggerResolved == null) {
                    loggerResolved = NullLogger.Instance;
                }
            } else {
                loggerResolved = NullLogger.Instance;
            }
            
            SheetModule module = new SheetModule(
                storeResolved,
                optionsResolved,
                vocabularyResolved,
                loggerResolved
            );
            module.Register(builder);
            return builder;
        }
    }
}
