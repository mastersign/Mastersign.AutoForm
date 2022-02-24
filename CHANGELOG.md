# Changelog

All notable changes to this project will be documented in this file.

This project follows the rules of
[Semantic Versioning](http://semver.org/).

<!--
This document follows the guidelines in http://keepachangelog.md.

Use the following change groups: Added, Changed, Deprecated, Removed, Fixed, Security
Add a link to the GitHub diff like
[<this-version>]: https://github.com/mastersign/Mastersign.AutoForm/compare/v<last-version>...v<this-version>
-->

## [Unreleased]

[Unreleased]: https://github.com/mastersign/Mastersign.AutoForm/compare/main...dev

### Added
* Support for conditional actions
* Substitution check when loading an Excel file

## [1.0.0] - 2022-02-23

[1.0.0]: https://github.com/mastersign/Mastersign.AutoForm/tree/v1.0.0

Initial release

### Added
* Template Excel file as a starting point
* Opening and reloading an Excel file as AutoForm script
* Navigating through records with a preview on the _Current Record_ tab
* Actions
    + Pause
    + Delay
    + Navigate
    + WaitFor
    + CheckText
    + Click
    + Input
    + Form
* Skip all pause actions
* Play only for the current record or for all
