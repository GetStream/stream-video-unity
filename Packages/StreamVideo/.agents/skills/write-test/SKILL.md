---
name: write-test
description: Write tests for the Stream Video Unity SDK. Use when creating, modifying, or reviewing test files. Covers assembly structure, naming conventions, base classes, async patterns, and all project-specific rules.
---

# Write Test

Guidelines and conventions for writing tests in the Stream Video Unity SDK.

## When to Use

- When creating a new test file or test method
- When modifying existing tests
- When reviewing test code for convention compliance
- When asked to add test coverage for a feature

## Project Structure

Tests live under `Tests/` in three assemblies:

| Assembly | Folder | Purpose | Inherits `TestsBase`? |
|---|---|---|---|
| `StreamVideo.Tests.Editor` | `Tests/Editor/` | Pure-logic editor tests — no networking or live client | **No** (unless the test needs a connection, e.g. `StreamClientTests`) |
| `StreamVideo.Tests.Runtime` | `Tests/Runtime/` | Integration tests — require a live Stream Video client | **Yes, always** |
| `StreamVideo.Tests.Shared` | `Tests/Shared/` | Shared base classes, utilities, test client infra | N/A (not tests themselves) |

Never create new `.asmdef` files. Place your file in the correct existing folder.

## Compilation Guard

**Every** test `.cs` file must be wrapped in:

```csharp
#if STREAM_TESTS_ENABLED
// ... entire file contents ...
#endif
```

This is a master kill-switch that prevents test code from compiling in production builds.

## Namespaces

- Editor tests: `StreamVideo.Tests.Editor`
- Runtime tests: `StreamVideo.Tests.Runtime`
- Shared utilities: `StreamVideo.Tests.Shared`

## Class Conventions

- Mark test classes `internal` (optionally `sealed` for editor tests).
- Name classes `{SubjectUnderTest}Tests` — e.g. `SessionIDTests`, `CallsTests`.
- Add an XML doc `<summary>` comment on the class referencing the class under test via `<see cref="..."/>`.
- Store the subject-under-test or any per-test state as **private fields at the bottom** of the class.

## Test Method Naming

### Runtime / integration tests

Use the **`When_<condition>_expect_<outcome>`** pattern with underscores:

```
When_two_clients_join_same_call_expect_no_errors
When_setting_call_custom_data_expect_custom_data_set
When_participant_pinned_expect_pinned_participants_changed_event_fired
```

The private async companion uses the same name with an `_Async` suffix:

```
When_two_clients_join_same_call_expect_no_errors_Async
```

### Editor / unit tests

Use **`<Method>_<Scenario>_<Expected>`** or a descriptive phrase:

```
NewInstance_IsEmpty
Regenerate_IncrementsVersion
Clear_DoesNotResetVersion
Regenerate_AfterClear_RestoresNonEmptyStateAndIncrementsVersion
```

## Writing an Editor Test (no client connection)

1. Create `{Feature}Tests.cs` in `Tests/Editor/`.
2. Do **not** inherit from `TestsBase`.
3. Use `[Test]` attribute for synchronous tests.
4. Use `[SetUp]` for per-test initialization.
5. Use NUnit constraint-model assertions (`Assert.That(...)`) with descriptive failure messages.

### Template

```csharp
#if STREAM_TESTS_ENABLED
using NUnit.Framework;
using StreamVideo.Core.SomeNamespace;

namespace StreamVideo.Tests.Editor
{
    /// <summary>
    /// Tests for <see cref="MyFeature"/>.
    /// </summary>
    internal sealed class MyFeatureTests
    {
        [SetUp]
        public void SetUp()
        {
            _myFeature = new MyFeature();
        }

        [Test]
        public void SomeProperty_DefaultValue_IsExpected()
        {
            Assert.That(_myFeature.SomeProperty, Is.EqualTo(expectedValue),
                "Descriptive failure message explaining what went wrong.");
        }

        private MyFeature _myFeature;
    }
}
#endif
```

## Writing a Runtime Test (needs client connection)

1. Create `{Feature}Tests.cs` in `Tests/Runtime/`.
2. **Inherit from `TestsBase`**.
3. Use `[UnityTest]` attribute (returns `IEnumerator`).
4. Delegate to `ConnectAndExecute(...)` which handles client provisioning, connection, and retry-on-rate-limit.
5. Write the actual logic in a **private `async Task` method** with the `_Async` suffix.
6. Choose the right `ConnectAndExecute` overload:
   - `ConnectAndExecute(SingleClientTestHandler)` — one `ITestClient`
   - `ConnectAndExecute(TwoClientsTestHandler)` — two `ITestClient` params
   - `ConnectAndExecute(Func<Task>)` — no client params (connection still happens)

