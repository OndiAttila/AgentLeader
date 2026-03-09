#!/bin/bash

pushd AgentLead
dotnet build
popd
dotnet test AgentLead.Tests
