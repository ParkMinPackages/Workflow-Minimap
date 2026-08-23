# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [4.3.0] - 2026-08-23

### Added
- Added a read-only marker collection and `ClearMarkers()` for bulk marker removal.

## [4.2.0] - 2026-08-22

### Added
- Added `MiniMapUI.SnapToTargetView()` to immediately complete the current smoothed view transition and update markers.

## [4.1.0] - 2026-08-22

### Added
- Added direct-child validation for the minimap image and marker container during initialization.

### Changed
- Configured the marker container to stretch across `MiniMapUI` and render after the minimap image.

### Fixed
- Initialized marker visibility state to match its initially visible GameObject.

## [4.0.0] - 2026-08-22

### Changed
- Changed `MiniMapUI.Initialize()` to require an explicit `MiniMapCamera` argument.
- Removed the serialized camera reference and public camera property from `MiniMapUI`.
- Kept start-time initialization by resolving the camera with the matching actor ID.

## [3.0.0] - 2026-08-22

### Changed
- Renamed `MiniMapMarker` to `MiniMapMarkerUI` and updated the marker APIs exposed by `MiniMapUI`.
- Added the custom editor icon metadata to `MiniMapMarkerUI`.

## [2.0.0] - 2026-08-22

### Added
- Added `IAutoRunController` for shared automatic-update attachment and disposal behavior.
- Added automatic bottom-to-top fitting with world or ratio padding overloads and static-target optimization.

### Changed
- Converted the bottom-to-top fitting and target-tracking controllers from `MonoBehaviour` components to constructor-injected C# classes.
- Moved UI logic controllers from `Components/UILogics` to `Objects/UILogics`.

## [1.0.0] - 2026-08-22

### Added
- Added `MiniMapCamera` with editor preview, runtime capture data, layer filtering, and configurable output resolution.
- Added `MiniMapUI` with world-space centering, rotation, view-height control, optional smoothing, and marker management.
- Added `MiniMapMarker` with static-target caching, rotation modes, out-of-bounds modes, and reactive bounds state.
- Added bottom-to-top fitting and target-tracking controllers for fixed and dynamically framed minimap views.

## [0.1.0] - 2026-08-21

### Added
- Added the initial Unity package structure and assembly definitions.
- Added package metadata, documentation, tests, samples, and dependency metadata scaffolding.
