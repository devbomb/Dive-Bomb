using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class PageNavigator : Control
    {
        public Page CurrentPage => this.EnumerateChildren()
            .Where(c => c is Page)
            .Cast<Page>()
            .FirstOrDefault(p => p.Visible);

        public void ChangePage(Page targetPage)
        {
            var pages = this.EnumerateChildren()
                .Where(c => c is Page)
                .Cast<Page>();

            foreach (var page in pages)
            {
                bool isTarget = page == targetPage;

                page.Visible = isTarget;
                page.ProcessMode = isTarget
                    ? ProcessModeEnum.Inherit
                    : ProcessModeEnum.Disabled;
            }

            targetPage?.OnPageEntered();
            targetPage?.FocusedControl?.GrabFocus();
        }
    }
}