#!/bin/bash

# Create screenshots directory
mkdir -p screenshots

# Restore and build
echo "Restoring packages..."
dotnet restore
echo "Building project..."
dotnet build --no-restore

# Install Playwright
echo "Installing Playwright browsers..."
dotnet build /t:PlaywrightInstall

# Run tests
echo "Running tests..."
dotnet test --no-build --verbosity normal
