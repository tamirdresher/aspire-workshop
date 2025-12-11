# Architecture and Design: Docker Compose vs. .NET Aspire Comparison

## Goal
Demonstrate how .NET Aspire simplifies local development compared to Docker Compose for a polyglot, complex application consisting of React, .NET, Python, PostgreSQL, Redis, and RabbitMQ.

## 1. Folder Structure
We will use a **Shared Source** strategy. The application source code will reside in a common `src` directory, while the orchestration logic will be separated into `docker` and `aspire` directories. This highlights that the application code remains largely agnostic to the orchestration tool, but the "glue" changes.

```text
complex-comparison/
├── src/                            # Shared Application Source Code
│   ├── frontend/                   # React Application (Node.js)
│   │   ├── Dockerfile              # Needed for Docker Compose
│   │   ├── package.json
│   │   └── ...
│   ├── backend/                    # .NET Web API
│   │   ├── Dockerfile              # Needed for Docker Compose
│   │   ├── Program.cs
│   │   └── ...
│   └── ai-service/                 # Python Service (FastAPI)
│       ├── Dockerfile              # Needed for Docker Compose
│       ├── main.py
│       └── requirements.txt
├── docker/                         # Docker Compose Orchestration
│   ├── docker-compose.yml
│   └── .env                        # Environment variables for Compose
└── aspire/                         # .NET Aspire Orchestration
    ├── ComplexApp.AppHost/         # The Aspire Orchestrator Project
    │   ├── Program.cs              # Defines resources and relationships
    │   └── ComplexApp.AppHost.csproj
    └── ComplexApp.ServiceDefaults/ # Shared .NET defaults (OpenTelemetry, etc.)
```

## 2. Architecture & Data Flow

The system follows an asynchronous processing pattern suitable for AI workloads.

### Components
1.  **Frontend (React):** User interface for submitting requests and viewing results.
2.  **Backend API (.NET):** Gateway for the frontend; manages request validation, caching, and queuing.
3.  **Message Queue (RabbitMQ):** Decouples the API from the heavy AI processing.
4.  **AI Service (Python):** Consumes messages, performs "AI processing" (simulated), and saves results.
5.  **Database (PostgreSQL):** Persistent storage for processed results.
6.  **Cache (Redis):** Caches frequent read requests from the API.

### Data Flow
1.  **Submission:** User submits a text prompt via the **Frontend**.
2.  **API Request:** Frontend sends a POST request to the **Backend API**.
3.  **Queueing:**
    *   Backend API generates a Job ID.
    *   Backend API publishes the job details to **RabbitMQ**.
    *   Backend API returns the Job ID to the Frontend immediately (Accepted 202).
4.  **Processing:**
    *   **AI Service** listens to RabbitMQ.
    *   It receives the message and simulates processing (e.g., a delay).
    *   It saves the result to **PostgreSQL**.
5.  **Retrieval:**
    *   Frontend polls the Backend API with the Job ID.
    *   Backend API checks **Redis** for the result.
    *   If not in Redis, it queries **PostgreSQL**.
    *   If found in DB, it caches the result in **Redis** and returns it to the Frontend.

## 3. Configuration & Environment Variables

### Docker Compose Approach
Requires manual wiring in `docker-compose.yml` and `.env`.

*   **Frontend:** `REACT_APP_API_URL` (hardcoded to localhost port or internal docker network alias).
*   **Backend:**
    *   `ConnectionStrings__Redis`
    *   `ConnectionStrings__Postgres`
    *   `RabbitMQ__Host`
*   **AI Service:**
    *   `POSTGRES_CONNECTION_STRING`
    *   `RABBITMQ_HOST`

### .NET Aspire Approach
Uses C# to define relationships. Connection strings are injected automatically.

*   **AppHost (`Program.cs`):**
    *   Defines `postgres`, `redis`, `rabbitmq` resources.
    *   Defines `backend`, `frontend`, `ai-service` projects.
    *   Uses `.WithReference()` to link them.
*   **Service Discovery:** Aspire handles dynamic port allocation and service discovery names (e.g., `http://backend`).

## 4. Implementation Plan (Code Mode)

1.  **Scaffold Directory Structure:** Create the root folders and `src` subfolders.
2.  **Implement Shared Services (src/):**
    *   Create a basic .NET Web API (`backend`).
    *   Create a basic React app (`frontend`).
    *   Create a basic Python FastAPI app (`ai-service`).
3.  **Implement Docker Orchestration (docker/):**
    *   Create `Dockerfile` for each service in their respective `src` folders.
    *   Create `docker-compose.yml` wiring up Postgres, Redis, RabbitMQ, and the 3 services.
4.  **Implement Aspire Orchestration (aspire/):**
    *   Create `ComplexApp.AppHost` and `ComplexApp.ServiceDefaults`.
    *   Update `AppHost/Program.cs` to add containers (Redis, Postgres, RabbitMQ) and projects.
    *   *Crucial:* Add the Python project resource (requires `Aspire.Hosting.Python` or generic container/executable resource).
    *   *Crucial:* Add the Node.js project resource (requires `Aspire.Hosting.NodeJs`).
5.  **Verify & Refine:**
    *   Ensure environment variables match what the code expects in both scenarios.
