#!/bin/bash
git submodule update --init
git submodule update --recursive
dotnet build --no-incremental -c Release xivModdingFramework/xivModdingFramework/xivModdingFramework.csproj -o FFXIV_Modding_Tool/references/ && dotnet build --no-incremental -c Release FFXIV_Modding_Tool/FFXIV_Modding_Tool.csproj
