using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class TimeTrialResultsMenu : Page
    {
        private AnimationPlayer _animator => GetNode<AnimationPlayer>("%Animator");
        private Control _buttons => GetNode<Control>("%Buttons");

        private TimeTrialCategoryHighScoresTable _categoriesTable => GetNode<TimeTrialCategoryHighScoresTable>("%CategoriesTable");
        private Label _categoryLabel => GetNode<Label>("%Category");
        private Label _yourTimeLabel => GetNode<Label>("%YourTimeLabel");
        private Label _bestTimeLabel => GetNode<Label>("%BestTimeLabel");

        public override void _Ready()
        {
            _animator.AnimationFinished += (StringName animName) =>
            {
                _buttons.Visible = true;
                FocusedControl.GrabFocus();

                _categoriesTable.Visible = true;
                _categoriesTable.Refresh();
            };
        }

        public override void OnPageEntered()
        {
            var ttm = this.GetLevel().TimeTrial;
            _yourTimeLabel.Text = ttm.TimerPhysicsTicks.FormatStopwatch();
            _bestTimeLabel.Text = TargetTimePhysicsTicks().FormatStopwatch();
            _categoryLabel.Text = GuessCategory().HumanReadableName();

            _buttons.Visible = false;
            _categoriesTable.Visible = false;

            _animator.Play("RESET");
            _animator.Advance(0);

            _animator.Play("Open");

            if (ttm.TimerPhysicsTicks < TargetTimePhysicsTicks())
                _animator.Queue("NewHighScore");
        }

        public void OnContinuePressed() => LevelTransitionManager.Instance.RespawnPlayerAfterDeath();

        private PhysicsTicks TargetTimePhysicsTicks()
        {
            var ttm = this.GetLevel().TimeTrial;
            return ttm.TargetTimePhysicsTicks(GuessCategory());
        }

        private TimeTrialCategory GuessCategory()
        {
            var ttm = this.GetLevel().TimeTrial;

            var prioritizedCategories = new[]
            {
                TimeTrialCategory.HundredPercent,
                TimeTrialCategory.FairyPercent,
                TimeTrialCategory.AnyPercent,
            };

            return prioritizedCategories
                .Where(ttm.IsRelevant)
                .Where(ttm.RequirementsMet)
                .First();
        }
    }
}