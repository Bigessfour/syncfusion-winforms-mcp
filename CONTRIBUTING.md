# Contributing to Syncfusion WinForms MCP Server

Thank you for your interest in contributing! This project aims to provide production-ready validation tools for Syncfusion WinForms applications.

## How to Contribute

1. **Fork the repository** and create a feature branch
2. **Make your changes** with clear commit messages
3. **Test your changes** thoroughly
4. **Submit a pull request** with a description of what you've added/fixed

## Development Setup

1. Clone the repository
2. Open the solution in Visual Studio 2022 or VS Code
3. Restore NuGet packages: `dotnet restore`
4. Build the project: `dotnet build`
5. Run the MCP server: `dotnet run --project tools/WileyWidgetMcpServer`

## Code Style

- Follow standard C# conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and single-purpose

## Testing

- Add unit tests for new tools
- Ensure all existing tests pass
- Test headless form instantiation scenarios
- Validate output formats (text/JSON/HTML)

## Pull Request Guidelines

- Reference any related issues
- Describe the problem your PR solves
- Include screenshots for UI-related changes
- Ensure CI checks pass

## Acknowledgments

This project is made possible by:

- **Stephen McKitrick** (Creator & Maintainer)
- **AI Collaborators:** Claude Haiku 4.5, Grok 4.1
- **Open Source Community:** Syncfusion, Microsoft, .NET Foundation, GitHub, Model Context Protocol, VS Code

## Contact

- **Issues:** Use GitHub Issues for bugs and feature requests
- **Discussions:** Use GitHub Discussions for questions and ideas
- **Email:** bigessfour@gmail.com (for sensitive matters only)

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
