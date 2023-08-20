<p align="center">
  <img width="128" align="center" src="https://user-images.githubusercontent.com/31434093/157200320-19a6a26e-c466-4d62-baae-6e2ff9fa4593.png">
</p>
<h1 align="center">
  Screenbox
</h1>
<p align="center">
  The modern media player for Windows
</p>
<p align="center">
  <a href='//www.microsoft.com/store/apps/9NTSNMSVCB5L?cid=storebadge&ocid=badge'>
    <picture>
      <source media="(prefers-color-scheme: dark)" srcset="https://get.microsoft.com/images/en-us%20light.svg">
      <source media="(prefers-color-scheme: light)" srcset="https://get.microsoft.com/images/en-us%20dark.svg">
      <img alt="Store link" src="https://get.microsoft.com/images/en-us%20dark.svg" height="50px">
    </picture>
  </a>
</p>

Screenbox is a modern video player with a focus on performance and ease of use on a wide range of Windows devices. It features a beautiful, friendly user interface while being fast and lightweight. Screenbox is available on Windows devices and Xbox consoles.

Screenbox is built on top of [LibVLCSharp](https://github.com/videolan/libvlcsharp) and the Universal Windows Platform (UWP).

![Screenshot of the home page](https://user-images.githubusercontent.com/31434093/226089502-0b82157d-8e48-408c-b501-6b6c17b8a584.png)

![Screenshot of the video player](https://user-images.githubusercontent.com/31434093/226089522-fc02208d-a7b5-4821-bb74-f48f79e9c813.png)

Some notable features:

- Fluent design user interface
- Gesture support for seeking and changing volume
- Window resize hotkeys (number row `1`-`4`)
- YouTube inspired hotkey layout
- Picture-in-picture mode
- Save video frame as image
- Chromecast support
- Browse and play media over the network

And many more on the way!

## Contribute

Feel free to open an issue if you want to report a bug, give feedback, or just want to ask a question. PRs are very welcome!

## Translation

[![Crowdin](https://badges.crowdin.net/screenbox/localized.svg)](https://crowdin.com/project/screenbox)

Help translate the app on [Crowdin](https://crowdin.com/project/screenbox)! Crowdin offers an intuitive UX for you to get started with localization and is therefore the recommended tool for the job.

If you wish to translate the app to other languages without Crowdin, follow the steps below.

### Adding a new language

*Requires Visual Studio 2022 and the [Multilingual App Toolkit extension](https://marketplace.visualstudio.com/items?itemName=dts-publisher.mat2022).*

- Fork and clone this repo.
- Open in VS 2022.
- Right click on the `Screenbox` project.
- Select Multilingual App Toolkit > Add translation language.
    - If you get a message saying "Translation Provider Manager Issue," just click Ok and ignore it. It's unrelated to adding a language.
- Select a language.
- Once you select a language, new `.xlf` files will be created in the `MultilingualResources` folder.
- Follow the steps of "Improving an existing language" below.

### Improving an existing language

- Inside the `MultilingualResources` folder, open the `.xlf` of the language you want to translate.
    - You can open using any text editor, or you can use the [Multilingual Editor](https://developer.microsoft.com/windows/develop/multilingual-app-toolkit)
- If you're using a text editor, translate the strings inside the `<target>` node. Then change the `state` property to `translated`.
- If you're using the Multilingual Editor, translate the strings inside the `Translation` text field. Make sure to save to preserve your changes.
- Once you're done, commit your changes, push to GitHub, and make a pull request.

