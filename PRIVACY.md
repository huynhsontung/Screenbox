# Screenbox - Privacy Policy

## Telemetry
Screenbox only collects exception and diagnostic details such as log messages, exception messages, and stack traces. When a crash or error occurs, an anonymized incident report is sent to [Sentry](https://sentry.io/) through the `Microsoft.Extensions.Logging` pipeline. This report does not contain any personally identifiable information. However, it does include your machine model name, Windows version, and Windows language. Screenbox does not collect usage behaviors and does not track your in-app activities.

Telemetry is used to assess Screenbox's adaptation and identify programming issues, guiding the development process. This data is not used for marketing or sales. This data is not sent to any third party.

During local debugging builds, logs may also be written to the Visual Studio debug output to help diagnose development issues.

Source code of the telemetry service is public and can be viewed from the project's repository.