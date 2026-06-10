# AGENTS.md

## Project

This repository contains a simple C# TCP chat server built with `TcpListener` and `TcpClient`.

## Guidelines

- Keep the implementation small and easy to read.
- Prefer standard .NET APIs before adding external packages.
- Use asynchronous socket handling for client connections and message reads.
- Keep server and client responsibilities separated when adding a client app.
- Handle client disconnects and socket exceptions without crashing the server.
- Avoid committing build output, IDE metadata, logs, or local environment files.

## Commands

- Build with `dotnet build` once a project file exists.
- Run tests with `dotnet test` if test projects are added.

## Notes

- This is a learning project, not a production chat system.
- Do not add authentication, encryption, persistence, or protocol complexity unless requested.
