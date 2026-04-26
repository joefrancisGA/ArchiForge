# ── ArchLucid Operator UI ───────────────────────────────────────────
# Multi-stage build: install + build → Alpine runtime with standalone output.
#
# Build context: REPO ROOT (not archlucid-ui/).
# The pricing generator reads docs/go-to-market/PRICING_PHILOSOPHY.md,
# which lives at the repo root and is outside the archlucid-ui/ subtree.
#
#   docker build -t archlucid-ui -f archlucid-ui/Dockerfile .
# ────────────────────────────────────────────────────────────────────

# ── Stage 1: dependencies (layer-cached until package-lock changes) ─
FROM node:22-alpine AS deps

WORKDIR /app

# Paths are relative to the repo-root build context.
COPY archlucid-ui/package.json archlucid-ui/package-lock.json ./
# `npm ci --ignore-scripts` skips sharp's install hook; Next then fails `next build` on Alpine with webpack
# "Module not found" / sharp native resolution (sharp is next's optionalDependency; required for standalone image output).
RUN npm ci --ignore-scripts && npm rebuild sharp

# ── Stage 2: build ──────────────────────────────────────────────────
FROM node:22-alpine AS build

WORKDIR /app

COPY --from=deps /app/node_modules ./node_modules

# Copy the UI source tree (everything in archlucid-ui/ becomes /app/).
COPY archlucid-ui/ .

# Vitest-only file at the UI root used to be typechecked by `next build` on older branches. It is not part
# of the runtime app; remove if present so Docker builds never fail on Vitest RTL mocks (mock lives under `testing/`).
RUN rm -f vitest-setup-rtl-mock.ts

# Synthetic aggregate ROI bulletin sample (read at SSR; canonical file lives under docs/ at repo root).
COPY docs/go-to-market/SAMPLE_AGGREGATE_ROI_BULLETIN_SYNTHETIC.md ./go-to-market-samples/SAMPLE_AGGREGATE_ROI_BULLETIN_SYNTHETIC.md

# Public accessibility self-attestation reads root ACCESSIBILITY.md at build time (mirrors monorepo `../ACCESSIBILITY.md`).
COPY ACCESSIBILITY.md ./go-to-market-samples/ACCESSIBILITY.md

# Public Trust Center marketing route reads docs/trust-center.md (mirrors monorepo `../docs/trust-center.md`).
COPY docs/trust-center.md ./go-to-market-samples/trust-center.md

# The pricing generator and its single-source document must be present at
# build time.  They live outside the archlucid-ui/ subtree, so we copy them
# to a dedicated directory (/pricing-source/) and invoke the generator with
# --repo-root pointing there.
COPY scripts/ci/generate_pricing_json.py /pricing-source/scripts/ci/generate_pricing_json.py
COPY docs/go-to-market/PRICING_PHILOSOPHY.md /pricing-source/docs/go-to-market/PRICING_PHILOSOPHY.md

# python3 is only needed in this build stage; it is NOT carried into the
# runtime image.  Run the generator then immediately call next build so we
# do not re-invoke npm run generate:pricing (which assumes the ../scripts/
# path available in local dev but not inside the container).
# NEXT_PUBLIC_* is inlined at build time for client bundles — keep in sync with docker-compose `ui.build.args`.
ARG NEXT_PUBLIC_DOCS_BASE_URL=https://github.com/joefrancisGA/ArchLucid/blob/main
ARG NEXT_PUBLIC_DEMO_URL=
ENV NEXT_PUBLIC_DOCS_BASE_URL=${NEXT_PUBLIC_DOCS_BASE_URL}
ENV NEXT_PUBLIC_DEMO_URL=${NEXT_PUBLIC_DEMO_URL}
ENV NEXT_TELEMETRY_DISABLED=1
RUN apk add --no-cache python3 \
    && mkdir -p public \
    && python3 /pricing-source/scripts/ci/generate_pricing_json.py --repo-root /pricing-source \
    && npx next build

# ── Stage 3: runtime ───────────────────────────────────────────────
FROM node:22-alpine AS runtime

WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

# Standalone server needs only `node`; the official Node base still ships npm/yarn trees that Trivy flags (e.g. picomatch
# under npm) even though the app never invokes them. Refresh Alpine index, upgrade OS packages, then pin OpenSSL ≥ 3.5.6-r0
# (CVE-2026-28390) so cached layers cannot keep libssl3/libcrypto3 on an older revision. Then drop npm/yarn.
RUN apk update --no-cache \
    && apk upgrade --no-cache \
    && apk add --no-cache --upgrade \
         "openssl>=3.5.6-r0" \
         "libssl3>=3.5.6-r0" \
         "libcrypto3>=3.5.6-r0" \
    && rm -rf /usr/local/lib/node_modules/npm \
    && ( [ ! -d /opt ] || find /opt -maxdepth 1 -type d -name 'yarn-v*' -exec rm -rf {} + ) \
    && rm -f /usr/local/bin/npm /usr/local/bin/npx /usr/local/bin/yarn /usr/local/bin/yarnpkg

RUN addgroup -S archlucid && adduser -S archlucid -G archlucid

# Standalone output includes a minimal node_modules and server.js.
COPY --from=build --chown=archlucid:archlucid /app/.next/standalone ./
# Static assets and public folder must be copied separately.
COPY --from=build --chown=archlucid:archlucid /app/.next/static ./.next/static
COPY --from=build --chown=archlucid:archlucid /app/public ./public
COPY --from=build --chown=archlucid:archlucid /app/go-to-market-samples ./go-to-market-samples

USER archlucid

EXPOSE 3000

HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:3000/ || exit 1

CMD ["node", "server.js"]
