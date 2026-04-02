# ── ArchiForge Operator UI ──────────────────────────────────────────
# Multi-stage build: install + build → Alpine runtime with standalone output.
# Build context is the archiforge-ui directory.
#
#   docker build -t archiforge-ui archiforge-ui/
# ────────────────────────────────────────────────────────────────────

# ── Stage 1: dependencies (layer-cached until package-lock changes) ─
FROM node:25-alpine AS deps

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts

# ── Stage 2: build ──────────────────────────────────────────────────
FROM node:25-alpine AS build

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
FROM node:25-alpine AS runtime

WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

RUN addgroup -S archiforge && adduser -S archiforge -G archiforge

# Standalone output includes a minimal node_modules and server.js.
COPY --from=build --chown=archiforge:archiforge /app/.next/standalone ./
# Static assets and public folder must be copied separately.
COPY --from=build --chown=archiforge:archiforge /app/.next/static ./.next/static
COPY --from=build --chown=archiforge:archiforge /app/public ./public

USER archiforge

EXPOSE 3000

HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:3000/ || exit 1

CMD ["node", "server.js"]
