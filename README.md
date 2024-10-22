<p align="center">
  <img width="128" align="center" src="Screenbox/Assets/StoreLogo.scale-400.png">
</p>
<h1 align="center">
  Screenbox
</h1>
<p align="center">
  The modern media player for Windows
</p>
<p align="center">
  <a href='https://apps.microsoft.com/detail/9NTSNMSVCB5L?cid=storebadge&mode=mini'>
    <picture>
      <source media="(prefers-color-scheme: dark)" srcset="https://get.microsoft.com/images/en-us%20light.svg">
      <source media="(prefers-color-scheme: light)" srcset="https://get.microsoft.com/images/en-us%20dark.svg">
      <img alt="Store link" src="https://get.microsoft.com/images/en-us%20dark.svg" height="50px">
    </picture>
  </a>
</p>

Screenbox is a modern video player that cares about performance and ease of use on a wide range of device types. It features a beautiful, friendly user interface while being fast and lightweight. Screenbox is available on Windows 10 version 1903 and up, Windows 11, and Xbox consoles.

Screenbox is built on top of [LibVLCSharp](https://github.com/videolan/libvlcsharp) and the Universal Windows Platform (UWP).

![Screenshot of the home page](https://user-images.githubusercontent.com/31434093/226089502-0b82157d-8e48-408c-b501-6b6c17b8a584.png)

![Screenshot of the video player](https://user-images.githubusercontent.com/31434093/226089522-fc02208d-a7b5-4821-bb74-f48f79e9c813.png)

Some notable features:

- Fluent design user interface
- Gesture support for seeking and changing volume
- Window resize hotkeys (number row `1`-`4`)
- YouTube-inspired hotkey layout
- Picture-in-picture mode
- Save the video frame as an image
- Chromecast support
- Browse and play media over the network

And many more on the way!

## Install

The recommended way to install Screenbox is through the [Microsoft Store](https://www.microsoft.com/store/apps/9NTSNMSVCB5L). Installing through the store ensures the app stays up to date automatically. You don't need a Microsoft account to download apps from the store. The app can also be installed through winget using the following command.

```shell
winget install "Screenbox Media Player" -s msstore
```

The sideload package found on the release page is supposed to be used for validation purposes only. It lacks a proper app certificate and does not have automatic update. Before installing the package, you must install the bundled certificate in the CA Root store. This action requires you to have the system administrator permission. If you understand the implications of installing a sideload package and still wish to install it anyway, excute the `Install.ps1` file in an elavated PowerShell instance.

## Contribute

Feel free to open an issue if you want to report a bug, give feedback, or just want to ask a question. PRs are very welcome!

## Translation

[![Crowdin](https://badges.crowdin.net/screenbox/localized.svg)](https://crowdin.com/project/screenbox)

Help translate the app to your language on [Crowdin](https://crowdin.com/project/screenbox)! Crowdin offers an intuitive UX for you to get started and is the recommended tool for localization. Translations are automatically synced to GitHub and published in the next minor update.

You can also translate the app locally without Crowdin. The project source language is English, United States. All localizable source files are in the `Screenbox\Strings\en-US` directory. Localizable file types are `.resw` and `.md`. It is recommended that you use [ResX Resource Manager](https://github.com/dotnet/ResXResourceManager) for easier `.resw` translation.

Make sure you have your translations in the appropriate folder under the `Screenbox\Strings` directory. We use a [IETF language tag](https://www.venea.net/web/culture_code) to name the folder that contains resources for that language (e.g. `fr-FR` for French (France), `es-ES` for Spanish (Spain)). Files in these folders are translated copies of the original resource files in `Screenbox\Strings\en-US`.

A typical workflow for translating resources:

1. Fork and clone this repo.
1. Create a folder for your language under `Screenbox\Strings` if there isn't one already.
1. Copy over any missing files from the `en-US` folder. 
1. Translate the `.resw` and `.md` files in the directory.
1. Once you're done, commit your changes, push to GitHub, and make a pull request.
