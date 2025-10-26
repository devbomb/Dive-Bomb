using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class TitleScreen : Control
    {
        private Control _buttons => GetNode<Control>("%Buttons");
        private Button _continueButton => GetNode<Button>("%Continue");

        private PageNavigator _pageNav => GetNode<PageNavigator>("%PageNavigator");

        public override void _Ready()
        {
            OpenMainPage();
        }

        public void OpenMainPage()
        {
            _pageNav.ChangePage(GetNode<Page>("%MainPage"));
        }

        public void OpenSaveFilesMenu()
        {
            _pageNav.ChangePage(GetNode<Page>("%SaveSlotManagementMenu"));
        }

        public void OpenUserSettingsMenu()
        {
            _pageNav.ChangePage(GetNode<Page>("%UserSettingsMenu"));
        }

        public void QuitToDesktop()
        {
            GetTree().Quit();
        }
    }
}