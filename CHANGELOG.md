# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0]

### Added

- Initial release.
- Bakes a KAT (KillFrenzy Avatar Text) compatible character board atlas from any Dynamic font.
- Live preview that re-bakes a few sample characters whenever a setting changes.
- Two background modes: transparent, or the opaque band the original atlas uses.
- Outline width control, baked into the alpha channel via a chamfer distance transform.
- Undo / Redo support on every setting.
- Optional automatic assignment of the generated texture to a material's `_MainTex`.
