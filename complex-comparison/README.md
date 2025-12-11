# Complex Comparison Project

## Project Overview
This project is a sample polyglot application designed to demonstrate and compare two different orchestration approaches: traditional Docker Compose and the new .NET Aspire.

The application consists of multiple services and infrastructure components:
- **Frontend**: A Node.js application serving the user interface.
- **Backend**: A .NET Web API handling business logic.
- **AI Service**: A Python service for AI-related tasks.
- **Infrastructure**:
  - **Redis**: For caching.
  - **PostgreSQL**: For persistent storage.
  - **RabbitMQ**: For message queuing.

## Folder Structure
This project uses a "Shared Source" strategy, where the application code is decoupled from the orchestration logic.

- **`src/`**: Contains the source code for all application services (Frontend, Backend, AI Service).
- **`docker/`**: Contains the Docker Compose configuration files for traditional container orchestration.
- **`aspire/`**: Contains the .NET Aspire AppHost project for code-first orchestration.

## How to Run with Docker Compose

To run the application using Docker Compose:

1.  Open a terminal and navigate to the `docker` directory:
    ```bash
    cd complex-comparison/docker
    ```
2.  Start the services:
    ```bash
    docker-compose up
    ```

**Note:** When using Docker Compose, you are responsible for manually managing port conflicts, environment variables, and viewing logs for each container separately.

## How to Run with .NET Aspire

To run the application using .NET Aspire:

1.  Open a terminal and navigate to the AppHost project directory:
    ```bash
    cd complex-comparison/aspire/ComplexApp.AppHost
    ```
2.  Run the application:
    ```bash
    dotnet run
    ```

**Benefits of .NET Aspire:**
- **Unified Dashboard**: View logs, traces, and metrics for all services in one place.
- **Service Discovery**: Automatic injection of connection strings and endpoints without manual wiring.
- **No YAML**: Orchestration is defined in C#, allowing for compile-time checks and refactoring support.

## Key Features

### Python Support
Aspire now treats Python as a first-class citizen with the `AddPythonApp` method. Unlike Docker Compose, where Python apps are just generic containers defined in YAML, Aspire allows you to orchestrate Python resources directly in C#. This enables native execution on the host machine, providing a significantly faster inner loop debugging experience compared to rebuilding containers.

Additionally, `AddPythonApp` automatically manages the Python virtual environment for you. It creates the environment if it's missing and installs dependencies from `requirements.txt`. This simplifies the "Clone & Run" experience, eliminating the need to manually set up virtual environments or build Docker images before starting development.

### Observability
We've added OpenTelemetry to the Python service to demonstrate Aspire's powerful observability features. Aspire *automatically* injects the OTLP endpoint configuration (`OTEL_EXPORTER_OTLP_ENDPOINT`) into the service, so no manual wiring is needed.

In contrast, achieving this with Docker Compose would typically require manually configuring an OpenTelemetry collector, setting up networking between containers, and managing environment variables for each service.

### Parameters vs. Environment Variables
In Docker Compose, secrets and configuration often end up hardcoded in YAML files or managed manually through `.env` files. Aspire introduces a more secure and flexible approach using Parameters.

For example, the `dbPassword` parameter is defined in code using `AddParameter`. This allows you to provide values securely via configuration providers like User Secrets or Azure Key Vault without modifying the orchestration code itself.

## Comparison Summary

| Feature | Docker Compose | .NET Aspire |
| :--- | :--- | :--- |
| **Configuration** | YAML (Declarative) | C# (Imperative/Code-first) |
| **Service Discovery** | Manual (DNS names, env vars) | Automatic (Service references) |
| **Observability** | Manual OTel wiring / Disparate tools | Auto-injected OTel / Integrated Dashboard |
| **Developer Experience** | Manual venv/container setup | Auto-managed venv & dependencies |
| **Polyglot Support** | Generic Containers | First-class integrations (e.g., Python, Node.js) |
| **Secrets Management** | `.env` files / Hardcoded | Abstracted Parameters (User Secrets, Key Vault) |

This comparison highlights how .NET Aspire simplifies the complexity of managing multi-service applications during development compared to manual Docker Compose configuration.