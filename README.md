# Mastersign AutoForm

> Automate filling web forms by describing actions and data in an Excel file

## Requirements

* .NET Framework &ge; 7.2.1 (is shipped with Windows since Windows 10 1803)
* Google Chrome Browser

## How to install

Download and execute the MSI file from the [release](https://github.com/mastersign/Mastersign.AutoForm/releases) page.

## Getting started

* Start _Mastersign AutoForm_ by choosing "Mastersign AutoForm" &rarr; "AutoForm" from the start menu.
* _AutoForm_ immediately opens a new Google Chrome window.
  You can move it to a position on your screen,
  where it does not overlap with the _AutoForm_ window or close it for now.
* Save the template file with the button _Save Template_ in the _AutoForm_ window.
* Open the template file with _Excel_
	+ Adapt the _Script_ sheet
		- Change the name of the script (row 5)
		- Change the description of the script (row 6)
		- Specify the size of the visible area in the browser (row 7 and 8)
		- Describe the actions (row 12 and following)
	+ Put your data records in the _Records_ sheet, or delete the existing template rows there
		- The first row must contain the column names
		- Empty rows are ignored
	+ You can find an overview of the supported automation steps in the sheet _ActionTypes_
	+ Save the modified file with a descriptive name
* Open the modified Excel file in _AutoForm_ by clicking on the _Open_ button
* If _AutoForm_ prints out some error messages, fix them in the Excel file and click on _Reload_
* If you put data records in the _Records_ sheet, you can preview them on the _Current Record_ tab
* Click on _Play_ to  run the automation

## Hints

* Use placeholders `$(...)` in action parameters to reference columns in your data records.
* You can hit the _Reload_ button, immediately after making changes in the Excel file and saving it.
  No need to close the Excel file.
* Use the _Only current_ checkbox to run the automation only for the current record,
  instead of iterating through all records.
* Use the _No pauses_ checkbox to automatically skip all _Pause_ actions in the script.
* Suspend individual actions (temporarily) by entering `Skip` in column A.

## Supported Actions

Some actions use a _Selector_ parameter to identify an HTML element on the page.
This is a CSS selector.
You can visit one of the many sites around CSS and web design, to learn about them.

E. g. <https://www.w3schools.com/cssref/css_selectors.asp> or,
more formal <https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_Selectors>

### Pause
_Interrupt the script execution and wait for the user to proceed._

* Parameter 1: Label  
  The message to display when pausing the automation

### Delay
_Wait the given milliseconds._

* Parameter 1: Duration  
  The number of milliseconds to wait

### Navigate
_Navigate to the given URL. Cancel if timeout in milliseconds is reached._

* Parameter 1: Url  
  The URL to navigate to
* Parameter 2: Timeout (_optional_, default: 500)  
  The maximum number of milliseconds to wait for the page to load

### WaitFor
_Wait for an element to be present and visible/hidden. Cancel if timeout in milliseconds is reached._

* Parameter 1: Selector  
  The CSS selector for the element in the HTML document tree
* Parameter 2: Visible (_optional_, default: yes)  
  `yes` to wait for the element to be visible, `no` to wait until the element is not visible
* Parameter 3: Timeout (_optional_, default: 500)  
  The maximum number of milliseconds to wait

### Click
_Wait for an element and click on it. Cancel if timeout in milliseconds is reached._

* Parameter 1: Selector  
  The CSS selector for the element in the HTML document tree
* Parameter 2: Timeout (_optional_, default: 500)  
  The maximum number of milliseconds to wait

### CheckText
_Wait for an element and check if it contains the given text. Cancel if timeout in milliseconds is reached._

* Parameter 1: Selector  
  The CSS selector for the element in the HTML document tree
* Parameter 2: Text  
  The text to expect as _inner text_ in the HTML element
* Parameter 3: Timeout (_optional_, default: 500)  
  The maximum number of milliseconds to wait

### Input
_Wait for an input element and set its value. Cancel if timeout in milliseconds is reached._

* Parameter 1: Selector  
  The CSS selector for the `input`, `select`, or `textarea` element in the HTML document tree
* Parameter 2: Value  
  The value to enter into the element. In case of `select` the _value_ of an option is required, not the caption.
* Parameter 3: Timeout (_optional_, default: 500)  
  The maximum number of milliseconds to wait for the element

### Form
_Wait for a form and fill its fields. Cancel if timeout in milliseconds is reached._

* Parameter 1: Selector  
  The CSS selector for the `form` element in the HTML document tree
* Parameter 2: Timeout (_optional_, default: 500)  
  The maximum number of milliseconds to wait for the element

The following lines below the `Form` action contain the actual form fields.
With the `name` of the field in Parameter 1 and the value in Parameter 2.

## License

This project is published under the MIT license.
