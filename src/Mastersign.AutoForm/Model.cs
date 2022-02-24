using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mastersign.AutoForm
{
    class AutomationProject
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int? ViewportWidth { get; set; }
        public int? ViewportHeight { get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        public List<Action> Actions { get; set; } = new List<Action>();

        public List<string> RecordColumns { get; set; } = new List<string>();

        public List<Record> Records { get; set; } = new List<Record>();

        public int SkippedActions { get; set; }

        public bool HasErrors => Errors.Any();

        private string ErrorString => HasErrors
            ? Errors.Count + "\n\t- " + string.Join("\n\t- ", Errors)
            : "No Errors";

        private string ActionListString => $"   Actions    \n-------------\n{string.Join("\n", Actions)}";

        private string RecordSchemaString => RecordColumns.Any()
            ? "\n\t- " + string.Join("\n\t- ", RecordColumns)
            : "None";

        public override string ToString() =>
            "Automation Project" +
            $"\n\tName:                {Name}" +
            $"\n\tDescription:         {Description}" +
            $"\n\tActions:             {Actions.Count}" +
            $"\n\tSkipped Actions:     {SkippedActions}" +
            $"\n\tConditional Actions: {Actions.Where(a => a.HasCondition).Count()}" +
            $"\n\tRecords:             {Records.Count}" +
            $"\n\tRecord Columns:      {RecordColumns.Count}" +
            $"\nRecord Schema: {RecordSchemaString}" +
            $"\nErrors: {ErrorString}" +
            $"\n\n{ActionListString}";
    }

    abstract class Action
    {
        public int Row { get; set; }

        public CellValue ConditionExpression { get; set; }

        public bool HasCondition => ConditionExpression != null && !ConditionExpression.IsEmptyOrWhitespace;

        public bool DeactivatedByCondition { get; set; }

        public virtual Action Substitute(Record record)
        {
            var result = (Action)MemberwiseClone();
            if (HasCondition)
            {
                result.DeactivatedByCondition = !EvaluateCondition(ConditionExpression, record);
            }
            return result;
        }
        
        static readonly Regex placeholderPattern = new Regex(@"\$\((?<name>[^\)]+)\)");

        protected static string Substitute(string s, Record record)
            => placeholderPattern.Replace(s,
                m => record.TryGetValue(m.Groups["name"].Value, out var value)
                    ? value.StringValue
                    : throw new SubstitutionException(s, m.Groups["name"].Value));

        static readonly Regex simpleConditionPattern = new Regex(@"^\$\((?<name>[^\)]+)\)$");

        static readonly Regex falseConditionPattern = new Regex(@"^\s*(?:false|no|falsch|nein|0+([.,]0*)?|[.,]0+)?\s*$", RegexOptions.IgnoreCase);

        protected static bool EvaluateCondition(CellValue condExpr, Record record)
        {
            if (condExpr.BooleanValue.HasValue)
            {
                if (condExpr.BooleanValue == false) return false;
            }
            else if (condExpr.NumericValue.HasValue)
            {
                if (condExpr.NumericValue == 0) return false;
            }
            else
            {
                var simpleMatch = simpleConditionPattern.Match(condExpr.StringValue);
                if (simpleMatch.Success)
                {
                    var columnName = simpleMatch.Groups["name"].Value;
                    if (record.TryGetValue(columnName, out var cellValue))
                    {
                        if (cellValue.BooleanValue.HasValue && cellValue.BooleanValue == false ||
                            cellValue.NumericValue.HasValue && cellValue.NumericValue == 0 ||
                            cellValue.IsEmptyOrWhitespace)
                        {
                            return false;
                        }
                    }
                    else
                        throw new SubstitutionException(condExpr.StringValue, columnName);
                }
                else
                {
                    var substitutedCondition = Substitute(condExpr.StringValue, record);
                    if (falseConditionPattern.IsMatch(substitutedCondition))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
    
    [Serializable]
    public class SubstitutionException : Exception
    {
        public string Expression { get; }
        public string ColumnName { get; }

        public SubstitutionException(string expression, string columnName)
            : base($"Column name '{columnName}' from expression '{expression}' not found.")
        {
            Expression = expression;
            ColumnName = columnName;
        }

        protected SubstitutionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    class PauseAction : Action
    {
        public string Label { get; set; } = "Waiting for user to proceed";

        public override string ToString() => $"Pause: {Label}";

        public override Action Substitute(Record record)
        {
            var result = (PauseAction)base.Substitute(record);
            result.Label = Substitute(Label, record);
            return result;
        }
    }

    class NavigateAction : Action
    {
        public string Url { get; set; }

        public int Timeout { get; set; } = 5000;

        public override string ToString() => $"Navigate: {Url}";

        public override Action Substitute(Record record)
        {
            var result = (NavigateAction)base.Substitute(record);
            result.Url = Substitute(Url, record);
            return result;
        }
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

        public override Action Substitute(Record record)
        {
            var result = (TargetedAction)base.Substitute(record);
            result.Selector = Substitute(Selector, record);
            return result;
        }
    }

    class CheckTextAction : TargetedAction
    {
        public string Text { get; set; }

        public override string ToString() => $"CheckText: {Selector} for '{Text}' (Timeout = {Timeout}ms)";

        public override Action Substitute(Record record)
        {
            var result = (CheckTextAction)base.Substitute(record);
            result.Text = Substitute(Text, record);
            return result;
        }
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

        public override Action Substitute(Record record)
        {
            var result = (InputAction)base.Substitute(record);
            result.Value = Substitute(Value, record);
            return result;
        }
    }

    class FormAction : TargetedAction
    {
        public List<FormField> Inputs { get; set; } = new List<FormField>();

        public override string ToString() => $"Form: {Selector} (Timeout = {Timeout}ms)\n\t{string.Join("\n\t", Inputs)}";

        public override Action Substitute(Record record)
        {
            var result = (FormAction)base.Substitute(record);
            result.Inputs = Inputs.Select(f => new FormField
            {
                Name = Substitute(f.Name, record),
                Value = Substitute(f.Value, record),
                DeactivatedByCondition = f.HasCondition && !EvaluateCondition(f.ConditionExpression, record),
            }).ToList();
            return result;
        }
    }

    class FormField
    {
        public CellValue ConditionExpression { get; set; }

        public bool HasCondition => ConditionExpression != null && !ConditionExpression.IsEmptyOrWhitespace;

        public bool DeactivatedByCondition { get; set; }

        public string Name { get; set; }

        public string Value { set; get; }

        public override string ToString() => $"Set: {Name} = \"{Value}\"";
    }

    class Record : Dictionary<string, CellValue> 
    {
    }

    class CellValue
    {
        public string StringValue { get; set; }

        public bool? BooleanValue { get; set; }

        public double? NumericValue { get; set; }

        public bool IsEmpty => !BooleanValue.HasValue && !NumericValue.HasValue && string.IsNullOrEmpty(StringValue);

        public bool IsEmptyOrWhitespace => IsEmpty || string.IsNullOrWhiteSpace(StringValue);

        public override string ToString() => StringValue;

        public object GetValue() => BooleanValue ?? NumericValue ?? (object)StringValue;
    }
}
