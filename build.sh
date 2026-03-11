#!/bin/bash

pushd AgentLead
dotnet build
popd
pushd MCPServer
dotnet build
popd
dotnet test AgentLead.Tests
