using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mastersign.AutoForm
{
    class AutomationProjectFactory
    {
        public AutomationProject ParseExcelFile(string filename)
        {
            var wbs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var wb = new ClosedXML.Excel.XLWorkbook(wbs);
            var ap = new AutomationProject();

            try
            {
                if (!wb.TryGetWorksheet("Script", out var ws)) {
                    ap.Errors.Add("Could not find worksheet 'Script'.");
                    return ap;
                }

                var rowLimit = ws.LastRowUsed().RowNumber();

                List<Action> actions = null;
                FormAction currentFormAction = null;

                for (var row = 1; row <= rowLimit; row++)
                {
                    var sectionCell = ws.Cell(row, 1);
                    var section = sectionCell.GetValue<string>();
                    var skip = false;
                    switch (section.ToLowerInvariant())
                    {
                        case "name":
                            actions = null;
                            var nameCell = ws.Cell(row, 2);
                            ap.Name = nameCell.GetValue<string>();
                            break;
                        case "description":
                            actions = null;
                            var descriptionCell = ws.Cell(row, 2);
                            ap.Description = descriptionCell.GetValue<string>();
                            break;
                        case "viewport width":
                        case "width":
                            actions = null;
                            var viewportWidthCell = ws.Cell(row, 2);
                            if (viewportWidthCell.TryGetValue(out int viewportWidth))
                                ap.ViewportWidth = viewportWidth;
                            else
                                ap.ViewportWidth = null;
                            break;
                        case "viewport height":
                        case "height":
                            actions = null;
                            var viewportHeightCell = ws.Cell(row, 2);
                            if (viewportHeightCell.TryGetValue(out int viewportHeight))
                                ap.ViewportHeight = viewportHeight;
                            else
                                ap.ViewportHeight = null;
                            break;
                        case "actions":
                            actions = ap.Actions;
                            break;
                        case "skip":
                            skip = true;
                            break;
                        default:
                            // ignore
                            break;
                    }
                    if (actions != null)
                    {
                        Action newAction = null;
                        var actionTypeCell = ws.Cell(row, 2);
                        var actionType = actionTypeCell.GetValue<string>();
                        var p1Cell = ws.Cell(row, 3);
                        var p2Cell = ws.Cell(row, 4);
                        var p3Cell = ws.Cell(row, 5);
                        switch (actionType)
                        {
                            case null:
                            case "":
                                if (currentFormAction != null)
                                {
                                    if (p1Cell.TryGetValue(out string fieldName) &&
                                        p2Cell.TryGetValue(out string fieldValue) &&
                                        !string.IsNullOrWhiteSpace(fieldName))
                                    {
                                        currentFormAction.Inputs.Add(new FormField
                                        {
                                            Name = fieldName,
                                            Value = fieldValue,
                                        });
                                    }
                                }
                                break;
                            case "Pause":
                                currentFormAction = null;
                                var pauseAction = new PauseAction();
                                if (p1Cell.TryGetValue(out string pauseLabel) &&
                                    !string.IsNullOrWhiteSpace(pauseLabel))
                                    pauseAction.Label = pauseLabel;
                                newAction = pauseAction;
                                break;
                            case "Delay":
                                currentFormAction = null;
                                if (p1Cell.TryGetValue(out int duration))
                                    newAction = new DelayAction { Duration = duration };
                                else
                                    ap.Errors.Add($"No valid duration for {actionType} action in row {row}");
                                break;
                            case "Navigate":
                                currentFormAction = null;
                                if (p1Cell.TryGetValue(out string uriStr) &&
                                    Uri.TryCreate(uriStr, UriKind.Absolute, out var uri))
                                {
                                    var navigateAction = new NavigateAction { Url = uri.ToString() };
                                    if (p2Cell.TryGetValue(out int timeout))
                                        navigateAction.Timeout = timeout;
                                    newAction = navigateAction;
                                }
                                else
                                    ap.Errors.Add($"No valid URL for {actionType} action in row {row}");
                                break;
                            case "WaitFor":
                                currentFormAction = null;
                                if (p1Cell.TryGetValue(out string waitForSelector) &&
                                    !string.IsNullOrWhiteSpace(waitForSelector))
                                {
                                    var waitForAction = new WaitForAction { Selector = waitForSelector };
                                    if (p2Cell.TryGetValue(out bool visibile))
                                        waitForAction.Visible = visibile;
                                    if (p3Cell.TryGetValue(out int timeout))
                                        waitForAction.Timeout = timeout;
                                    newAction = waitForAction;
                                }
                                else
                                    ap.Errors.Add($"No selector given for {actionType} action in row {row}");
                                break;
                            case "Click":
                                currentFormAction = null;
                                if (p1Cell.TryGetValue(out string clickSelector) &&
                                    !string.IsNullOrWhiteSpace(clickSelector))
                                {
                                    var clickAction = new ClickAction { Selector = clickSelector };
                                    if (p2Cell.TryGetValue(out int timeout))
                                        clickAction.Timeout = timeout;
                                    newAction = clickAction;
                                }
                                else
                                    ap.Errors.Add($"No selector given for {actionType} action in row {row}");
                                break;
                            case "CheckText":
                                currentFormAction = null;
                                if (p1Cell.TryGetValue(out string checkTextSelector) &&
                                    !string.IsNullOrWhiteSpace(checkTextSelector))
                                {
                                    if (p2Cell.TryGetValue(out string checkTextText) &&
                                        !string.IsNullOrEmpty(checkTextText))
                                    {
                                        var checkTextAction = new CheckTextAction
                                        {
                                            Selector = checkTextSelector,
                                            Text = checkTextText,
                                        };
                                        if (p3Cell.TryGetValue(out int timeout))
                                            checkTextAction.Timeout = timeout;
                                        newAction = checkTextAction;
                                    }
                                    else
                                        ap.Errors.Add($"No text given for {actionType} action in row {row}");
                                }
                                else
                                    ap.Errors.Add($"No selector given for {actionType} action in row {row}");
                                break;
                            case "Input":
                                currentFormAction = null;
                                if (p1Cell.TryGetValue(out string inputSelector) &&
                                    !string.IsNullOrWhiteSpace(inputSelector))
                                {
                                    var inputAction = new InputAction
                                    {
                                        Selector = inputSelector,
                                        Value = p2Cell.TryGetValue(out string inputValue) ? inputValue : string.Empty,
                                    };
                                    if (p3Cell.TryGetValue(out int timeout))
                                        inputAction.Timeout = timeout;
                                    newAction = inputAction;
                                }
                                else
                                    ap.Errors.Add($"No selector given for {actionType} action in row {row}");
                                break;
                            case "Form":
                                if (p1Cell.TryGetValue(out string formSelector) &&
                                    !string.IsNullOrWhiteSpace(formSelector))
                                {
                                    var formAction = new FormAction { Selector = formSelector };
                                    if (p2Cell.TryGetValue(out int formTimeout))
                                        formAction.Timeout = formTimeout;
                                    newAction = formAction;
                                    currentFormAction = formAction;
                                }
                                else
                                    ap.Errors.Add($"No selector given for {actionType} action in row {row}");
                                break;
                            default:
                                // ignore
                                break;
                        }
                        if (newAction != null)
                        {
                            if (skip)
                                ap.SkippedActions++;
                            else
                                actions.Add(newAction);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ap.Errors.Add(ex.Message);
            }
            finally
            {
                wb.Dispose();
                wbs.Dispose();
            }
            return ap;
        }
    }
}
