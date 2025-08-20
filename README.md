# Synack

**noun**  
**/snæk/**

**1.** A lightweight, high-performance TCP listener and HTTP parser designed as a modern replacement for `HttpListener`.  
&emsp;*"When you've outgrown HttpListener but not grown into Kestrel - that's when you use Synack"*

**2.** (informal) A satisfying, bite-sized solution to heavyweight problems in HTTP handling.  
&emsp;*"When all you need is simple, fast, and HTTP - Synack satisfies."*

**Origin:**  
A portmanteau of *SYN* and *ACK*, the opening handshake packets of TCP communication - pronounced like *snack*, because it's just that good.

**Compare:**  
`HttpListener`, `TcpListener`

[![docs](https://img.shields.io/badge/docs-github.io-blue)](https://scottoffen.github.io/synack)
[![NuGet](https://img.shields.io/nuget/v/synack)](https://www.nuget.org/packages/synack/)
[![MIT](https://img.shields.io/github/license/scottoffen/synack?color=blue)](./LICENSE)
[![Target1](https://img.shields.io/badge/netstandard-2.0-blue)](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)
[![Target2](https://img.shields.io/badge/dotnet-8.0-blue)](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)
[![Contributor Covenant](https://img.shields.io/badge/Contributor%20Covenant-2.1-blue.svg)](./CODE_OF_CONDUCT.md)

## Installation

Synack is available on [NuGet.org](https://www.nuget.org/packages/synack/) and can be installed using a NuGet package manager or the .NET CLI.

## Features

| Feature                    | Description                                                                |
| -------------------------- | -------------------------------------------------------------------------- |
| **Lightweight Core**       | No dependencies on ASP.NET Core or hosting infrastructure                  |
| **TCP Listener**           | Built directly on `TcpListener` for maximum control                        |
| **HTTP/1.1 Parsing**       | Minimal-allocation request parsing with support for common headers         |
| **Async Request Handling** | Async-first design, no thread-per-connection overhead                      |
| **Pluggable Routing**      | Delegates request handling to your own logic, no built-in router           |
| **Prefix Support**         | Drop-in compatibility for `HttpListener`-style prefix filtering            |
| **Custom Response Logic**  | Full control over response headers, status codes, and body output          |
| **WebSocket Ready**        | Architecture supports future upgrade handling for WebSocket connections    |
| **TLS Support (Planned)**  | Modular TLS support without built-in HTTPS middleware (via `SslStream`)    |
| **Cross-Platform**         | Runs on Windows, Linux, and macOS with zero platform-specific behavior     |

## Usage and Support

- Check out the project documentation https://scottoffen.github.io/synack.

- Engage in our [community discussions](https://github.com/scottoffen/synack/discussions) for Q&A, ideas, and show and tell!

- Have a question you can't find an answer for in the documentation or discussions? You can ask your questions on [StackOverflow](https://stackoverflow.com) using [#synack](https://stackoverflow.com/questions/tagged/synack?sort=newest). Make sure you include the version of Synack you are using, the platform you using it on, code samples and any specific error messages you are seeing.

- **Issues created to ask "how to" questions will be closed.**

## Contributing

We welcome contributions from the community! In order to ensure the best experience for everyone, before creating an issue or submitting a pull request, please see the [contributing guidelines](CONTRIBUTING.md) and the [code of conduct](CODE_OF_CONDUCT.md). Failure to adhere to these guidelines can result in significant delays in getting your contributions included in the project.

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/scottoffen/synack/releases).

## Test Coverage

You can generate and open a test coverage report by running the following command in the project root:

```bash
pwsh ./test-coverage.ps1
```

> [!NOTE]
> This is a [Powershell](https://learn.microsoft.com/en-us/powershell/) script. You must have Powershell installed to run this command.
> The command depends on the global installation of the dotnet tool [ReportGenerator](https://www.nuget.org/packages/ReportGenerator).

## License

Synack is licensed under the [MIT](./LICENSE) license.

## Using Synack? We'd Love To Hear About It!

Few thing are as satisfying as hearing that your open source project is being used and appreciated by others. Jump over to the discussion boards and [share the love](https://github.com/scottoffen/synack/discussions)!