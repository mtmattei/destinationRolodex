# 09 · Sources and provenance

Reviewed 7 October 2026. These links are primary technical documentation or asset source/license pages. They support the specific API/rights notes, not a claim that this handoff was compiled in Uno.

| ID | Source | Relevant use |
| --- | --- | --- |
| S1 | https://platform.uno/docs/articles/controls/SKCanvasElement.html | RenderOverride, Invalidate, support check, renderer/package conditions and WinAppSDK architecture note |
| S2 | https://skia.org/docs/user/sksl/ | Child shader eval, local/pixel coordinate spaces, color management, premultiplied alpha |
| S3 | https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skruntimeeffect?view=skiasharp | Runtime effect API family; resolve overloads against installed version |
| S3a | https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skruntimeeffect.createshader?view=skiasharp | Compile SkSL shader effect |
| S3b | https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skruntimeeffect.toshader?view=skiasharp | Instantiate shader with uniforms and children |
| S4 | https://platform.uno/docs/articles/getting-started/wizard/using-wizard.html | .NET 10, XAML/MVVM template options and target selection |
| S5 | https://platform.uno/docs/articles/Uno-UI-Performance.html | SKCanvasElement rendering guidance and frame diagnostics |
| S6 | https://unsplash.com/license | General Unsplash photo license terms; does not verify every individual photo |
| S7 | https://commons.wikimedia.org/wiki/File:Milford_Sound,_New_Zealand_(Unsplash_lSUx0xM2bEo).jpg | Lindsey Costa source, Milford Sound/Stirling Falls, page-declared CC0 |
| S8 | https://unsplash.com/photos/lush-green-mountains-surround-a-tranquil-blue-lagoon-9bmkwiFUV6o | Philippines source, Florian Delée, source page license |
| S9 | https://unsplash.com/photos/a-mountain-with-a-lake-in-front-of-it-lS1DG90Rhmw | Fuji photo, katsuma tanaka, source page license |
| S10 | https://unsplash.com/photos/a-beautiful-mountain-lake-with-dock-zWTmywY3nyg | Lago di Braies photo, Micah & Sammie Chaffin, source page license |

Norway and Iceland source URLs are retained in `assets/manifest.json`; those individual source pages were not successfully retrieved during packaging. Mark them for review rather than inventing verification.

## Local evidence

`archive/original-transition-prototype.html` and `archive/original-transition-source.zip` are unmodified copies of the prior delivered artifacts. The component source defines the visual measurements, nine-row pool, spring/camera behavior and media/data contracts. The transition renderer defines the actual procedural equations. The user's explicit instruction selects only Morph Blend and Liquid Warp, overriding the older five-mode set.

`reference/original-user-reference.png` is copied from the supplied image. `assets/images/philippines-preview.jpg` is copied from the original source ZIP. The later generated image concepts are excluded. Layout captures in `reference/` are from a real browser execution of the approved HTML, using the bundled preview as documented. Synthetic test textures belong only to QA.

No font files, third-party SDK binaries, scraped documentation copies or unsupported production photo claims are included.
