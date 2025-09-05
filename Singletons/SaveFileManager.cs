using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Godot;

namespace FastDragon
{
    public partial class SaveFileManager : Node
    {
        public static SaveFileManager Instance { get; private set; }
        public static SaveFile Current => Instance.CurrentFile;

        public int ActiveSlot { get; private set; } = 0;
        public SaveFile CurrentFile { get; private set; } = new();

        public override void _Ready()
        {
            Instance = this;
        }

        public void StartNewGame(int slotNumber)
        {
            ActiveSlot = slotNumber;
            CurrentFile = new();
        }

        public bool SlotHasData(int slotNumber)
        {
            return FileAccess.FileExists(SlotFilePath(slotNumber));
        }

        public void LoadFromSlot(int slotNumber)
        {
            string filePath = SlotFilePath(slotNumber);
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            file.Close();

            ActiveSlot = slotNumber;
            CurrentFile = SaveFile.FromJson(json);

            LevelTransitionManager.Instance.GoToLevelWithFadeToBlack(CurrentFile.CurrentLevel);
        }

        public void SaveToSlot(int slotNumber)
        {
            DirAccess.MakeDirRecursiveAbsolute("user://Saves");

            string filePath = SlotFilePath(slotNumber);
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
            file.StoreLine(CurrentFile.ToJson());
            file.Close();

            ActiveSlot = slotNumber;
        }

        private static string SlotFilePath(int number)
        {
            return $"user://Saves/Slot{number}.json";
        }
    }
}