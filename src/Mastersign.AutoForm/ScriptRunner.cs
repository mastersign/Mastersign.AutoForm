using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using PuppeteerSharp;

namespace Mastersign.AutoForm
{
    internal class ScriptRunner : IDisposable
    {
        Browser browser;
        Page page;

        public bool IsReady => page != null && !page.IsClosed;

        public IEnumerable<string> GetPotentialExecutablePathsForChrome()
        {
            var userUninstallLocation = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\Google Chrome", "InstallLocation", null) as string;
            if (userUninstallLocation != null) yield return userUninstallLocation;
            var userAppPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe", "", null) as string;
            if (userAppPath != null) yield return userAppPath;
            var machineUninstallLocation = Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Uninstall\Google Chrome", "InstallLocation", null) as string;
            if (machineUninstallLocation != null) yield return machineUninstallLocation;
            var machineAppPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe", "", null) as string;
            if (machineAppPath != null) yield return machineAppPath;
            yield return Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Google\Chrome\Application\chrome.exe");
            yield return Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe");
        }

        //public IEnumerable<string> GetPotentialExecutablePathForFirefox()
        //{
        //    var userAppPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe", "", null) as string;
        //    if (userAppPath != null) yield return userAppPath;
        //    var userVersion = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Mozilla\Mozilla Firefox", "CurrentVersion", null) as string;
        //    if (userVersion != null)
        //    {
        //        var userLocation = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Mozilla\Mozilla Firefox\" + userVersion, "PathToExe", null) as string;
        //        if (userLocation != null) yield return userLocation;
        //    }
        //    var machineVersion = Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Mozilla\Mozilla Firefox", "CurrentVersion", null) as string;
        //    if (machineVersion != null)
        //    {
        //        var machineLocation = Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Mozilla\Mozilla Firefox\" + machineVersion, "PathToExe", null) as string;
        //        if (machineLocation != null) yield return machineLocation;
        //    }
        //    yield return Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Mozilla Firefox\firefox.exe");
        //    yield return Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Mozilla Firefox\firefox.exe");
        //}

        public string FindExecutable(Product product)
        {
            switch (product)
            {
                case Product.Chrome:
                    return GetPotentialExecutablePathsForChrome().FirstOrDefault(p => File.Exists(p));
                //case Product.Firefox:
                //    return GetPotentialExecutablePathForFirefox().FirstOrDefault(p => File.Exists(p));
                default:
                    throw new NotSupportedException();
            }
        }

