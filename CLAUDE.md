# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build                                        # build all projects
dotnet test Tests/LogosStorageTests                 # short tests
dotnet test Tests/LogosStorageLongTests             # long tests
dotnet test Tests/LogosStorageReleaseTests          # release/CI tests
dotnet test --filter=TestClassName                  # single test class
docker-compose -f docker/docker-compose.yaml up    # run via Docker locally
```

## Architecture

The repo is a C# distributed test framework that orchestrates Logos Storage nodes inside a Kubernetes cluster.

**`Framework/`** — reusable infrastructure, independent of Logos Storage:
- `Core/` — plugin system, entry point, lifecycle management, `IK8sTimeSet` (configurable timeouts)
- `KubernetesWorkflow/` — all K8s interaction: `K8sController.cs` owns namespace lifecycle, deployments, PVCs, service accounts
- `Logging/`, `Utils/`, `OverwatchTranscript/` — support libraries

**`ProjectPlugins/`** — Logos Storage-specific extensions wired into the framework:
- `StoragePlugin/` — container recipes for storage nodes, `LogosStorageNode` wrapper
- `LogosStorageClient/` — generated OpenAPI client
- `MetricsPlugin/` — Prometheus scraping during tests

**`Tests/`** — NUnit test assemblies that consume the framework. `DistTestCore/` is the base class library; the other projects contain actual tests and are run separately.

## Test Lifecycle

Each test gets an isolated Kubernetes namespace named `storage-<guid>`. The lifecycle per test:

1. **Setup**: `DistTest` constructor creates the namespace and injects labels (`deployid`, `runid`, `testid`, `fixturename`, `testname`) onto every pod — used for log correlation in GCP.
2. **Body**: test code calls `Ci.AddLogosStorage()` and similar helpers, which create K8s Deployments via `K8sController`.
3. **TearDown**: `lifecycle.DeleteAllResources()` → `Decommission()` → `K8sController.DeleteNamespace()`. Before namespace deletion, all PVCs have their `kubernetes.io/pvc-protection` finalizer patched to `[]` then are deleted — this is intentional so the GCE CSI driver can release backing PDs asynchronously while tests continue.
4. **Global TearDown**: `Global.TearDown()` calls `DeleteNamespacesStartingWith("storage-", wait: true)` to sweep any leftover namespaces from the run.

By default teardown is fire-and-forget (`wait: false`) so the next test starts while K8s cleans up in the background. Annotate a test with `[WaitForCleanup]` to block until the namespace is fully gone.

After teardown, each test writes a JSON result to raw stdout (bypassing NUnit's capture):
```json
{"type":"test-result","runid":"...","fixture":"...","testname":"...","status":"Passed","success":true}
```
GCP's logging agent parses this into queryable structured fields.

## Docker Entrypoint

`docker/docker-entrypoint.sh` clones this repo at container startup — the Docker image does not bundle the source. The branch to clone is controlled by `BRANCH` (default: `master`). Code changes must be on the cloned branch to take effect; no image rebuild is needed.

Key env vars for remote/GCP runs:

| Variable | Purpose |
|---|---|
| `BRANCH` | Branch of this repo to clone at startup |
| `KUBECONFIG` | Path to kubeconfig (required for GKE) |
| `RUNNERLOCATION` | `InternalToCluster` when running inside GKE |
| `STORAGEDOCKERIMAGE` | Override the storage node image |
| `RUNID` | Run identifier for log aggregation |
| `TEST_TYPE=release-tests` | Enables long timeouts |
| `ALWAYS_LOGS=1` | Download logs even on passing tests |

## OpenAPI Client / `openapi.yaml`

`ProjectPlugins/LogosStorageClient/openapi.yaml` is **not committed**. It is materialized at
build time so the NSwag-generated API client always matches the Logos Storage container under
test. (The previous approach — commit the spec and hash-check it at runtime via `ApiChecker` —
drifted whenever the storage repo's spec changed; both `ApiChecker` and the
`LogosStoragePluginPrebuild` project were removed.)

How the spec is obtained depends on the environment, decided by whether a Docker daemon is
reachable:

- **Local dev** (Docker Desktop, Linux Docker Engine, or WSL — any reachable daemon): the
  MSBuild target `MaterializeOpenApi` in `ProjectPlugins/LogosStorageClient/LogosStorageClient.csproj`
  (runs `BeforeTargets="_GenerateOpenApiCode"`) extracts it byte-for-byte from the storage image
  via `docker cp` and **always overwrites** it, so switching `STORAGEDOCKERIMAGE` never reuses a
  stale spec. Image = `STORAGEDOCKERIMAGE` (default `logosstorage/logos-storage-nim:latest-dist-tests`).
  Consequence: a local `dotnet build`/`dotnet test` now requires a running Docker daemon.
- **CI** (the runner pod has no Docker daemon): each `docker/job-*.yaml` runs an `initContainer`
  on the image under test that copies `/logosstorage/openapi.yaml` into a shared `emptyDir`;
  `docker/docker-entrypoint.sh` then places it into `ProjectPlugins/LogosStorageClient/openapi.yaml`
  after cloning, before the build. The MSBuild target detects no daemon and uses that pre-placed
  file, failing loudly if it is missing.

Gotchas:
- Editing `docker/docker-entrypoint.sh` requires rebuilding the runner image via the
  `docker-runner.yml` workflow (it triggers on changes to that file).
- `run-dist-tests.yaml` sets `STORAGEDOCKERIMAGE` so its job manifest can resolve the
  initContainer image, like the release and continuous workflows already do.

## GCP Release Test Infrastructure

Release tests run on an ephemeral GKE cluster provisioned by Terraform in the `logos-storage-nim` repo (`.github/release/clusters/`). The cluster is created, tests run, then destroyed — all within a single CI workflow run.

The `release-tests@sodium-ray-494007-v3.iam.gserviceaccount.com` SA runs the CI workflow. Its project-level IAM roles are **not tracked in any Terraform or version control** — they were granted imperatively and must be re-granted manually if the GCP project is ever recreated:

- `roles/compute.viewer`
- `roles/container.admin`
- `roles/storage.objectAdmin`
- `projects/sodium-ray-494007-v3/roles/releaseTestsDiskCleaner` — custom role with `compute.disks.delete` + `compute.disks.list`, required by the orphaned-disk cleanup step in the release workflow
