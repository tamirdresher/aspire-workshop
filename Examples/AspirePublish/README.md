# Publish, Deploy, and Destroy with Aspire

This sample demonstrates the current Aspire deployment pipeline with one AppHost and two
selectable targets:

* Docker Compose for a local, OCI-compatible deployment.
* An existing Kubernetes cluster, deployed as a generated Helm release.

The sample is pinned to the latest stable Aspire release available when this guide was
updated: Aspire CLI and AppHost SDK **13.4.6**. The Docker and Python integrations are stable.
The Kubernetes and AKS hosting integrations shipped alongside 13.4.6 as
`13.4.6-preview.1.26319.6`, so their APIs can change before becoming stable.
The file-based AppHost opts out of the repository's central package versions so these
deployment-specific pins remain self-contained and reproducible.

## What the commands do

| Command | Result |
| --- | --- |
| `aspire publish` | Builds the production application model and writes target-specific artifacts. It does not install or start them. |
| `aspire deploy` | Runs the deployment pipeline: build, push when required, provision, and install or update the selected target. |
| `aspire destroy` | Runs the selected compute environment's destroy step using persisted deployment state. |
| `aspire do <step>` | Runs one discovered pipeline step and its dependencies. Step names come from the selected target. |

Use `--list-steps` before an operation to inspect its pipeline without running it:

```bash
aspire deploy --list-steps -- --target compose
aspire publish --list-steps -- --target k8s --registry registry.example.com
aspire destroy --list-steps --yes --non-interactive -- --target compose
```

Arguments after `--` are passed to `AppHost.cs`. This sample maps `--target` and
`--registry` into AppHost configuration. The default target is `compose`. The CLI requires
`--yes` whenever `destroy` is non-interactive, including when only listing steps.

