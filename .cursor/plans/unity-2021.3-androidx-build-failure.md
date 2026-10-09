# Unity 2021.3 Android build failure: AndroidX not enabled

Handoff for the background-filters Android library (`StreamBackgroundFilters.androidlib`) on Unity 2021.3. Written 2026-10-09 from CI run [37909572343](https://github.com/GetStream/stream-video-unity/actions/runs/37909572343).

The player build fails only on the Unity 2021.3 matrix job. Tests on that job passed. Every newer Unity version in the same workflow passed, including the Android IL2CPP player build.

## What failed

| | |
|---|---|
| Workflow | `CI/CD - Run Tests & Build Project` |
| Run | [37909572343](https://github.com/GetStream/stream-video-unity/actions/runs/37909572343) |
| Event | `pull_request` |
| PR | [GetStream/stream-video-unity#230](https://github.com/GetStream/stream-video-unity/pull/230) |
| Branch | `feature/uni-204-implement-background-filters` |
| Merge SHA checked out by CI | `bfaf5ac6a08db06bcc020f71b7d6b3e332e471f7` (`Merge 1f920c6 into a242305`) |
| Job | `build (2021.3, android, STANDARD_2_x, il2cpp)` |
| Job id | `113751198303` |
| Failed step | **Build Sample Project** (step 24) |
| Runner | `ubuntu-24.04`, GitHub-hosted |
| Started | 2026-10-09 09:10:33Z |
| Gradle failed | 2026-10-09 09:24:18Z |
| Action conclusion | `##[error]Build failed with exit code 1` |

Other jobs in the same run succeeded:

- `build (2022.3, android, NET_4_x, il2cpp)`
- `build (2023.2, android, STANDARD_2_x, il2cpp)`
- `build (6000.0, android, NET_4_x, il2cpp)`
- `build (6000.1, android, STANDARD_2_x, il2cpp)`
- `build (6000.2, android, NET_4_x, il2cpp)`

`fail-fast` is false, so those jobs were not cancelled by the 2021.3 failure.

## The error

Unity finished scripting, IL2CPP, and Gradle project generation, then invoked Gradle. Gradle 6.1.1 failed during configuration of `:launcher:lintVitalRelease`:

```
FAILURE: Build failed with an exception.

* What went wrong:
Could not determine the dependencies of task ':launcher:lintVitalRelease'.
> This project uses AndroidX dependencies, but the 'android.useAndroidX' property is not enabled. Set this property to true in the gradle.properties file and retry.
  The following AndroidX dependencies are detected: androidx.lifecycle:lifecycle-runtime:2.5.1, androidx.versionedparcelable:versionedparcelable:1.1.1, androidx.customview:customview:1.0.0, androidx.lifecycle:lifecycle-livedata-core:2.5.1, androidx.lifecycle:lifecycle-viewmodel-savedstate:2.5.1, androidx.appcompat:appcompat:1.6.1, androidx.exifinterface:exifinterface:1.0.0, androidx.core:core:1.9.0, androidx.annotation:annotation-experimental:1.3.0, androidx.fragment:fragment:1.3.6, androidx.interpolator:interpolator:1.0.0, androidx.loader:loader:1.0.0, androidx.drawerlayout:drawerlayout:1.0.0, androidx.collection:collection:1.1.0, androidx.core:core-ktx:1.9.0, androidx.viewpager:viewpager:1.0.0, androidx.activity:activity:1.6.0, androidx.arch.core:core-common:2.1.0, androidx.emoji2:emoji2-views-helper:1.2.0, androidx.savedstate:savedstate:1.2.0, androidx.annotation:annotation:1.3.0, androidx.appcompat:appcompat-resources:1.6.1, androidx.lifecycle:lifecycle-common:2.5.1, androidx.startup:startup-runtime:1.1.1, androidx.concurrent:concurrent-futures:1.0.0, androidx.emoji2:emoji2:1.2.0, androidx.lifecycle:lifecycle-livedata:2.0.0, androidx.tracing:tracing:1.0.0, androidx.lifecycle:lifecycle-viewmodel:2.5.1, androidx.resourceinspection:resourceinspection-annotation:1.0.1, androidx.lifecycle:lifecycle-process:2.4.1, androidx.arch.core:core-runtime:2.1.0, androidx.vectordrawable:vectordrawable-animated:1.1.0, androidx.cursoradapter:cursoradapter:1.0.0, androidx.vectordrawable:vectordrawable:1.1.0

BUILD FAILED in 31s
```

Unity wraps that as:

```
UnityEditor.Android.GradleInvokationException
CommandInvokationFailure: Gradle build failed.
/opt/unity/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/java -classpath ".../gradle-launcher-6.1.1.jar" org.gradle.launcher.GradleMain "-Dorg.gradle.jvmargs=-Xmx4096m" "assembleRelease"
Rethrow as BuildFailedException
```

Call stack from the player build (not from tests):

- `UnityEditor.Android.PostProcessor.Tasks.BuildGradleProject.Execute`
- `UnityEditor.Android.PostProcessAndroidPlayer.PostProcess`
- `StreamVideo.EditorTools.Builders.StreamAppBuilder.BuildSampleApp` (`Packages/StreamVideo/Editor/Builders/StreamAppBuilder.cs:40`)
- `StreamVideo.EditorTools.StreamEditorTools.BuildSampleApp` (`Packages/StreamVideo/Editor/StreamEditorTools.cs:48`)

There is also a non-fatal Gradle warning on the same invocation: `android.enableR8=false` is deprecated and will be removed in Android Gradle Plugin 5.0. That is Unity 2021.3's own generated flag. It is not the failure.

Immediately before Gradle, the log shows the background-filter library was copied into the generated project, including:

- `unityLibrary/StreamBackgroundFilters.androidlib/build.gradle`
- `unityLibrary/StreamBackgroundFilters.androidlib/src/main/java/io/getstream/unitybackgroundfilters/UnityMlKitPersonSegmenter.java`
- `unityLibrary/StreamBackgroundFilters.androidlib/src/main/AndroidManifest.xml`
- `unityLibrary/libs/libwebrtc.aar`

IL2CPP itself succeeded: `Tundra build success (238.19 seconds), 612 items updated` for `arm64-v8a`, including `libil2cpp.so`. The failure is the subsequent Gradle `assembleRelease`, not script compilation and not IL2CPP.

## Where it runs, and with which tools

Workflow file: `.github/workflows/main.ci.cd.workflow.yml`.

The 2021.3 matrix row:

```yaml
{ unity_version: "2021.3", target_platform: android, dotnet_version: STANDARD_2_x, compiler: il2cpp, dataset_index: 0, image: "unityci/editor:ubuntu-2021.3.36f1-android-3.1.0" }
```

Image that actually ran: `unityci/editor:ubuntu-2021.3.36f1-android-3.1.0`.

Editor reported by that container: **Unity 2021.3.36f1 (7a0645017be0)**.

Gradle: **6.1.1**. That pairs with Android Gradle Plugin 4.x. The androidlib comments say AGP 4 is Unity 2021.3 through 2021.3.40, and AGP 4 has no `namespace` DSL.

Committed `ProjectSettings/ProjectVersion.txt` on this branch is `2021.3.26f1 (a16dc32e0ff2)`. game-ci `unity-builder` reads that into `UNITY_VERSION` on the **Enable Tests** step (`UNITY_VERSION=2021.3.26f1`) while still launching the custom image, so the process that runs is 2021.3.36f1. The **Build Sample Project** step passed `UNITY_VERSION=2021.3.36f1` and the editor log confirms 2021.3.36f1. The ProjectVersion mismatch did not choose a different editor for the failing build.

Build invocation (step **Build Sample Project**):

- Action: `game-ci/unity-builder@v4`
- `buildMethod`: `StreamVideo.EditorTools.StreamEditorTools.BuildSampleApp`
- `customImage`: `unityci/editor:ubuntu-2021.3.36f1-android-3.1.0`
- `customParameters`: `-apiCompatibility STANDARD_2_x -scriptingBackend il2cpp -buildTargetPlatform android -buildTargetPath SampleAppBuild/2021.3_android_il2cpp_STANDARD_2_x.apk` plus the test-data args
- `allowDirtyBuild: true`

`BuildSampleApp` parses those custom parameters in `BuildSettingsCommandLineParser` and builds Android from `StreamAppBuilder`. The Android Gradle tree in the log (`Library/Bee/Android/Prj/IL2CPP/Gradle/...`) confirms the player target was Android.

game-ci also sets its own env `BUILD_TARGET=StandaloneWindows64` and `BUILD_FILE=...apk.exe` because this workflow does not pass game-ci's `targetPlatform` input. That default is overridden by `-buildTargetPlatform android` inside `BuildSampleApp`. It looks wrong in the `docker run` line and is not the failure.

## What the 2021.3 job did before the failure

Steps that succeeded:

1. Checkout, disk cleanup, apt deps, docker image selection.
2. **Enable Tests** — `unity-builder` runs `StreamEditorTools.EnableStreamTestsEnabledCompilerFlag`. It set scripting define `STREAM_TESTS_ENABLED` and quit. `Exiting batchmode successfully`. This step is not a player build.
3. **Run Tests (Attempt 1)** — `game-ci/unity-test-runner@v4.3.1`, `testMode: all`. Succeeded. Retries 2 and 3 were skipped.
4. Upload test results, `assert-tests-ran.sh` for editmode and playmode XML, free disk, `git diff`.
5. **Build Sample Project** — failed as above.
6. Upload Build as Artifact was skipped because the build failed.
7. Slack notify ran (`if: always() && failure()`).

Test runner warning, not a failure: `allowDirtyBuild` is not a valid input of `unity-test-runner@v4.3.1` (it is valid on `unity-builder`). The workflow still passes it. The action ignores it.

## Why AndroidX shows up

`Packages/StreamVideo/Runtime/Libs/BackgroundFilters/Plugins/Android/StreamBackgroundFilters.androidlib/build.gradle` depends on ML Kit:

```gradle
dependencies {
    implementation fileTree(dir: 'libs', include: ['*.jar'])
    implementation 'com.google.mlkit:segmentation-selfie:16.0.0-beta6'
}
```

That coordinate pulls the AndroidX artifacts listed in the Gradle error (`androidx.appcompat`, `lifecycle`, `core`, `fragment`, and the rest). The Java entry point that needs it is `UnityMlKitPersonSegmenter.java` in the same androidlib.

Unity 2021.3.36 does not write `android.useAndroidX=true` into the generated root `gradle.properties`. Newer Unity editors in this matrix do, which is why 2022.3, 2023.2, and Unity 6 jobs passed with the same library.

There is no `gradleTemplate.properties` anywhere in the repo, and nothing in `Packages/StreamVideo` implements `IPostGenerateGradleAndroidProject` or sets `useAndroidX` / `enableJetifier`.

`project.properties` inside the androidlib only contains:

```
target=android-31
android.library=true
```

That is the old Eclipse Android format. It does not set `android.useAndroidX`, and a `gradle.properties` placed inside the androidlib would not satisfy the check either. Android Gradle Plugin reads `android.useAndroidX` from the **root** Gradle project's `gradle.properties`, which Unity generates and then invokes `assembleRelease` against.

## Same commit that people will suspect: it did not cause this

Commit `dd2754fa5e5ada8bcc8d3681477a7fb78b61e830` (2026-10-08 14:27 +0200), message `Remove double reference of com.google.mlkit`.

What it changed:

- Deleted `Packages/StreamVideo/Editor/BackgroundFiltersDependencies.xml` (and its `.meta`). That file was an External Dependency Manager for Unity (EDM4U) manifest with a second copy of the same coordinate:

  ```xml
  <androidPackage spec="com.google.mlkit:segmentation-selfie:16.0.0-beta6" />
  ```

- Stopped setting `namespace "io.getstream.unitybackgroundfilters"` unconditionally inside the `android {}` block. AGP 4 has no `namespace` DSL. The commit sets it only when `androidExt.hasProperty("namespace")`. The manifest `package` attribute is the namespace on AGP 4.

What it did **not** change:

- The `implementation 'com.google.mlkit:segmentation-selfie:16.0.0-beta6'` line. It was already in `build.gradle` before this commit and is still there. That line is what pulls AndroidX into the player build.

This repository does not vendor EDM4U / Play Services Resolver / `google-jar-resolver`. A search of `Packages` for those names finds nothing. The deleted XML only affects a customer project that already has EDM4U. It never ran in this CI job, so it never wrote `android.useAndroidX` into CI's Gradle project. Removing it cannot be why CI's `gradle.properties` lacks that flag.

The namespace change in that commit is what lets Unity 2021.3.36 parse the androidlib at all. Before it, an unconditional `namespace` would have failed earlier on AGP 4 if the player build had been reached. The AndroidX gap was already latent as soon as the androidlib depended on ML Kit.

## Earlier 2021.3 runs never reached the player build

These runs on the same branch failed the 2021.3 job in **Run Tests (Attempt 3)**, so **Build Sample Project** did not run. They do not show this Gradle error, and they do not prove the player build used to pass.

| Run | When (UTC) | Relative to `dd2754f` | 2021.3 failed step | Job id |
|---|---|---|---|---|
| [37772761055](https://github.com/GetStream/stream-video-unity/actions/runs/37772761055) | 2026-10-08 11:50 | before | Run Tests (Attempt 3) | `113296115697` |
| [37777033392](https://github.com/GetStream/stream-video-unity/actions/runs/37777033392) | 2026-10-08 12:27 | before | Run Tests (Attempt 3) | `113310385510` |
| [37801362910](https://github.com/GetStream/stream-video-unity/actions/runs/37801362910) | 2026-10-08 15:30 | after | Run Tests (Attempt 3) | `113394128649` |
| [37909572343](https://github.com/GetStream/stream-video-unity/actions/runs/37909572343) | 2026-10-09 09:10 | after | **Build Sample Project** | `113751198303` |

The test-failure reasons in the October 8 runs were not investigated. They are a separate problem. Nightly `main` on 2026-10-09 ([37887248001](https://github.com/GetStream/stream-video-unity/actions/runs/37887248001)) succeeded; that branch does not contain this background-filter androidlib.

## How this was analyzed

1. Local log `C:\Users\Daniel\Downloads\new 173330.txt` (2,119,792 bytes, 11,301 lines). It is the 2021.3 job log **cut off** at 2026-10-09 09:17:14, mid-stack, during playmode `BackgroundFilterTests` while leaving a call. It does not contain the Gradle failure. The last meaningful SDK lines in that file are a successful call join, then:

   `[Stream Video] Background filter is not supported on this platform or device. The request was ignored.`

   That warning is the Linux CI editor (no camera / no ML Kit device support). The test is written to not throw in that case. It is not the build failure.

2. GitHub Actions API for job `113751198303` showed step conclusions. Tests attempt 1 succeeded. **Build Sample Project** failed.

3. Full job log downloaded from `GET /repos/GetStream/stream-video-unity/actions/jobs/113751198303/logs` (~4.8 MB). The Gradle exception is at the end of that log, about seven minutes after the truncated file ends.

4. Compared matrix siblings on the same run (all success except 2021.3).

5. Read the workflow, `StreamBackgroundFilters.androidlib/build.gradle`, `project.properties`, and searched the package for `useAndroidX`, `enableJetifier`, `gradleTemplate.properties`, and `IPostGenerateGradleAndroidProject`. None of those exist.

6. Inspected `dd2754f` and the parent version of `build.gradle`. Confirmed the Maven `implementation` line predates the dedup, and the deleted file was only the EDM4U XML.

7. Checked October 8 2021.3 jobs via the Actions API. They failed in test attempt 3 and never started the player build.

## Ruled out

- **Script compile / IL2CPP.** Tundra succeeded. The only compiler message noted was an existing warning: `RtcSession._audioPlaybackPausedByUser` is assigned but never used (`CS0414`).
- **Tests.** Attempt 1 passed on this run. The background-filter "not supported on this platform" warning is expected on the Linux editor.
- **Removing the duplicate ML Kit XML.** See the commit section above.
- **`BUILD_TARGET=StandaloneWindows64` in the game-ci docker env.** The sample build still produced an Android Gradle project because `BuildSampleApp` uses `-buildTargetPlatform android`.
- **`ProjectVersion.txt` saying 2021.3.26f1.** The failing editor process was 2021.3.36f1 from the custom image.
- **`Native extension for Android target not found`.** Logged during editor startup on this image, including during license activation of an empty project, and the editor continued through compile, tests, and Gradle generation. The managed Android module did load (`UnityEditor.Android.Extensions.dll`).
- **License.** Entitlement activation succeeded (`Pro License: NO`, personal/ULF). `Access token is unavailable; failed to update` is the usual first licensing line before the serial update succeeds.
- **`allowDirtyBuild` warning on the test runner.** Ignored unknown input. Tests still ran.
- **Disk space.** The job frees disk before the build. The failure is a Gradle property check, not `ENOSPC`.

## Recommended fix

Do this in the SDK, not only in the sample project's custom Gradle template. A template under the sample app would fix CI and would not fix a customer project that builds with the package.

Add an editor `IPostGenerateGradleAndroidProject` (assembly `StreamVideo.EditorTools` is editor-only and already exists). Unity calls it after generating the Gradle project and **before** `assembleRelease`. The log on this run shows that order: `Calling IPostGenerateGradleAndroidProject callbacks` (1.3 ms, nothing implemented), then `Building Gradle project`.

`OnPostGenerateGradleAndroidProject` receives the `unityLibrary` directory. The file that matters is the parent:

`Path.GetFullPath(Path.Combine(path, "..", "gradle.properties"))`

Append these lines only if they are not already present:

```
android.useAndroidX=true
android.enableJetifier=true
```

`android.useAndroidX=true` is the property this failure names.

`android.enableJetifier=true` was **not** exercised by this log. It is the usual companion on Unity 2021.3 because that editor still ships Android Support Library artifacts. Enabling AndroidX alone often fails the next Gradle step with duplicate `android.support` / `androidx` classes. Unity's own templates on 2022+ set both flags. On 2022.3 and Unity 6 the post-processor should see the lines already present and write nothing.

Do not put the flag only in `StreamBackgroundFilters.androidlib/project.properties` or in a `gradle.properties` inside that library. AGP checks the root project, and Gradle has already loaded `gradle.properties` before the androidlib `build.gradle` runs, so writing the file from the library during configuration does not affect the current build.

## What was not done

- The post-processor was not implemented.
- The October 8 test failures were not diagnosed.
- No local Android player build was run.
- Customer projects that use EDM4U were not built. The handoff note in `.cursor/plans/background-filters-review-handoff.md` still calls out a separate risk: EDM4U in "download AARs" mode plus this androidlib's Maven `implementation` can duplicate `com.google.mlkit` classes. That is not the error in this CI log.
