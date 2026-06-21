using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
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
            string levelScenePath,
            string hubScenePath
        )
        {
            ActiveSlot = slotNumber;
            CurrentFile = new()
            {
                LastHubWorld = hubScenePath,
            };
            LevelTransitionManager.Instance.GoToLevelWithFadeToBlack(levelScenePath);
        }

        public bool SlotHasData(int slotNumber)
        {
            return FileAccess.FileExists(SlotFilePath(slotNumber));
        }

        /// <summary>
        /// Returns the save data located in the given slot without technically
        /// "loading" it as the current file.  Useful if you want to display a
        /// summary of this save file in a menu.
        ///
        /// Returns null if there is no save file in this slot.
        /// </summary>
        public SaveFile PeekSlot(int slotNumber)
        {
            if (!SlotHasData(slotNumber))
                return null;

            string filePath = SlotFilePath(slotNumber);
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            file.Close();

            return SaveFile.FromJson(json);
        }

        public void LoadFromSlot(int slotNumber)
        {
            ActiveSlot = slotNumber;
            CurrentFile = PeekSlot(slotNumber);

            LevelTransitionManager.Instance.GoToLevelWithFadeToBlack(CurrentFile.CurrentLevel);
        }

        public void SaveToSlot(int slotNumber)
        {
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