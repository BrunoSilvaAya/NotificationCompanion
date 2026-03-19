# NotificationCompanion

NotificationCompanion is a local Blazor Server utility for browsing Razor email templates, inspecting the fields they expect, auto-generating sample values, and rendering HTML previews before those templates are used by the main application stack.

## What the app does

- Loads email templates from a configurable repository folder.
- Lists available `.cshtml` templates and lets you filter them by path.
- Parses each template for `Model.Property` references, `@if` checks, and `@foreach` collections.
- Infers field types such as text, email, URL, integer, decimal, boolean, date, datetime, JSON, and collections.
- Generates sample values for empty fields so templates can be previewed quickly.
- Renders the selected template with RazorLight and shows the result in an embedded preview frame.

## What it is for

This app is meant to help developers inspect and validate notification templates outside the main product flow. It is useful when you need to:

- understand what data a template expects,
- preview the final HTML without sending a real email,
- test conditional branches and collections with sample data,
- review template changes before they are wired into the main system.

## Current scope and limitations

- The app currently scans only `.cshtml` templates for previewing.
- SMTP settings exist in the UI, but email sending is not implemented yet.
- Some model schemas are explicitly defined for known DTO types, but most fields are inferred directly from the template source.
- The preview uses dynamic sample data, so it is intended for local validation rather than exact production execution.

## Required template source

The app depends on a local copy of the email template repository content.

The user has to copy the `EmailTemplates` folder from the monorepo `Applications\BlobStorageFiles\Private Azure Blob Storage`.

Place that copied folder so this repository can resolve it as:

```text
NotificationCompanion/
  EmailTemplates/
```

If the folder is not present in the workspace root, you can point the app at another repository root from the Settings page.

## How it works

### 1. App startup

The app is an ASP.NET Core Blazor Server application targeting `.NET 10`.

- `Program.cs` registers the configuration, template discovery, schema, and rendering services.
- Razor components run in interactive server mode.

### 2. Template discovery

`AppConfigService` determines where templates live.

- If a repository root path is saved in settings, that path is used.
- Otherwise, the app tries to detect the workspace root by walking upward until it finds either `NotificationCompanion.sln` or an `EmailTemplates` folder.
- The default template subfolder is `EmailTemplates`.

The selected configuration is saved to:

```text
%AppData%\EmailCompanion\config.json
```

### 3. Template list

`TemplateFileService` scans the configured template folder recursively and loads `.cshtml` files.

For each template it captures:

- file name,
- relative path,
- full path,
- declared `@model` type, if present,
- last modified timestamp,
- file size.

### 4. Field extraction

`TemplateParserService` parses the template source and extracts:

- direct model references like `Model.Name`,
- conditional references used in `@if`,
- collection references used in `@foreach`.

These references are normalized into field paths such as `Facility.Name` and surfaced in the form UI.

### 5. Field typing and sample data

`TemplateSchemaService` merges parsed fields with any known schema definitions.

When a field is not explicitly defined, the app infers a type from the field name and usage. For example:

- `Email` becomes an email field,
- `Url` or `Link` becomes a URL field,
- `Id` becomes an integer field,
- `Is...` or `Has...` becomes a boolean field,
- `Date` or `Time` becomes a date or datetime field.

The same service also provides placeholders and default sample values for fast previewing.

### 6. Preview rendering

`TemplateRenderService` reads the selected template, removes the `@model` directive, builds a dynamic object from the entered field values, and renders the template with RazorLight.

Important implementation details:

- `PreserveCompilationContext=true` is required so RazorLight can compile templates at runtime.
- `Newtonsoft.Json` is referenced because external templates may rely on `JObject` or `JToken` usage.
- Field values are coerced into appropriate runtime types where possible, including booleans, numbers, dates, JSON objects, and arrays.

## UI overview

### Templates page

The home page provides three working areas:

- a searchable template list,
- a fields/source panel for the selected template,
- a live preview panel.

From this page you can:

- select a template,
- inspect the raw source,
- fill or auto-fill extracted fields,
- render the preview,
- clear the form and re-test the template.

### Settings page

The settings page lets you configure:

- repository root path,
- template subfolder.

It also shows the computed full template path and whether that path exists.

SMTP settings are present as placeholders for a future version, but they are currently disabled.

## Running the app

### Prerequisites

- .NET 10 SDK
- a local copy of the `EmailTemplates` folder from the monorepo source described above

### Start from the repository root

```powershell
dotnet run --project .\EmailCompanion\EmailCompanion.csproj
```

Or run it from the project folder:

```powershell
cd .\EmailCompanion
dotnet run
```

Then open the local URL shown by ASP.NET Core in your browser.

## Typical workflow

1. Copy the `EmailTemplates` folder from `Applications\BlobStorageFiles\Private Azure Blob Storage` into this repository, or configure the external repository root in Settings.
2. Start the app.
3. Open Settings and confirm the resolved template path is marked as found.
4. Open Templates.
5. Select a `.cshtml` template.
6. Auto-fill sample data or enter values manually.
7. Click Preview and inspect the rendered HTML.

## Project structure

```text
NotificationCompanion.sln
EmailCompanion/          Blazor Server app
EmailTemplates/          Local template source consumed by the app
```

## Future direction already hinted in the code

- SMTP-backed sending support
- broader schema coverage for known template models
- continued support for external template repositories and richer preview data