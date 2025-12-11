# Documentation Update Plan: Observability & Virtual Environments

## Goal
Update `complex-comparison/README.md` to highlight new Observability features and Virtual Environment management capabilities in .NET Aspire.

## Proposed Changes

### 1. Update "Python Support" Section
**Current:** Focuses on first-class citizenship and inner loop speed.
**Update:** Add a paragraph explaining that `AddPythonApp` automatically manages the virtual environment (creation and `requirements.txt` installation), simplifying the "Clone & Run" experience compared to manual venv setup or Docker builds.

### 2. Add "Observability" Section
**Location:** Insert after "Python Support" and before "Parameters vs. Environment Variables".
**Content:**
*   Mention the addition of OpenTelemetry to the Python service.
*   Highlight that Aspire *automatically* injects the `OTEL_EXPORTER_OTLP_ENDPOINT` configuration.
*   Contrast with Docker Compose, which requires manual OTel collector and network configuration.

### 3. Update Comparison Table
Refine the "Observability" and "Developer Experience" rows to be more specific based on the new content.

| Feature | Docker Compose | .NET Aspire |
| :--- | :--- | :--- |
| **Observability** | Manual OTel wiring / Disparate tools | Auto-injected OTel / Integrated Dashboard |
| **Developer Experience** | Manual venv/container setup | Auto-managed venv & dependencies |
