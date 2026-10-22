# Snipe-IT UI Automation

BDD end-to-end tests for the asset lifecycle in [Snipe-IT](https://snipeitapp.com), an open-source IT asset management system, running against the public demo at https://demo.snipeitapp.com.

**Stack:** C# / .NET 10 · [Playwright for .NET](https://playwright.dev/dotnet/) · [Reqnroll](https://reqnroll.net) (Gherkin BDD, the maintained successor to SpecFlow) · NUnit · GitHub Actions

---

## Quick start

The only prerequisite is the [.NET 10 SDK](https://dotnet.microsoft.com/download) (or later; see `global.json`).

```bash
git clone <this-repo-url>
cd <repo-folder>
dotnet test
```

That's all. The first run restores packages and downloads Chromium automatically (about a minute); later runs take about 40 seconds for the whole suite. No PowerShell or separate browser install step is needed.

Expected result:

```
Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2, Duration: 41 s - SnipeIt.Tests.dll (net10.0)
```

To watch each step as it runs, including which user the asset was checked out to:

```bash
dotnet test --logger "console;verbosity=detailed"
```

---

## What is tested

The scenarios are in [`src/SnipeIt.Tests/Features`](src/SnipeIt.Tests/Features) and describe the behaviour in business language.

### Happy path: create, check out and verify an asset

[`AssetLifecycle.feature`](src/SnipeIt.Tests/Features/AssetLifecycle.feature)

```gherkin
Scenario: A new asset checked out to a user is listed with correct details and history
  When I create an asset checked out to a random user with these details:
    | Field           | Value           |
    | Model           | Macbook Pro 13" |
    | Status          | Ready to Deploy |
    | Purchase Date   | 2026-01-15      |
    | Purchase Cost   | 899.50          |
    | Warranty Months | 24              |
  Then the asset appears in the assets list with its name, serial and assignee
  When I open the asset from the assets list
  Then the asset page shows the details I entered
  And the asset is deployed to the selected user
  When I open the asset's history
  Then the history shows the asset was created
  And the history shows exactly one checkout to the selected user
```

Each run also generates a unique **asset tag, name, serial, order number and notes**, and checks the asset out to a **random user** from the user dropdown. The checks verify the record end to end:

| Where | What is verified |
|---|---|
| Assets list | The row found by asset tag shows the name, serial and assignee |
| Asset page | Tag, name, serial, model, notes, order number, purchase date, purchase cost, and warranty expiry (purchase date + warranty months), each checked in its own field |
| Status | Shows *Ready to Deploy* with the *Deployed* label and a link to the selected user |
| History tab | A create entry, and exactly one checkout, to the selected user |

Filling in many fields with run-unique values makes sure the checks are looking at the record this run created, not one that merely looks similar.

### Negative path: duplicate asset tags are rejected

[`AssetValidation.feature`](src/SnipeIt.Tests/Features/AssetValidation.feature)

```gherkin
Scenario: An asset tag that is already in use is rejected
  Given an asset already exists with these details:
    | Field  | Value           |
    | Model  | Macbook Pro 13" |
    | Status | Ready to Deploy |
  When I try to create another asset with the same asset tag
  Then the asset tag is rejected with the error "The Asset Tag must be unique."
```

The scenario creates the asset it collides with instead of borrowing a seeded one, so it doesn't depend on the demo's data. It checks for the form's error banner, the field-level message, and the absence of a success message.

---

## How it is built

```
Feature files (Gherkin)        what the system should do, in business language
        │
Step definitions               thin glue: parse tables, call page objects, share scenario state
        │
Page objects + components      selectors, page behaviour and assertions, one class per page
        │
Playwright                     browser automation with auto-waiting, web-first assertions
```

```
src/SnipeIt.Tests/
  Features/     Gherkin scenarios
  Steps/        Step definitions (LoginSteps, AssetSteps)
  Pages/        Page objects: Login, CreateAsset, AssetList, AssetDetails
  Components/   Select2Dropdown: reusable driver for Snipe-IT's Select2 widgets
  Models/       AssetData: test data record with a unique-value factory
  Support/      Playwright lifecycle hooks, per-scenario state, helpers
  Config/       Typed settings from appsettings.json + environment variables
```

### Design decisions

- **BDD with thin steps.** Scenarios can be reviewed by testers, developers and product owners. Step definitions only translate Gherkin into page-object calls, so the BDD layer adds readability without duplicating logic.
- **Page Object Model.** Selectors and assertions live in one class per page, so a UI change is fixed in one place and steps read like the scenario.
- **Scenario state through dependency injection.** Reqnroll creates one `AssetScenarioState` per scenario and injects it into the step classes, so no static state leaks between scenarios. If a step runs before the step it depends on, it fails with a clear message.
- **Precise assertions.** Each value is checked in its own field (for example the serial in the info panel, the status in the status box) with exact-text matching, not by searching the whole page. A passing run means each field is right.
- **Web-first assertions only.** Every check uses Playwright's auto-retrying `Expect(...)`. There are no hard-coded sleeps.
- **Stable selectors.** Ids, `name` attributes and Snipe-IT's own value hooks (`js-copy-*` spans), plus role-based locators for buttons and links. No brittle CSS paths or positional XPath.
- **Isolation on a shared environment.** Every run creates its own uniquely named data, and the negative scenario creates its own collision, so runs never depend on or interfere with other people's data.
- **Debuggable failures.** A Playwright trace (screenshots, DOM snapshots, network) is saved and attached to the test result only when a scenario fails. Open one with:

  ```bash
  pwsh src/SnipeIt.Tests/bin/Debug/net10.0/playwright.ps1 show-trace <trace.zip>
  ```

  or drag the zip onto https://trace.playwright.dev.

---

## Configuration

Settings are in [`src/SnipeIt.Tests/appsettings.json`](src/SnipeIt.Tests/appsettings.json). Any of them can be overridden with an environment variable using a double underscore:

| Setting | Default | Example override |
|---|---|---|
| `BaseUrl` | `https://demo.snipeitapp.com` | `SnipeIt__BaseUrl=https://snipeit.example.com` |
| `Username` / `Password` | `admin` / `password` (the demo's public login) | `SnipeIt__Password=secret` |
| `Browser` | `chromium` | `SnipeIt__Browser=firefox` (or `webkit`) |
| `Headless` | `true` | `SnipeIt__Headless=false` to watch the browser |
| `DefaultTimeoutMs` / `ExpectTimeoutMs` | `30000` / `15000` | `SnipeIt__ExpectTimeoutMs=20000` |

```bash
# macOS / Linux
SnipeIt__Headless=false dotnet test

# Windows PowerShell
$env:SnipeIt__Headless = "false"; dotnet test

# Run a single feature
dotnet test --filter "FullyQualifiedName~AssetValidation"
```

The committed credentials are the demo's public ones. For any real environment, pass credentials through environment variables or CI secrets instead of committing them.

Test data (model, status, purchase details) lives in the feature files, next to the scenario that uses it.

---

## Continuous integration

[`.github/workflows/ui-tests.yml`](.github/workflows/ui-tests.yml) runs on every push to `main`, on every pull request, and on demand. It builds the solution, installs Chromium with its Linux system dependencies, runs the suite, and uploads the TRX results plus any failure traces as a build artifact.

---

## Assumptions and limitations

- **Shared public demo.** The demo is used by many people and is reset regularly, so created assets disappear after a reset. This is also why tests create unique data instead of cleaning up.
- **Seed data.** The scenarios rely on the demo's seeded *Macbook Pro 13"* model and *Ready to Deploy* status. If these change, update the tables in the feature files.
- **Serial runs.** Scenarios run one at a time (`test.runsettings`) to keep load on the shared demo predictable.
- **Random assignee.** The user is picked at random from the first page of the dropdown, to show the flow works for any user. The chosen user is written to the test output for traceability.

---

