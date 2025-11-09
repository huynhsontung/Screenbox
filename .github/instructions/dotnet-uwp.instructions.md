---
description: 'Guidelines for building .NET UWP applications'
applyTo: '**/*.cs'
---

# .NET UWP

## C# Instructions

- Always use the latest version C#, currently C# 13 features.
- Write clear and concise comments for each function.
- Use `#nullable enable` directive at the top of each file to enable nullable reference types.

## General Instructions

- Make only high confidence suggestions when reviewing code changes.
- Write code with good maintainability practices, including comments on why certain design decisions were made.
- Handle edge cases and write clear exception handling.
- Be aware of the .NET and WinRT interop boundaries.
- Prefer overriding virtual UWP methods, such as `OnNavigatedTo`, instead of attaching event handlers when possible.

## Naming Conventions

- Follow PascalCase for component names, method names, public members, and constants.
- Use camelCase for local variables.
- Prefix private fields with an underscore and use camelCase (e.g., _myField).
- Prefix interface names with "I" (e.g., IUserService).
- Type parameters should start with a capital T, optionally followed by a descriptive word (e.g., T, TItem, TResult).
- Boolean properties should be named to imply true/false (e.g., IsEnabled, HasItems).

## Formatting

- Apply code-formatting style defined in `.editorconfig`.
- Prefer file-scoped namespace declarations and single-line using directives.
- Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Ensure that XML doc comments are created for any public APIs. When applicable, include `<example>` and `<code>` documentation in the comments.

## Project Setup and Structure

- Demonstrate how to organize code using feature folders or domain-driven design principles.
- Show proper separation of concerns with models, views, view models, services, factories, and other components.
- Explain dependency injection principles and how to register services in the DI container.

### Main Directories

- `Screenbox/Pages/`: XAML pages
- `Screenbox/Controls/`: Custom controls and user interface components
- `Screenbox/Converters/`: Data binding converters
- `Screenbox/Helpers/`: Utility and helper classes
- `Screenbox/Strings/`: Localization resources
- `Screenbox.Core/ViewModels/`: Presentation logic and data binding
- `Screenbox.Core/Services/`: Business logic and data access services
- `Screenbox.Core/Models/`: Data structures and entities
- `Screenbox.Core/Messages/`: MVVM Toolkit messenger message types
- `Screenbox.Core/Playback/`: Media playback logic and components

## Nullable Reference Types

- Prefer non-nullable variables and parameters unless null is a valid state.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

## Patterns

- Use MVVM architecture.
- Use Messenger pattern for communication between view models.
- View models should not directly reference views or other UI components.
- View models should not directly reference other view models; use Messenger to communicate instead. Exceptions can be made for parent-child relationships.
- Use Dependency Injection for service, factory, and view model instantiation.
- View should have minimal code-behind; use data binding and commands instead.
- View should not bind directly to models; use view models as intermediaries.
- View models should derived from `ObservableObject` or `ObservableRecipient` from the MVVM Toolkit.
- UI properties in view models should use `ObservableProperty` attribute for automatic property change notification.
- Use `ICommand` implementations (e.g., `RelayCommand`, `AsyncRelayCommand`) for handling user interactions instead of event handlers in code-behind.
- Services should be stateless.

## Testing

- Include test cases for critical paths of the application.
- Guide users through creating unit tests.
- Do not emit "Act", "Arrange" or "Assert" comments.
- Copy existing style in nearby files for test method names and capitalization.

## Performance Optimization

- Use efficient collection iteration techniques.
- Avoid unnecessary allocations and boxing where possible.
- Consider UI thread responsiveness when performing long-running operations.
- Consider UI thread affinity when working with async/await.
- Consider pagination, filtering, and sorting for large data sets.

## Sample Instruction Snippets Copilot Can Use

```cs
public class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _userName;

    [ObservableProperty]
    private string _password;

    [RelayCommand]
    private void Login()
    {
        // Add login logic here
    }
}

public class MessagingViewModel : ObservableRecipient,
    IRecipient<Message>
{
    private readonly IDependencyService _service;

    public MessagingViewModel(IDependencyService service)
    {
        _service = service;

        // Activate the view model's messenger
        IsActive = true;
    }

    public void Receive(Message message)
    {
        // Handle the received message
    }
}
```