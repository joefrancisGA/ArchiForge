#!/usr/bin/env bash
# Installs k6 from dl.k6.io apt repo on Debian/Ubuntu (e.g. GitHub ubuntu-latest).
# GitHub-hosted runners may lack a usable gpg homedir and dirmngr; without them
# `gpg --keyserver ... --recv-keys` fails with "No dirmngr".
set -euo pipefail

sudo apt-get update
sudo apt-get install -y --no-install-recommends ca-certificates curl gnupg dirmngr

sudo mkdir -p /root/.gnupg
sudo chmod 700 /root/.gnupg

sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg \
  --keyserver hkp://keyserver.ubuntu.com:80 \
  --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69

echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list

sudo apt-get update
sudo apt-get install -y k6

k6 version