## Common prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download)
* [Aspire CLI](https://aspire.dev/get-started/install-cli/) 13.4.6 or later
* Python 3.13 or later for the included FastAPI service
* An OCI-compatible container runtime for image builds:
  [Docker](https://docs.docker.com/get-started/get-docker/) or
  [Podman](https://podman.io/docs/installation)

Confirm the installed release before using the version-pinned sample:

```bash
aspire --version
aspire doctor
```

Run all commands in this directory. `aspire.config.json` identifies `AppHost.cs`, so
`--apphost` is not required.

## Docker Compose target

Docker Compose deployment requires Docker Compose or a compatible Podman setup. The current
`Aspire.Hosting.Docker` integration adds the Compose publish, deploy, and destroy pipeline
steps.

### Generate artifacts

```bash
aspire publish \
  --output-path ./aspire-output/compose \
  --environment Production \
  --non-interactive \
  -- \
  --target compose
```

The output includes `docker-compose.yaml` and environment files. Review generated artifacts
before promoting them through CI/CD; they can contain deployment configuration and references
to secrets.

### Deploy

```bash
aspire deploy \
  --output-path ./aspire-output/compose \
  --environment Production \
  --non-interactive \
  -- \
  --target compose
```

Aspire builds the image and runs the selected Compose runtime's `compose up` operation. Read
the pipeline summary for the allocated application and dashboard URLs; do not assume fixed
localhost ports.

### Destroy

```bash
aspire destroy \
  --output-path ./aspire-output/compose \
  --environment Production \
  --yes \
  --non-interactive \
  -- \
  --target compose
```

For Compose, destroy runs `compose down`, which stops and removes the stack's containers and
network. Named volumes are retained unless you remove them separately. `--yes` is required for
unattended teardown.

## Existing Kubernetes cluster target

The Kubernetes integration generates a Helm chart from the AppHost model. Project and
container resources become Kubernetes workloads, endpoints become Services, and
configuration is represented by ConfigMaps and Secrets.

### Additional prerequisites

* An existing Kubernetes cluster
* [`kubectl`](https://kubernetes.io/docs/tasks/tools/) configured for the target cluster
* [Helm](https://helm.sh/docs/intro/install/) **4.2.0 or later**
* A container registry reachable from both the workstation and the cluster nodes
* Local credentials that can push to that registry and cluster credentials that can pull

Always verify the active context before deploying:

```bash
kubectl config current-context
helm version
```

The registry API used by this sample is also preview in 13.4.6. Pass only the registry host,
for example `registry.example.com` or `registry.example.com:5000`, and authenticate with the
registry before deployment.

### Generate a Helm chart

```bash
aspire publish \
  --output-path ./aspire-output/k8s \
  --environment Production \
  --non-interactive \
  -- \
  --target k8s \
  --registry registry.example.com
```

This writes `Chart.yaml`, `values.yaml`, and resource templates under the output directory.
Publishing is the right boundary when a GitOps system, rather than Aspire, owns installation.
Commit reviewed overlays or values files, not generated secrets.

### Deploy to the current context

```bash
aspire deploy \
  --output-path ./aspire-output/k8s \
  --environment Production \
  --non-interactive \
  -- \
  --target k8s \
  --registry registry.example.com
```

Aspire builds and pushes images, then uses Helm to install the release into the
`aspire-publish` namespace. A later deploy upgrades the tracked release.

### Inspect and destroy

```bash
kubectl get all --namespace aspire-publish
helm list --namespace aspire-publish

aspire destroy \
  --output-path ./aspire-output/k8s \
  --environment Production \
  --yes \
  --non-interactive \
  -- \
  --target k8s \
  --registry registry.example.com
```

Destroy removes the application Helm release and the resources it owns. External charts
registered with `AddHelmChart` are intentionally retained by default because tools such as
cert-manager can be shared across applications. Add `.WithDestroy()` to an app-specific
external chart only when it should be uninstalled with the application.

## Kubernetes routing and TLS

`WithExternalHttpEndpoints()` identifies an endpoint intended for external access, but a
Kubernetes deployment still needs a routing and load-balancing implementation. Do not assume
that a generated Service also installs an ingress controller, a GatewayClass, DNS, or a TLS
issuer.

For an existing cluster:

1. Confirm that the cluster has an Ingress controller or Gateway API implementation.
2. Model routes with Aspire's `AddIngress` or `AddGateway` APIs.
3. Install shared components such as cert-manager or an ingress controller separately, or
   register their charts with `AddHelmChart`.
4. Verify the generated hostname, route, certificate, and network policy before exposing
   production traffic.

Prefer Gateway API for new designs when the target cluster supports it. Use Ingress when an
existing platform standard or controller requires it. Kubernetes Secrets are base64 encoded,
not encrypted; integrate the generated chart with the secret-management controls required by
your platform.

## AKS target

AKS uses a different hosting integration:

```bash
aspire integration search azure-kubernetes --format Json --non-interactive
aspire add azure-kubernetes --apphost <path-to-apphost> --version <version-from-search> --non-interactive
```

Configure the target with:

```csharp
var aks = builder.AddAzureKubernetesEnvironment("aks");
```

When the AKS environment is the only compute environment, Aspire automatically targets all
compute resources to it. `aspire publish` generates Helm and Bicep artifacts. `aspire deploy`
provisions AKS, Azure Container Registry, managed identity, referenced Azure resources, and
then installs the application chart. A separate registry parameter is not required.

Local deployment uses Azure CLI credentials by default:

```bash
az login
```

For non-interactive deployment, provide the Azure settings and credentials expected by your
environment, including `Azure__SubscriptionId`, `Azure__Location`, and an isolated
`Azure__ResourceGroup`. Azure destroy removes the deployment resource group and everything in
it, so never point this workflow at a resource group containing unrelated resources.

Services are cluster-internal by default. The current AKS guidance uses Azure Application
Gateway for Containers with Gateway API:

* `AddLoadBalancer(...)` provisions the AGC integration.
* `AddGateway(...)` and `WithRoute(...)` model public routes.
* `AddCertManager(...)`, `AddIssuer(...)`, and `WithTls(...)` automate HTTPS.

Use the official [AKS deployment guide](https://aspire.dev/deployment/kubernetes/aks/) for
the required subnet delegation, Gateway API, DNS, and cert-manager configuration.

## CI/CD rules

* Pin the Aspire CLI, AppHost SDK, and hosting integration versions together.
* Run with `--non-interactive`; supply required parameters and credentials explicitly.
* Use `--pipeline-log-level` for deployment pipeline verbosity. The old pipeline
  `--log-level` spelling was replaced in Aspire 13.3.
* Keep `--environment`, `--output-path`, AppHost path, and target arguments consistent across
  deploy and destroy.
* Use `aspire <operation> --list-steps` to discover target-specific steps before scripting
  `aspire do <step>`.
* Treat `aspire-output`, deployment state, generated `.env` files, kubeconfigs, and registry
  credentials as deployment artifacts, not source material.
* Require a deliberate approval before `aspire destroy --yes`.

## Current references

* [Publishing and deployment overview](https://aspire.dev/deployment/deploy-with-aspire/)
* [Pipelines and app topology](https://aspire.dev/deployment/pipelines/)
* [Deploy to Kubernetes clusters](https://aspire.dev/deployment/kubernetes/clusters/)
* [Deploy to AKS](https://aspire.dev/deployment/kubernetes/aks/)
* [Install external Helm charts](https://aspire.dev/deployment/kubernetes/helm-charts/)
* [`aspire publish`](https://aspire.dev/reference/cli/commands/aspire-publish/)
* [`aspire deploy`](https://aspire.dev/reference/cli/commands/aspire-deploy/)
* [`aspire destroy`](https://aspire.dev/reference/cli/commands/aspire-destroy/)
