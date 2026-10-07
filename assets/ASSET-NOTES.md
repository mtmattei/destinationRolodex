# Asset inventory and preparation

## Bundled now

The original user reference PNG, the original 828×333 Philippines loading crop, 15 SVG icons extracted from the prototype, six destination records and the two procedural effect definitions. There is no displacement bitmap to acquire: Morph Blend computes its flow and Liquid Warp computes its ring in the shader.

## Not bundled

All six full-size photographs. They were externally referenced in the original prototype. Downloads failed in this environment, so the manifest records the gap. Do not rename the loading crop to ph.jpg and call photography complete. Do not reuse the PNG's embedded words/logo as app content.

`tools/fetch_assets.py` obtains the exact selected photographs, not fresh search results. It validates decoding, corrects EXIF orientation, writes normalized JPEGs atomically and records image dimensions and SHA-256 hashes. It reports missing originals and returns a nonzero exit code rather than silently using alternate scenery.

| File expected | Destination | Credit retained |
| --- | --- | --- |
| `photographs/ph.jpg` | Philippines | Florian Delée |
| `photographs/jp.jpg` | Japan | katsuma tanaka |
| `photographs/it.jpg` | Italy | Micah & Sammie Chaffin |
| `photographs/no.jpg` | Norway | Bingqi Huang, source-page verification pending |
| `photographs/is.jpg` | Iceland | Denys Nevozhai, source-page verification pending |
| `photographs/nz.jpg` | New Zealand | Lindsey Costa |

## Rights

Code licensing does not cover photographs. Retain per-image source URLs/credits in the app and handoff. The retrieved Philippines/Japan/Italy source pages identify the standard Unsplash License. The Commons New Zealand page identifies a pre-June-2017 CC0 release. Norway/Iceland attribution is inherited from the prototype and needs source-page confirmation. See S6–S10 in `briefs/09-sources.md`; review current source terms before public distribution.

The user-provided reference and its preview crop have no independently established production rights here. Treat them as internal reference material. No font files are included; resolve the app's typography separately.

## Uno import

After fetch succeeds, copy the six JPEGs to `Assets/Destinations/` in the generated Uno project and ensure the selected project template packages them as app assets. The manifest's `unoUri` fields define their intended resource URIs. The existing JSON uses web URLs; map each media reference by ID to the packaged URI for the native repository. Keep alt text, author and focal point in the data model. Do not attempt to pass an ms-appx URI directly to a browser Image element.

Use decoded dimensions for aspect calculations; do not trust a thumbnail URL's width suffix as actual bytes/dimensions. Visual-check the downloaded files against their source pages before acceptance.
