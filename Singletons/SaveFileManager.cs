using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Godot;


namespace FastDragon
{
    public partial class SaveFileManager : Node
    {
        private const string SavesFolder = "user://Saves";

        public static SaveFileManager Instance { get; private set; }
        public static SaveFile Current => Instance.CurrentFile;

        public int ActiveSlot { get; private set; } = -1;
        public SaveFile CurrentFile { get; private set; } = new();

        /// <summary>
        /// True if a level was started directly from the Godot editor.
        /// The game will not autosave if this is true.
        /// </summary>
        public bool NoActiveSlot() => ActiveSlot < 0;

        public override void _Ready()
        {
            Instance = this;
        }

        public void StartNewGame(
            int slotNumber,
            LevelManifest level,
            LevelManifest hub
        )
        {
            ActiveSlot = slotNumber;
            CurrentFile = new()
            {
                LastHubWorld = hub,
            };
            LevelTransitionManager.Instance.GoToLevelWithFadeToBlack(level);
        }

        public bool SlotHasData(int slotNumber)
        {
            return FileAccess.FileExists(SlotFilePath(slotNumber));
        }

        /// <summary>
        /// Returns the save data located in the given slot without technically
        /// "loading" it as the current file.  Useful if you want to display a
        /// summary of this save file in a menu.
        /// </summary>
        public PeekResult PeekSlot(int slotNumber, out SaveFile result)
        {
            result = null;

            if (!SlotHasData(slotNumber))
                return PeekResult.Empty;

            try
            {
                string filePath = SlotFilePath(slotNumber);
                using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                file.Close();

                // Check the version number to see if it's compatible
                var jobj = JObject.Parse(json);
                int? version = jobj.GetValue(nameof(SaveFile.SaveFormatVersion))?.ToObject<int>();

                if (version == null || version.Value < SaveFile.MinSaveFormatVersion)
                    return PeekResult.TooOld;

                if (version > SaveFile.CurrentSaveFormatVersion)
                    return PeekResult.TooNew;

                result = SaveFile.FromJson(json);
                return PeekResult.Valid;
            }
            catch (Exception e)
            {
                GD.PushError(e);
                return PeekResult.Broken;
            }
        }

        public enum PeekResult
        {
            /// <summary>
            ///     There is no data in this save slot
            /// </summary>
            Empty,

            /// <summary>
            ///     This save is safe to load
            /// </summary>
            Valid,

            /// <summary>
            ///     This save contains invalid json, or otherwise causes
            ///     an exception when trying to parse it
            /// </summary>
            Broken,

            /// <summary>
            ///     This save is incompatible with the current version of the
            ///     game because the version that wrote it is too old.
            /// </summary>
            TooOld,

            /// <summary>
            ///     This save was made by a newer version of the game than the
            ///     version being played.
            /// </summary>
            TooNew,
        }

        public void LoadFromSlot(int slotNumber)
        {
            if (PeekSlot(slotNumber, out var saveFile) != PeekResult.Valid)
            {
                GD.PushError($"Refusing to invalid save in slot {slotNumber}");
                return;
            }

            ActiveSlot = slotNumber;
            CurrentFile = saveFile;
            LevelTransitionManager.Instance.GoToLevelWithFadeToBlack(CurrentFile.CurrentLevel);
        }

        public void SaveToSlot(int slotNumber)
        {
            // HACK: Update the save format right before saving it.
            // I consider this a hack because you wouldn't expect saving
            // something to also modify it in memory.
            CurrentFile.SaveFormatVersion = SaveFile.CurrentSaveFormatVersion;

            DirAccess.MakeDirRecursiveAbsolute(SavesFolder);

            string filePath = SlotFilePath(slotNumber);
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
            file.StoreLine(CurrentFile.ToJson());
            file.Close();

            ActiveSlot = slotNumber;
        }

        /// <summary>
        /// Saves the game to the active slot, if a slot is active.
        /// </summary>
        public void RequestAutosave()
        {
            if (!NoActiveSlot())
                SaveToSlot(ActiveSlot);
        }

        public void EraseSlot(int slotNumber)
        {
            string globalizedPath = ProjectSettings.GlobalizePath(SlotFilePath(slotNumber));
            OS.MoveToTrash(globalizedPath);
        }

        private static string SlotFilePath(int number)
        {
            return $"{SavesFolder}/Slot{number}.json";
        }
    }
}