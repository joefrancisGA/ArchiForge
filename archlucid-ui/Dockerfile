# ── ArchLucid Operator UI ───────────────────────────────────────────
# Multi-stage build: install + build → Alpine runtime with standalone output.
# Build context is the archlucid-ui directory.
#
#   docker build -t archlucid-ui archlucid-ui/
# ────────────────────────────────────────────────────────────────────

# ── Stage 1: dependencies (layer-cached until package-lock changes) ─
FROM node:22-alpine AS deps

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts

# ── Stage 2: build ──────────────────────────────────────────────────
FROM node:22-alpine AS build

WORKDIR /app

COPY --from=deps /app/node_modules ./node_modules
COPY . .

# Ensure public/ exists so the runtime COPY always succeeds
# (files added later — favicon, robots.txt — are picked up automatically).
RUN mkdir -p public

# next build reads output:"standalone" from next.config.ts and produces
# .next/standalone (server) + .next/static (client assets).
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build

# ── Stage 3: runtime ───────────────────────────────────────────────
FROM node:22-alpine AS runtime

WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

# Standalone server needs only `node`; the official Node base still ships npm/yarn trees that Trivy flags (e.g. picomatch
# under npm) even though the app never invokes them. Upgrade Alpine packages for OS CVEs (e.g. zlib), then drop npm/yarn.
RUN apk update \
    && apk upgrade --no-cache \
    && rm -rf /usr/local/lib/node_modules/npm \
    && ( [ ! -d /opt ] || find /opt -maxdepth 1 -type d -name 'yarn-v*' -exec rm -rf {} + ) \
    && rm -f /usr/local/bin/npm /usr/local/bin/npx /usr/local/bin/yarn /usr/local/bin/yarnpkg

RUN addgroup -S archlucid && adduser -S archlucid -G archlucid

# Standalone output includes a minimal node_modules and server.js.
COPY --from=build --chown=archlucid:archlucid /app/.next/standalone ./
# Static assets and public folder must be copied separately.
COPY --from=build --chown=archlucid:archlucid /app/.next/static ./.next/static
COPY --from=build --chown=archlucid:archlucid /app/public ./public

USER archlucid

EXPOSE 3000

HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:3000/ || exit 1

CMD ["node", "server.js"]
