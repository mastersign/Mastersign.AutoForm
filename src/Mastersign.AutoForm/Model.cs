using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mastersign.AutoForm
{
    class AutomationProject
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        public List<Action> Actions { get; set; } = new List<Action>();

        public bool HasErrors => Errors.Any();

        private string ErrorString => HasErrors
            ? "Errors:\n\t" + string.Join("\n\t", Errors)
            : "No Errors";

        public override string ToString() =>
            $"Automation Project\n\tName: {Name}\n\tDescription: {Description}\n" +
            $"{ErrorString}\n--- Actions ---\n{string.Join("\n", Actions)}";
    }

    abstract class Action
    {
    }

    class PauseAction : Action
    {
        public string Label { get; set; } = "Waiting for user to proceed";

        public override string ToString() => $"Pause: {Label}";
    }

    class NavigateAction : Action
    {
        public string Url { get; set; }

        public int Timeout { get; set; } = 5000;

        public override string ToString() => $"Navigate: {Url}";
    }

    class DelayAction : Action
    {
        public int Duration { get; set; } = 1000;

        public override string ToString() => $"Delay: {Duration}ms";
    }

    abstract class TargetedAction : Action
    {
        public string Selector { get; set; }

        public int Timeout { get; set; } = 500;
    }

    class CheckTextAction : TargetedAction
    {
        public string Text { get; set; }

        public override string ToString() => $"CheckText: {Selector} for '{Text}' (Timeout = {Timeout}ms)";
    }

    class ClickAction : TargetedAction
    {
        public override string ToString() => $"Click: {Selector} (Timeout = {Timeout}ms)";
    }

    class WaitForAction : TargetedAction
    {
        public bool Visible { get; set; } = true;

        public override string ToString() => $"WaitFor: {Selector} (Timeout = {Timeout}ms)";
    }

    class InputAction : TargetedAction
    {
        public string Value { get; set; }

        public override string ToString() => $"Input: {Selector} (Timeout = {Timeout}ms)";
    }

    class FormAction : TargetedAction
    {
        public List<FormField> Inputs { get; set; } = new List<FormField>();

        public override string ToString() => $"Form: {Selector} (Timeout = {Timeout}ms)\n\t{string.Join("\n\t", Inputs)}";
    }

    class FormField
    {
        public string Name { get; set; }

        public string Value { set; get; }

        public override string ToString() => $"Set: {Name} = \"{Value}\"";
    }
}
