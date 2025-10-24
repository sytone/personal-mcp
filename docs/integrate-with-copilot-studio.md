This will take you throughthe process to integrate your application with Copilot Studio. 

It is based on the guide at [[Step by Step] Host and Integrate C# MCP Server with Copilot Studio](https://rajeevpentyala.com/2025/10/16/step-by-step-host-and-integrate-c-mcp-server-with-copilot-studio/)

## Prerequisites

- Install Dev tunnel
- Install MCP Inspector (optional, for testing MCP server tools locally)
- Install .NET SDK (version 10.0 or later)


## Installing Dev tunnel

1. Open Command prompt
2. Run following command.
    ```powershell
    winget install Microsoft.devtunnel
    ```
3. Open a new command prompt and run the login command, you should be prompted to login via a popup window.
    ```powershell
    devtunnel user login
    ```
4. Now we need to create a tunnel to expose our MCP server to the internet. Run the following command, replacing `<port>` with the port your MCP server will run on (e.g., 5000) and `tunnel-id` with a name for your tunnel.
    ```powershell
    devtunnel create '<tunnel-id>'
    devtunnel port create '<tunnel-id>' -p 5000
    ```
5. Confirm the tunnel is created by running:
    ```powershell
    devtunnel list
    ```    
6. We now need to start the tunnel. Run the following command:
    ```powershell
    devtunnel host '<tunnel-id>'
    ```
7. Take a note of the 'Connect via browser' value as it will be used to access the MCP server.

## Running the MCP Server

You can run the server directly using the `dotnet run` command or build it first for better performance.

Alternatively, you can run it straight from the nuget package if you prefer not to build the project.

```powershell
dnx Sytone.Personal.Mcp@0.3.0 --yes
```


```powershell
dotnet tool install -g <your-nuget-package>
```

## Testing the MCP server locally (optional)
