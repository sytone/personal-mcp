# Documentation Index

Welcome to the Personal MCP Server documentation.

## Guides

- **[Publishing to NuGet](./publishing-to-nuget.md)** - Complete guide for publishing the MCP server to NuGet.org, including security checklist, testing, and troubleshooting.

## Quick Links

### For Users

- [Main README](../README.md) - Getting started, installation, and usage
- [Port Details](./port-details.md) - Implementation status and development log

### For Contributors

- [Publishing to NuGet](./publishing-to-nuget.md) - Package publication process
- [Project Constitution](../.specify/memory/constitution.md) - Development principles and standards

## Repository Structure

```powershell
obsidian-mcp/
├── docs/                       # Documentation
│   ├── README.md              # This file
│   ├── publishing-to-nuget.md # Publishing guide
│   ├── port-details.md        # Technical details
├── src/
│   └── Personal.Mcp/          # Main project
│       ├── Services/          # Core services
│       ├── Tools/             # MCP tools
│       ├── .mcp/              # MCP server metadata
│       └── Personal.Mcp.csproj
├── .vscode/                   # VS Code configuration
├── .github/                   # GitHub workflows and templates
└── README.md                  # Main documentation
```

## Additional Resources

- **MCP Protocol**: <https://modelcontextprotocol.io/>
- **NuGet.org**: <https://www.nuget.org/packages/Sytone.Personal.Mcp>
- **GitHub Repository**: <https://github.com/sytone/obsidian-mcp>
- **Original Inspiration**: <https://github.com/that0n3guy/ObsidianPilot>

---

**Need help?** Open an issue on [GitHub](https://github.com/sytone/obsidian-mcp/issues).
