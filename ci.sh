#!/bin/bash
set -e

echo "========================================="
echo "  Running ci"
echo "========================================="

# Install keepassxc-cli for tests if not already installed
if ! command -v keepassxc-cli &>/dev/null; then
    echo ""
    echo "[0/3] Installing keepassxc-cli..."
    sudo apt-get update -qq
    sudo apt-get install -y -qq keepassxc >/dev/null 2>&1 || sudo apt-get install -y keepassxc
    echo "keepassxc-cli installed: $(which keepassxc-cli)"
fi

# Step 1: Restore
echo ""
echo "[1/3] Restoring packages..."
dotnet restore Deployer.slnx
echo "Restore successful"

# Step 2: Build
echo ""
echo "[2/3] Building..."
dotnet build Deployer.slnx
echo "Build successful"

# Step 3: Tests
echo ""
echo "[3/3] Running tests..."
mkdir -p test-results
dotnet test Deployer.slnx --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx" --results-directory "test-results"
echo "Tests passed"

echo ""
echo "========================================="
echo "  All ci checks passed!"
echo "========================================="
