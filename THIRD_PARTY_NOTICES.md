# Third-party notices

WashinPC source is distributed under GPL-3.0. Third-party components retain their own licenses.

| Component | Use | License and attribution |
|---|---|---|
| Inter | Embedded UI font | SIL Open Font License 1.1; Copyright 2020 The Inter Project Authors. [Full notice](src/Assets/Fonts/Inter/OFL.txt). Portable packages include `licenses/Inter-OFL.txt`. |
| Lucide / Feather-derived icons | SVG assets and interface icon geometry | ISC / MIT notices, including Feather attribution, in [the upstream license](docs/licenses/lucide-ISC.txt). Portable packages include `licenses/lucide-ISC.txt`. |
| Nerdbank.GitVersioning 3.9.50 | Build-time version generation | MIT; package metadata and notices are retained in the offline NuGet package. Not a runtime dependency. |
| MSTest and Microsoft test platform | Development tests | Microsoft package licenses and notices are retained in the offline NuGet packages. Not shipped as application runtime assemblies. |

The repository keeps original `.nupkg` files under `.nuget/offline-feed`, including their metadata and bundled notices, for offline restores. Transitive test/build package versions are recorded in `packages.lock.json`. Windows and .NET Framework are prerequisites, not bundled runtimes.
