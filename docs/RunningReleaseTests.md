# Running Release Tests

This guide covers running the release tests both **locally** (against a local Kubernetes cluster) and **on Google Kubernetes Engine** (or any remote Kubernetes cluster).

---

## 1. Running Locally (Docker Desktop Kubernetes)

### Prerequisites

1. Install [.NET 10.0+](https://dotnet.microsoft.com/download). (If you install a newer version, update all `net10.0` references in the `.csproj` files to match.)
2. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/).
3. Enable Kubernetes in Docker Desktop: **Settings → Kubernetes → Enable Kubernetes (kubeadm) → Apply & Restart**. This may take a few minutes.

> **Note on Kubernetes client compatibility:** The `KubernetesClient` package version in the `KubernetesWorkflow` project [must be compatible](https://github.com/kubernetes-client/csharp#version-compatibility) with the Kubernetes version that kubeadm exposes. For example, if kubeadm exposes Kubernetes `1.34.1`, then `KubernetesClient` must be version `18.x`.

### How it works

When you run `dotnet test` from your machine, the framework detects it is running **outside** the cluster (by checking whether the `KUBERNETES_PORT` and `KUBERNETES_SERVICE_HOST` environment variables are set). In this mode it connects to the cluster via your local `~/.kube/config`, which Docker Desktop automatically configures.

Each test creates its own isolated namespace in the cluster, starts the required Storage nodes as pods, runs the test, then tears everything down.

### Verify the cluster is working

Before running tests, confirm Docker Desktop's Kubernetes context is active:

```bash
kubectl config current-context   # should show "docker-desktop"
kubectl get nodes                # should show a Ready node
```

If the current context is not `docker-desktop` (e.g. it points to a remote cluster), switch it:
```bash
kubectl config use-context docker-desktop
```

### Run the tests

Most IDEs let you run individual tests or test fixtures directly from the code file. To run from the command line:

```bash
cd /path/to/logos-storage-nim-cs-dist-tests

# Run all release tests
dotnet test Tests/LogosStorageReleaseTests

# Run a specific test by name
dotnet test Tests/LogosStorageReleaseTests --filter=OneClientTest

# Run with verbose output
dotnet test Tests/LogosStorageReleaseTests --logger="console;verbosity=detailed"
```

### Useful environment variables

| Variable | Default | Description |
|---|---|---|
| `STORAGEDOCKERIMAGE` | `logosstorage/logos-storage-nim:latest-dist-tests` | Storage node image to test |
| `KUBECONFIG` | `~/.kube/config` | Path to kubeconfig file (optional when using Docker Desktop) |
| `LOGPATH` | `LogosStorageTestLogs` (relative) | Directory for test logs |
| `DATAFILEPATH` | `TestDataFiles` (relative) | Directory for test data files |
| `ALWAYS_LOGS` | _(unset)_ | Set to any non-empty value to always download container logs (not just on failure) |
| `TEST_TYPE` | _(unset)_ | Set to `release-tests` only when running inside the cluster (see §2c). Do **not** set this for local runs — it activates long in-cluster timeouts. |

Example — run against a specific Storage image:

```bash
STORAGEDOCKERIMAGE=logosstorage/logos-storage-nim:v0.1.8 dotnet test Tests/LogosStorageReleaseTests
```

### Troubleshooting

**`NullReferenceException` at `DistTest..ctor()`**

If every test fails immediately with a stack trace ending at `DistTest.GetWebCallTimeSet()` or `DistTest.GetK8sTimeSet()`, check whether `TEST_TYPE` is set to `release-tests` in your shell:

```bash
echo $TEST_TYPE
```

If it is, unset it before running locally:

```bash
unset TEST_TYPE
dotnet test Tests/LogosStorageReleaseTests
```

Setting `TEST_TYPE=release-tests` triggers in-cluster detection which tries to log a message through an object that hasn't been constructed yet, causing the crash.

**Tests fail with `kubectl` errors / wrong cluster**

Ensure the active context is `docker-desktop`, not a remote cluster:

```bash
kubectl config current-context      # confirm "docker-desktop"
kubectl config use-context docker-desktop   # switch if needed
kubectl get nodes                   # should show a Ready node
```

**Image not found**

The `STORAGEDOCKERIMAGE` must be pullable from Docker Desktop. Either use a published image or build and push locally:

```bash
docker pull logosstorage/logos-storage-nim:latest-dist-tests
```

---

## 2. Running on Google Kubernetes Engine (Remote Kubernetes)

### Overview

On a remote cluster the test runner itself must run **inside** the cluster, because the framework needs direct pod-to-pod networking. The CI workflow does this automatically by creating a Kubernetes Job that runs the test runner image. You can also do it manually.

Pod logs from all containers (test runner and storage nodes) are automatically shipped to Google Cloud Logging — no additional log agent is required.

### Prerequisites

- A running GKE cluster (provisioned via Terraform — see `.github/release/clusters/logos-storage-dist-tests-gcp-europe-west4/`)
- [`gcloud` CLI](https://cloud.google.com/sdk/docs/install) installed and authenticated
- `kubectl` installed
- The cluster must be pre-configured (see below)

### 2a. Cluster pre-configuration

Do these steps once per cluster. When the cluster is provisioned via CI (see §2b), the workflow handles steps 1 and 2 automatically.

**1. Authenticate kubectl against the cluster**

```bash
gcloud container clusters get-credentials logos-storage-dist-tests-gcp-europe-west4 \
  --region europe-west4 \
  --project <your-gcp-project-id>
```

**2. Create the kubeconfig secret for the test runner**

The test runner pod itself needs a kubeconfig to manage pods inside the cluster. Use a static service-account-based kubeconfig — avoid copying your local `~/.kube/config` directly, as it uses `gcloud` exec credentials that won't be available inside the pod.

```bash
# Create a service account and grant it cluster-admin access
kubectl create serviceaccount dist-tests-app
kubectl create clusterrolebinding dist-tests-app \
  --clusterrole=cluster-admin \
  --serviceaccount=default:dist-tests-app

# Create a long-lived static token
kubectl create token dist-tests-app --duration=8760h > /tmp/sa-token.txt

# Build a static kubeconfig using the token
CLUSTER=$(kubectl config view --minify -o jsonpath='{.clusters[0].name}')
SERVER=$(kubectl config view --minify -o jsonpath='{.clusters[0].cluster.server}')
kubectl config view --minify --raw -o jsonpath='{.clusters[0].cluster.certificate-authority-data}' | base64 --decode > /tmp/ca.crt

kubectl --kubeconfig=/tmp/static-kubeconfig.yaml config set-cluster "$CLUSTER" \
  --server="$SERVER" \
  --certificate-authority=/tmp/ca.crt \
  --embed-certs=true
kubectl --kubeconfig=/tmp/static-kubeconfig.yaml config set-credentials dist-tests-app \
  --token=$(cat /tmp/sa-token.txt)
kubectl --kubeconfig=/tmp/static-kubeconfig.yaml config set-context default \
  --cluster="$CLUSTER" --user=dist-tests-app
kubectl --kubeconfig=/tmp/static-kubeconfig.yaml config use-context default

# Store it as a secret
kubectl create secret generic storage-dist-tests-app-kubeconfig \
  --from-file=kubeconfig.yaml=/tmp/static-kubeconfig.yaml \
  -n default
```

The pod mounts this at `/opt/kubeconfig.yaml` and passes it via `KUBECONFIG`.

**3. (If not already present) Create the `system-node-critical` priority class**

The job manifest requests `priorityClassName: system-node-critical`. On most clusters this exists already; check with:

```bash
kubectl get priorityclass system-node-critical
```

If missing, create it or change the priority class name in [docker/job-release-tests.yaml](../docker/job-release-tests.yaml).

### 2b. Running via GitHub Actions (recommended)

This is the standard automated path. The CI workflow provisions a fresh GKE cluster, runs the tests, then tears the cluster down — all in one job.

**Required GitHub secrets:**

| Secret | Description |
|---|---|
| `RELEASE_TESTS_GCP_WORKLOAD_IDENTITY_PROVIDER` | Workload Identity Federation provider resource name |
| `RELEASE_TESTS_GCP_SERVICE_ACCOUNT` | Service account email used by the workflow |
| `RELEASE_TESTS_TF_STATE_BUCKET` | GCS bucket name for Terraform state |

**Required GitHub variables** (Settings → Secrets and variables → Actions → **Variables** tab):

| Variable | Description |
|---|---|
| `RELEASE_TESTS_GCP_PROJECT` | GCP project ID — stored as a variable (not a secret) so it appears unmasked in logs and in the Cloud Logging link printed during the run |

**Required GCP setup (one-time):**

1. Enable APIs: `container.googleapis.com`, `cloudresourcemanager.googleapis.com`
2. Create a Workload Identity Federation pool + GitHub OIDC provider bound to the `logos-storage/logos-storage-nim` repository
3. Grant the service account: `roles/container.admin` (project-level) and `roles/storage.objectAdmin` (scoped to the state bucket)

**Trigger the workflow:**

The release tests run automatically on every version tag push (`v*.*.*`). To trigger manually, go to **Actions → Release → Run workflow**.

**What happens:**

1. GitHub Actions authenticates to GCP via Workload Identity Federation (no long-lived credentials)
2. `terraform apply` provisions the GKE cluster
3. `gcloud container clusters get-credentials` configures kubectl
4. A service account + in-cluster kubeconfig secret are created
5. A Kubernetes Job is deployed from `.github/release/job-release-tests.yaml`
6. The Job runs `logosstorage/logos-storage-dist-tests:latest`, which clones this repo and runs `dotnet test Tests/LogosStorageReleaseTests`
7. The workflow streams pod logs and fails if the Job does not complete successfully
8. `terraform destroy` tears the cluster down (runs even on failure)

Pod logs are also available in Google Cloud Logging under `resource.type="k8s_container"` for the project and cluster.

### 2c. Running manually with kubectl

Useful for debugging or one-off runs against an already-provisioned cluster.

**Authenticate kubectl first (if not already done):**

```bash
gcloud container clusters get-credentials logos-storage-dist-tests-gcp-europe-west4 \
  --region europe-west4 \
  --project <your-gcp-project-id>
```

**Set the required variables:**

```bash
export NAMESPACE=default
export NAMEPREFIX=r-tests-manual
export RUNID=$(date +%Y%m%d-%H%M%S)
export TESTID=$(git rev-parse --short HEAD)
export TEST_TYPE=release-tests
export SOURCE=https://github.com/logos-storage/logos-storage-nim-cs-dist-tests.git
export BRANCH=master
export STORAGEDOCKERIMAGE=logosstorage/logos-storage-nim:latest-dist-tests
export COMMAND='["dotnet","test","Tests/LogosStorageReleaseTests"]'
```

**Apply the job:**

```bash
envsubst < docker/job-release-tests.yaml | kubectl apply -f -
```

**Follow the logs:**

```bash
# Wait for pod to start
kubectl get pod --selector job-name=$NAMEPREFIX -w

# Stream logs
POD=$(kubectl get pod --selector job-name=$NAMEPREFIX -o jsonpath='{.items[0].metadata.name}')
kubectl logs $POD -f

# Check final job status
kubectl get job $NAMEPREFIX -o jsonpath='{.status.conditions[0].type}'
# Should print "Complete"
```

Logs are also available in Cloud Logging. To query from the CLI:

```bash
gcloud logging read \
  'resource.type="k8s_container" AND resource.labels.cluster_name="logos-storage-dist-tests-gcp-europe-west4"' \
  --project=<your-gcp-project-id> \
  --format=json \
  --limit=100
```

**Cleanup:**

Jobs are auto-deleted after 24 hours (TTL configured in the manifest). To delete immediately:

```bash
kubectl delete job $NAMEPREFIX
```

### Key differences: local vs. remote

| | Local (Docker Desktop) | Remote (GKE) |
|---|---|---|
| Runner location | Your machine (external to cluster) | Inside a pod in the cluster |
| Kubeconfig | `~/.kube/config` (auto) | Mounted secret `storage-dist-tests-app-kubeconfig` |
| Network access to pods | Via `kubectl port-forward` / node IP | Direct pod-to-pod |
| `RUNNERLOCATION` detection | `ExternalToCluster` (automatic) | `InternalToCluster` (automatic inside pod) |
| How to run | `dotnet test` on your machine | Kubernetes Job |
| Image required | No (builds from source) | `logosstorage/logos-storage-dist-tests:latest` |
| Log access | Local files / console output | `kubectl logs` + Google Cloud Logging |