        public async Task Initialize(AutomationProject project)
        {
            var screenWidth = (int)System.Windows.SystemParameters.WorkArea.Width;
            var screenHeight = (int)System.Windows.SystemParameters.WorkArea.Height;
            var defaultViewportWidth = Math.Min(screenWidth - 20, Math.Max(1200, screenWidth / 2));
            var defaultViewportHeight = screenHeight - 180;

            if (browser == null)
            {
                // select browser, prioritize Chrome
                var chromePath = FindExecutable(Product.Chrome);
                //var firefoxPath = FindExecutable(Product.Firefox);
                //var product = chromePath == null && firefoxPath != null ? Product.Firefox : Product.Chrome;
                var executablePath = chromePath;
                //if (product == Product.Firefox) executablePath = firefoxPath;
                if (executablePath == null) throw new NotSupportedException("Google Chrome is not installed.");

                browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = false,
                    //Product = product,
                    Product = Product.Chrome,
                    ExecutablePath = executablePath,
                    DefaultViewport = new ViewPortOptions
                    {
                        Width = defaultViewportWidth,
                        Height = defaultViewportHeight,
                        IsLandscape = true,
                        HasTouch = false,
                        IsMobile = false,
                        DeviceScaleFactor = 1.0,
                    },
                });
                browser.Closed += (o, e) => browser = null;
            }
            if (project != null && project.CleanBrowser)
            {
                page = await browser.NewPageAsync();
                var pages = await browser.PagesAsync();
                foreach (var p in pages)
                {
                    if (p != page) await p.CloseAsync();
                }
            }
            else
            {
                page = (await browser.PagesAsync()).FirstOrDefault();
                if (page == null)
                {
                    page = await browser.NewPageAsync();
                }
            }
            if (project != null)
            {
                await page.SetViewportAsync(new ViewPortOptions
                {
                    Width = project.ViewportWidth ?? defaultViewportWidth,
                    Height = project.ViewportHeight ?? defaultViewportHeight,
                });
            }
        }

        public void Dispose()
        {
            if (browser != null) browser.Dispose();
        }

        public Task Run(Action action)
        {
            if (!IsReady) throw new InvalidOperationException("Browser was closed.");
            if (action is DelayAction delayAction) return RunDelay(delayAction);
            if (page == null) throw new Exception("Page is closed");
            if (action is NavigateAction navigateAction) return RunNavigate(navigateAction);
            if (action is ReloadAction reloadAction) return RunReload(reloadAction);
            if (action is WaitForAction waitForAction) return RunWaitFor(waitForAction);
            if (action is ClickAction clickAction) return RunClick(clickAction);
            if (action is CheckTextAction checkTextAction) return RunCheckText(checkTextAction);
            if (action is InputAction inputAction) return RunInputAction(inputAction);
            if (action is FormAction formAction) return RunFormAction(formAction);
            throw new NotSupportedException();
        }

        private Task RunDelay(DelayAction a) => Task.Delay(a.Duration);

        private Task RunNavigate(NavigateAction a)
            => page.GoToAsync(a.Url, a.Timeout, new[] {
                WaitUntilNavigation.DOMContentLoaded,
                WaitUntilNavigation.Load,
            });

        private Task RunReload(ReloadAction a)
            => page.ReloadAsync(a.Timeout, new[] {
                WaitUntilNavigation.DOMContentLoaded,
                WaitUntilNavigation.Load,
            });

        private Task RunWaitFor(WaitForAction a)
            => page.WaitForSelectorAsync(a.Selector, new WaitForSelectorOptions
            {
                Timeout = a.Timeout,
                Visible = a.Visible,
            });

        private async Task RunClick(ClickAction a)
        {
            var e = await page.WaitForSelectorAsync(a.Selector, new WaitForSelectorOptions
            {
                Timeout = a.Timeout,
                Visible = true,
            });
            if (e == null) throw new ElementNotFoundException(a.Selector);
            await e.ClickAsync();
        }

        private async Task RunCheckText(CheckTextAction a)
        {
            var e = await page.WaitForSelectorAsync(a.Selector, new WaitForSelectorOptions
            {
                Timeout = a.Timeout,
                Visible = true,
            });
            if (e == null) throw new ElementNotFoundException(a.Selector);
            var text = await e.EvaluateFunctionAsync<string>("e => e.innerText");
            if (string.IsNullOrWhiteSpace(text) || !text.Contains(a.Text))
            {
                throw new CheckTextFailedException(a.Text, text);
            }
        }

        private async Task SetInputValue(ElementHandle e, string value)
        {

            if (await e.EvaluateFunctionAsync<bool>("e => e.disabled")) return;

            var tagName = (await e.EvaluateFunctionAsync<string>("e => e.tagName")).ToUpperInvariant();
            if (tagName == "SELECT")
            {
                await e.FocusAsync();
                await e.SelectAsync(value);
            }
            else if (tagName == "INPUT")
            {
                var inputType = await e.EvaluateFunctionAsync<string>("e => e.type");
                if (inputType == "checkbox")
                {
                    var boolValue = (new[] { "1", "y", "true", "yes", "wahr", "ja" }).Contains(value.ToLowerInvariant());
                    var isChecked = await e.EvaluateFunctionAsync<bool>("e => e.checked");
                    if (boolValue && !isChecked || !boolValue && isChecked)
                    {
                        await e.ClickAsync();
                    }
                }
                else if (inputType == "radio")
                {
                    throw new NotSupportedException("Radio buttons can not be set with Input action. Use the Form action.");
                }
                else
                {
                    await e.FocusAsync();
                    await e.EvaluateFunctionAsync("e => { e.value = ''; }");
                    await e.TypeAsync(value);
                }
            }
            else if (tagName == "TEXTAREA")
            {
                await e.FocusAsync();
                await e.EvaluateFunctionAsync("e => { e.value = ''; }");
                await e.TypeAsync(value);
            }
            else
            {
                throw new NotSupportedException($"Setting value of element type {tagName} is not supported.");
            }
        }

        private async Task RunInputAction(InputAction a)
        {
            var e = await page.WaitForSelectorAsync(a.Selector, new WaitForSelectorOptions
            {
                Timeout = a.Timeout,
                Visible = true,
            });
            if (e == null) throw new ElementNotFoundException(a.Selector);
            await SetInputValue(e, a.Value);
        }

        private async Task RunFormAction(FormAction a)
        {
            var form = await page.WaitForSelectorAsync(a.Selector, new WaitForSelectorOptions
            {
                Timeout = a.Timeout,
            });
            if (form == null) throw new ElementNotFoundException(a.Selector);

            foreach (var f in a.Inputs)
            {
                if (f.DeactivatedByCondition) continue;
                var elements = await form.QuerySelectorAllAsync(string.Join(", ", new[] {
                    $"input[name='{f.Name}']",
                    $"textarea[name='{f.Name}']",
                    $"select[name='{f.Name}']",
                }));
                if (elements.Length == 0) throw new FormInputNotFound(f.Name);
                if (elements.Length == 1)
                    await SetInputValue(elements[0], f.Value);
                else
                {
                    var isRadio = true;
                    ElementHandle radioElement = null;
                    foreach (var e in elements)
                    {
                        if ((await e.EvaluateFunctionAsync<string>("e => e.type")) != "radio")
                        {
                            isRadio = false;
                            break;
                        }
                        if ((await e.EvaluateFunctionAsync<string>("e => e.value")) == f.Value)
                        {
                            radioElement = e;
                        }
                    }
                    if (isRadio)
                    {
                        if (radioElement != null)
                        {
                            await radioElement.ClickAsync();
                        }
                        else
                            throw new RadioButtonNotFound(f.Name, f.Value);
                    }
                    else
                        throw new RadioGroupNotFound(f.Name);
                }
            }
        }
    }

    public class ElementNotFoundException : Exception
    {
        public ElementNotFoundException(string selector)
            : base($"Element not found: {selector}")
        { }
    }

    public class CheckTextFailedException : Exception
    {
        public CheckTextFailedException(string expected, string actual)
            : base($"Text '{expected}' was not found in '{actual}'.")
        { }
    }

    public class FormInputNotFound : Exception
    {
        public FormInputNotFound(string fieldname)
            : base($"Form input {fieldname} not found. Searched for INPUT, SELECT, and TEXTAREA.")
        { }
    }

    public class RadioGroupNotFound : Exception
    {
        public RadioGroupNotFound(string groupname)
            : base($"Radio group {groupname} not found.")
        { }
    }

    public class RadioButtonNotFound : Exception
    {
        public RadioButtonNotFound(string groupname, string value)
            : base($"Radio button with value '{value}' in group {groupname} not found.")
        { }
    }
}
