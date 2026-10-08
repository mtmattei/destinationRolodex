# Evidence: a keyed Style without BasedOn drops the theme template; Simple style-setter ThemeResources resolve at app scope

Session `dc4f13dc-b01c-431e-96bb-89c8d5c37940`, 2026-10-08. Uno.Sdk 6.7.30, Uno.WinUI 6.7.135, Uno.Simple 7.1.1 (`UnoFeatures` SimpleTheme),
net10.0-desktop (Win32, Skia). App: Destination Rolodex demo page (`MainPage.xaml`, lab sliders).

## Symptom
`<Style x:Key="DemoSlider" TargetType="Slider">` with `Foreground`/`Background` setters painted the sage track, yet every thumb stayed the
Fluent accent blue (`captures`: `p2_lab.png`, `p3_sliders.png`, `p4_sliders.png`).

## Probes (in order)
1. Page-level `<SolidColorBrush x:Key="SliderThumbBackground">` in `Page.Resources`: no change.
2. Same keys inside `Page.Resources` → `ResourceDictionary.ThemeDictionaries` (Light + Default), per the Uno Themes lightweight-styling doc: no change.
3. Same keys in `App.xaml` merged `ThemeDictionaries` (after `SimpleToolkitTheme`): no change.
4. Read the theme source (`src/library/Uno.Simple.WinUI/Styles/Controls/Slider.xaml`, master): the thumb is
   `<Thumb Style="{StaticResource SimpleSliderThumbStyle}">` and that style sets `Background="{ThemeResource SliderThumbBackground}"`.
   The Simple `Slider` template was never in play: a keyed `Style` with no `BasedOn` inherits nothing, so the control used the Fluent
   (`XamlControlsResources`) template, whose thumb reads other keys.

## Fix that held
`<Style x:Key="DemoSlider" TargetType="Slider" BasedOn="{StaticResource SimpleSliderStyle}">` plus the `SliderThumbBackground*`,
`SliderTrackValueFill*`, `SliderTrackFill` overrides in `App.xaml` `ThemeDictionaries` (`p5_sliders.png`: sage round thumbs). The toggle
override (`ToggleSwitchKnobBoundsFill`) worked from `Page.Resources` because the ToggleSwitch used the implicit Simple style all along.

## Rule
- A keyed style must `BasedOn` the theme style (`SimpleSliderStyle`, `SimpleButtonStyle`, …) or it silently reverts to the Fluent template.
- Lightweight keys consumed by a *style setter* (`SimpleSliderThumbStyle`) resolve at app scope: put the override in `App.xaml`
  `ThemeDictionaries`, never only in `Page.Resources`.
- Key names are not in the docs tables the MCP returns; extract them from the theme assembly (UTF-16 strings) or read the XAML on GitHub.
