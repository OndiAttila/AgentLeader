#!/bin/bash

dotnet run --project AgentLead/AgentLead.csproj -- --base-url=http://MiniPc:12434/v1 --model=qwen2.5-coder:14b $*
