Company Planner CSV Exporter
===========================

IMPORTANT: Keep all files in this folder together
-------------------------------------------------
Do not move only the executable. The app needs these files side by side:
- CompanyPlannerCsvExporter (or CompanyPlannerCsvExporter.exe on Windows)
- appsettings.json
- .playwright/   (required Playwright driver folder)

Windows
-------
1. Unzip the full folder.
2. Double-click CompanyPlannerCsvExporter.exe
   (or run it from Command Prompt / PowerShell).
3. On first run, Chromium will download automatically (~250 MB).
4. Log in to Fenster when the browser opens.
5. Go to the subscription list page, then press ENTER in the terminal.
6. CSV files are written to the output folder next to the app.

macOS
-----
1. Unzip the full folder.
2. Open Terminal in the unzipped folder.
3. Run:
   xattr -cr .
   chmod +x CompanyPlannerCsvExporter
4. Run: ./CompanyPlannerCsvExporter

If macOS blocks the app ("Apple could not verify...")
----------------------------------------------------
This is normal for apps not distributed through the App Store.

Option A (easiest):
  Right-click CompanyPlannerCsvExporter -> Open -> click Open

Option B:
  System Settings -> Privacy & Security -> scroll down -> Open Anyway

Option C (Terminal, in the unzipped folder):
  xattr -cr .
  chmod +x CompanyPlannerCsvExporter
  ./CompanyPlannerCsvExporter

5. On first run, Chromium will download automatically (~250 MB).
6. Log in to Fenster when the browser opens.
7. Go to the subscription list page, then press ENTER in the terminal.
8. CSV files are written to the output folder next to the app.

Output files
------------
- output/exported-import.csv
- output/failed-records.csv
- output/export.log

Install browsers only (optional)
--------------------------------
Windows: CompanyPlannerCsvExporter.exe --install-browsers
macOS:   ./CompanyPlannerCsvExporter --install-browsers

Troubleshooting
---------------
"Driver not found":
  You are probably running only the executable without the .playwright folder.
  Unzip the full release zip again and run the app from inside that folder.
