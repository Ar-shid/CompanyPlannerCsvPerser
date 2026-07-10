# Company Planner CSV Exporter

Standalone .NET 8 console application that exports subscription data from Fenster into a CSV file matching the provided import template. The app uses **Microsoft Playwright** for browser automation (macOS and Windows) and **AngleSharp** for isolated HTML parsing.

## Project structure

```
CompanyPlannerCsvPerser/
├── samples/                          # Local MHTML + CSV template for development
│   ├── sample-import-template.csv
│   ├── subscription-list.mhtml
│   └── subscription-detail.mhtml
├── src/CompanyPlannerCsvPerser/
│   ├── Program.cs                      # Host setup, DI, logging
│   ├── appsettings.json                # URLs, selectors, mode, paths
│   ├── Configuration/                  # Strongly typed options
│   ├── Models/                         # Export/list/detail models
│   ├── Services/ExportService.cs       # Orchestrates the export workflow
│   ├── BrowserAutomation/              # Playwright wrapper
│   ├── HtmlParser/                     # AngleSharp parsing (testable)
│   ├── CsvExporter/                    # CSV output via CsvHelper
│   └── Mhtml/                          # MHTML → HTML loader for local dev
└── tests/CompanyPlannerCsvPerser.Tests/
    └── FensterHtmlParserTests.cs       # Parser tests against sample MHTML
```

### Workflow

1. Launch Chromium via Playwright
2. **LocalMhtml mode:** load saved list/detail MHTML snapshots
3. **Live mode:** open the site, wait for manual login, read the live list page
4. Parse every subscription row from the list
5. Open each detail page and extract form fields
6. Map fields to the CSV template (unknown fields left empty)
7. Write `exported-import.csv`, `failed-records.csv`, and an application log

Failed records are skipped and logged; processing continues for the rest.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- macOS or Windows

## Install Playwright browsers (required once)

Playwright downloads its own Chromium build. You must install it **after building** and again whenever the `Microsoft.Playwright` NuGet package version changes.

### From Rider or terminal

**Option A — automatic (recommended):** just run the app. If Chromium is missing, it will download automatically on first launch.

**Option B — manual install:**

```bash
dotnet run --project src/CompanyPlannerCsvPerser -- --install-browsers
```

**Option C — shell script:**

```bash
./install-playwright-browsers.sh
```

After the first build, install Chromium once:

### macOS / Linux

```bash
./install-playwright-browsers.sh
```

Or manually after building:

```bash
cd src/CompanyPlannerCsvPerser
dotnet build
node bin/Debug/net8.0/.playwright/package/cli.js install chromium
```

If PowerShell is installed:

```bash
pwsh bin/Debug/net8.0/playwright.ps1 install chromium
```

### Windows (PowerShell)

```powershell
cd src\CompanyPlannerCsvPerser
dotnet build
.\bin\Debug\net8.0\playwright.ps1 install chromium
```

## Run on macOS

From the repository root:

```bash
# Run parser unit tests (no browser required)
dotnet test

# Run export using local MHTML files (default mode)
dotnet run --project src/CompanyPlannerCsvPerser
```

Output is written to `./output/`:

- `exported-import.csv`
- `failed-records.csv`
- `export.log`

## Run on Windows

```powershell
dotnet test
dotnet run --project src\CompanyPlannerCsvPerser
```

Install browsers with `playwright.ps1` as shown above.

## Test with provided MHTML files

The repo includes saved snapshots in `samples/`:

| File | Source page |
|------|-------------|
| `subscription-list.mhtml` | `https://www.fenster.dk/subscription_list` |
| `subscription-detail.mhtml` | `https://www.fenster.dk/subscription_edit/405683` |

Default `appsettings.json` uses **LocalMhtml** mode:

```json
"Export": {
  "Mode": "LocalMhtml"
},
"LocalMhtml": {
  "ListPagePath": "./samples/subscription-list.mhtml",
  "DetailPagePath": "./samples/subscription-detail.mhtml",
  "DetailPageSubscriptionId": "405683"
}
```

Only subscription `405683` has a local detail snapshot. Other list rows are exported with list-page contact data; detail fields remain empty for those rows.

Parser tests run without a browser:

```bash
dotnet test
```

## Switch to the live website

When the client has access to the real system:

1. Open `src/CompanyPlannerCsvPerser/appsettings.json`
2. Change mode and browser settings:

```json
"Export": {
  "Mode": "Live"
},
"Browser": {
  "Headless": false,
  "WaitForManualLogin": true,
  "LoginUrl": "https://www.fenster.dk/",
  "ListPageUrl": "https://www.fenster.dk/subscription_list"
}
```

3. Run the app. Chromium opens.
4. Log in manually and navigate to the subscription list.
5. Press **Enter** in the terminal when the list page is visible.
6. The app visits each detail page and writes the CSV.

If the site HTML changes, update selectors under the `Selectors` section in `appsettings.json` — no code changes required for selector tweaks.

## Configuration reference

| Section | Purpose |
|---------|---------|
| `Export` | Mode, output paths, file names |
| `Browser` | Playwright launch options, login URLs, manual login prompt |
| `LocalMhtml` | Paths to saved MHTML files for offline development |
| `Selectors` | CSS selectors for list rows, detail form fields |

## Field mapping (initial)

| CSV column | Source (best effort) |
|------------|-------------------|
| `TitleForJob` | First task category on detail page |
| `Name`, `Email`, `PhoneNumber`, `SubscriptionStreet`, `SubscriptionCity` | List row customer column; billing textarea on detail when populated |
| `OutsideJob*` / `InsideJob*` / `ExterirorJob*` | Tasks by `semantic_meaning` (`WINDOW_CLEANING_OUTSIDE`, `WINDOW_CLEANING_INSIDE`, etc.) |
| `StartDate` | `#id_base_start_week` selected value |
| `StartTime` | `#id_fixed_time_of_day` |
| `EstimatedTime` | Sum of task durations |
| `CustomerNote` / `SubscriptionNote` | `#id_address_comment` |
| `FirstVisit*` | Task `staged` checkbox per job type |
| `FirstOrderDate` | Not mapped yet (left empty) |

Review `exported-import.csv` and adjust mappings in `FensterHtmlParser.MapToExportRecord` or selectors as needed.

## Test release package locally (before commit/release)

Run the same packaging flow as GitHub Actions on your Mac:

```bash
./scripts/package-and-test.sh osx-arm64
```

This will:
1. Publish a production build
2. Create a zip and unzip it (simulates client download)
3. Run `--validate-package` on the extracted app

If it passes, the package is safe to release. The test output is written to `artifacts/package-test-osx-arm64/unzipped/`.

You can also validate any unzipped client folder manually:

```bash
./CompanyPlannerCsvExporter --validate-package
```

## Troubleshooting

- **Playwright browser missing:** run `playwright.sh install chromium` (macOS) or `playwright.ps1 install chromium` (Windows).
- **Sample files not found:** run from the repository root so `./samples/...` resolves correctly.
- **Login timeout on live site:** increase `Browser.NavigationTimeoutMs` in `appsettings.json`.
