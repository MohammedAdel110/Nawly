# Nawly Web (Angular PWA) — نَولي

Local multi-store ordering and delivery web application built for residents, shop owners, and couriers of Nawa village in Qalyubia, Egypt.

## Stack (Phase 1)

- **Angular 21** (standalone components, Signals, `@if`/`@for` control flow, lazy `loadChildren`, functional guards & interceptors)
- **Tailwind CSS v4** (`@tailwindcss/postcss`, custom Egyptian local marketplace tokens in `src/styles.css`)
- **RTL-first Arabic (`ar`) + English (`en`)** switching with tabular Western digits (`0-9`) and `Cairo` / `Plus Jakarta Sans` fonts
- **Backend Proxy**: `proxy.conf.json` forwards `/api` and `/hubs` to `http://localhost:5082`

## Development

```powershell
cd F:\Nawly\frontend
npm start
```

Open `http://localhost:4200/` in your browser.

## Production Build

```powershell
cd F:\Nawly\frontend
npm run build
```
