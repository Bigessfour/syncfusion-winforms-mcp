@Bigessfour - Opening PR to merge `add/proactive-insights-layout-test` into `main`.

This PR adds an EvalCSharp-based xUnit test verifying `ProactiveInsightsPanel` layout invariants (header min height, grid filling remaining area, overlay coverage). It also adds documentation clarifying that these tests must run on Windows hosts and will not function correctly in non-Windows Docker containers.
