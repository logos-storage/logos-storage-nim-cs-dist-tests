# Distributed System Tests for Nim-Codex

## Local setup
These steps will help you set up everything you need to run and debug the tests on your local system.

### Installing the requirements.
1. Install dotnet v10.0 or newer. (If you install a newer version, consider updating the .csproj files by replacing all mention of `net10.0` with your version.)
1. Set up a nice C# IDE or plugin for your current IDE.
1. Install docker desktop.
1. In the docker-desktop settings, enable kubernetes using kubeadm. (This might take a few minutes.) Note that the version of the `KubernetesClient` package in the `KubernetesWorkflow` project [must be compatible with](https://github.com/kubernetes-client/csharp#version-compatibility) the version of Kubernetes that `kubeadm` exposes. For example, the current version of Kubernetes that kubeadm exposes is `1.34.1`, therefore `KubernetesClient` must use version `18.x`. See https://github.com/kubernetes-client/csharp#version-compatibility for more information.

### Running the tests
Most IDEs will let you run individual tests or test fixtures straight from the code file. If you want to run all the tests, you can use `dotnet test`. You can control which tests to run by specifying which folder of tests to run. `dotnet test Tests/CodexTests` will run only the tests in `/Tests/CodexTests` and exclude the long tests.
