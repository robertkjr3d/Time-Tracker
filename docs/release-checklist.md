# Release Checklist

Use this checklist before creating a GitHub release.

## 1) Versioning

- [ ] Update `<Version>` in `Time Tracker/Time Tracker.csproj`
- [ ] Confirm changelog/release notes draft

## 2) Local Validation

- [ ] Build solution: `dotnet build "Time Tracker.slnx"`
- [ ] Run tests: `dotnet test "Time Tracker.slnx"`
- [ ] Manual smoke test on Windows
- [ ] Manual smoke test on Linux VM (recommended)

## 3) Packaging

- [ ] Verify GitHub Actions run succeeded on `master`
- [ ] Confirm artifacts uploaded:
  - [ ] `time-tracker-win-x64`
  - [ ] `time-tracker-linux-x64`
  - [ ] `time-tracker-linux-arm64`
  - [ ] `time-tracker-osx-x64`
  - [ ] `time-tracker-osx-arm64`
  - [ ] `time-tracker-deb-linux-x64`

## 4) Linux .deb Validation

- [ ] Install `.deb` on Ubuntu test VM
- [ ] Launch with `time-tracker`
- [ ] Verify database creation, tracking, edit, and CSV export

## 5) macOS Notes

- [ ] Confirm release notes include unsigned/notarization disclaimer
- [ ] If distributing broadly, complete Apple signing/notarization process

## 6) GitHub Release

- [ ] Create release tag
- [ ] Attach final artifacts (if desired beyond workflow artifacts)
- [ ] Publish release notes with platform-specific install notes