### Template

```csharp
#if STREAM_TESTS_ENABLED
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamVideo.Tests.Shared;
using UnityEngine.TestTools;

namespace StreamVideo.Tests.Runtime
{
    /// <summary>
    /// Tests for <see cref="SomeFeature"/>.
    /// </summary>
    internal class SomeFeatureTests : TestsBase
    {
        [UnityTest]
        public IEnumerator When_doing_X_expect_Y()
            => ConnectAndExecute(When_doing_X_expect_Y_Async);

        private async Task When_doing_X_expect_Y_Async(ITestClient client)
        {
            var call = await client.JoinRandomCallAsync();

            // ... test logic ...

            await WaitForConditionAsync(() => /* condition to poll */);
            Assert.AreEqual(expected, actual);
        }
    }
}
#endif
```

## The Async-to-Coroutine Bridge

Unity's `[UnityTest]` requires `IEnumerator`, but test logic is `async Task`. The bridge works like this:

```
[UnityTest] public IEnumerator MethodName()
    => ConnectAndExecute(MethodName_Async);
```

`ConnectAndExecute` internally calls `RunAsIEnumerator()` (from `TestUtils`) which polls `task.IsCompleted` each frame and rethrows faults. You never need to call `RunAsIEnumerator()` directly in a runtime test that uses `ConnectAndExecute`.

For editor tests that are async but don't need a client, use the `Execute(...)` helper:

```csharp
[UnityTest]
public IEnumerator My_async_editor_test()
    => Execute(My_async_editor_test_Async);
```

Or call `RunAsIEnumerator()` directly on the task:

```csharp
[UnityTest]
public IEnumerator My_async_editor_test()
    => My_async_editor_test_Async().RunAsIEnumerator();
```

## Key Shared Utilities

| Utility | What it does |
|---|---|
| `TestsBase` | Base class for runtime tests. Manages client lifecycle, teardown, retry logic. |
| `ITestClient` / `TestClient` | Wraps `IStreamVideoClient` with test helpers like `JoinRandomCallAsync()`. |
| `StreamTestClientProvider` | Singleton that pools and reuses `IStreamVideoClient` instances across fixtures. |
| `WaitForConditionAsync(condition, timeoutMs)` | Polls a `Func<bool>` until true or timeout. Use instead of `Task.Delay`. |
| `DisposableAssetsProvider` | Tracks and disposes Unity assets (e.g. `WebCamTexture`) created during tests. |
| `TestUtils.RunAsIEnumerator()` | Extension method bridging `Task` → `IEnumerator` for Unity test runner. |
| `TestUtils.TryGetFirstWorkingCameraDeviceAsync()` | Finds a working camera device for video tests. |

## Conditional Ignore for Camera Tests

If a test requires a camera device, add the conditional ignore attribute:

```csharp
[UnityTest, ConditionalIgnore(IgnoreConditionNoCameraKey, IgnoreConditionNoCameraReason)]
public IEnumerator When_client_joins_call_with_video_expect_receiving_video_track()
    => ConnectAndExecute(When_client_joins_call_with_video_expect_receiving_video_track_Async,
        ignoreFailingMessages: true);
```

These constants are defined in `TestsBase`.

## Golden Rules

1. **Always** wrap files in `#if STREAM_TESTS_ENABLED` / `#endif`.
2. **Never** create a new `.asmdef` — use the existing `Tests/Editor/` or `Tests/Runtime/` folder.
3. **Inherit `TestsBase`** for runtime tests; **don't** for pure-logic editor tests.
4. **Follow the naming patterns**: `When_<condition>_expect_<outcome>` for runtime; `<Method>_<Scenario>_<Expected>` for unit tests.
5. **Use the async-bridge pattern**: public `[UnityTest]` → `IEnumerator` → `ConnectAndExecute` → private `async Task` with `_Async` suffix.
6. **Add descriptive assertion messages** to every assertion.
7. **Don't manage client lifecycle manually** — let `TestsBase` and `StreamTestClientProvider` handle it.
8. **Use `WaitForConditionAsync`** instead of `Task.Delay` for polling asynchronous state.
9. **Track disposable assets** via `DisposableAssetsProvider`.
10. **Keep test classes `internal`** — tests are not public API.
11. **Add `<summary>` XML doc comments** on the class with `<see cref="..."/>` to the subject under test.
12. **Place shared helpers** in `Tests/Shared/` — never duplicate utilities across Editor and Runtime.
