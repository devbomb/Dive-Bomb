using Godot;
using System;
using System.Linq;

namespace FastDragon
{
    public partial class SaveSlotManagementMenu : Page
    {
        [Export(PropertyHint.File, hintString: "*.tscn")] public string NewGameLevel;

        public override void OnPageEntered()
        {
            var buttons = GetNode<Control>("%SlotButtons")
                .EnumerateChildren()
                .Cast<Button>()
                .ToArray();

            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].Text = SlotText(i);
            }
        }

        public void OnSlotChosen(int slotNumber)
        {
            if (SaveFileManager.Instance.SlotHasData(slotNumber))
            {
                SaveFileManager.Instance.LoadFromSlot(slotNumber);
            }
            else
            {
                SaveFileManager.Instance.StartNewGame(slotNumber, NewGameLevel);
            }
        }

        private string SlotText(int slotNumber)
        {
            if (!SaveFileManager.Instance.SlotHasData(slotNumber))
                return "New Game";

            // Peek at the save file to learn which level it was saved in
            var saveFile = SaveFileManager.Instance.PeekSlot(slotNumber);

            string levelScenePath = saveFile.CurrentLevel;
            string levelName = AtlasCache.Instance.GetEntry(levelScenePath).HumanReadableName;

            return $"Continue -- {levelName}";
        }
    }
}
